<?php
function IsAllowManage(mysqli $connection, string $user_email, string $purpose ): bool {

    $statement = $connection->prepare( "SELECT 1 FROM accounts WHERE user_email = ?" );
    $statement->bind_param( "s", $user_email );
    $statement->execute();

    $result = $statement->get_result();
    $row = $result->num_rows;
    
    if($purpose === "create_account") return $row === 0;
    else if( $purpose === "reset_password" ) return $row === 1;

    return false;
}



header("Content-Type: application/json; charset=UTF-8");

// Only allow to post
// === -> value and var need to be the same
// == -> only value
if ($_SERVER["REQUEST_METHOD"] !== "POST") {

    // send to the Unity 
    // tell some things happened
    // 405 is means there have the method but not allow
    http_response_code(405);

    // if the method not post or use get open
    echo json_encode([
        "success" => false,
        "message" => "Method Not Allowed"
    ]);

    exit;
}

$data = json_decode( file_get_contents("php://input"), true);

// Check JSON
if (!is_array($data)) {
    // send to the Unity 
    // tell some things happened
    // 400 is means the resuit have somethings wrong
    http_response_code(400);
    echo json_encode([ "success" => false, "message" => "Invalid JSON" ]);
    exit;
}

$user_email = trim($data["user_email"] ?? "");
$user_name = trim($data["user_name"] ?? "");
$password = $data["password"] ?? "";

// checking the useremail, username, password
if (!filter_var($user_email, FILTER_VALIDATE_EMAIL)
    || !preg_match('/^[\p{L}\p{N}_]{1,100}$/u',$user_name)
    || !preg_match('/^[A-Za-z0-9_#!+*&-]{8,100}$/',$password)) {
    
    // send to the Unity 
    // tell some things happened
    // 400 is means the result have somethings wrong
    http_response_code(400);

    echo json_encode(
    [
        "success" => false,
        "message" => "Not A Email"
    ]
    );

    exit;
}

// connect to MySQL -> database
$connection = null;

try{

    require "/var/www/unity-config/db.php";
    require_once "/var/www/html/unity-api/database-function.php";

    // find verify data and username by player_uid
    // SELECT => witch data I want 
    // FROM => the tatget database's table
    // WHERE => the condition used to find the target row
    $statement = $connection->prepare( 
    "SELECT verified, expires_at, purpose 
    FROM auth_codes 
    WHERE user_email = ?" );

    $statement->bind_param( "s", $user_email );
    // run SQL
    $statement->execute();

    // get SQL result
    // SELECT
    $result = $statement->get_result();

    // check whether player_uid exists
    // from the SQL result
    // should be 1 and only 1
    if ($result->num_rows !== 1) {

        http_response_code(401);

        echo json_encode([
            "success" => false,
            "message" => "Invalid email or non verified email"
        ]);

        exit;
    }

    // got certificated data 
    $certification_data = $result->fetch_assoc();

    // check the uid is verified or not
    // compair the enter's data and the database's data
    if ((int)$certification_data["verified"] !== 1
        ||strtotime($certification_data["expires_at"]) <= time()) {

        http_response_code(401);

        echo json_encode([
            "success" => false,
            "message" => "Using non-verified uid"
        ]);

        exit;
    }

    // the purpose of certification
    $purpose = (string) $certification_data["purpose"];

    // password change to hash password
    // to avoid store the password in the database
    // the same password can generate different hashes
    // password_verify() is used to verify the password later
    $passwordHash = password_hash( $password, PASSWORD_DEFAULT);

    $isAllowManage = IsAllowManage($connection, $user_email, $purpose);

    if(!$isAllowManage){
        http_response_code(500);

        echo json_encode([
            "success" => false,
            "message" => "DataBase Data Error"
        ]);

        exit;
    
    }

    // stop the auto save of the dataBase
    // ready to create a new account
    $connection->begin_transaction();

    if( $purpose === "create_account" ){
        
        // create a SQL of the account
        $statement = $connection->prepare( 
        "INSERT INTO accounts (user_email, user_name, password_hash)
        VALUES (?, ?, ?)");

        // writing the SQL
        // s = string
        $statement->bind_param( "sss", $user_email, $user_name, $passwordHash);

        // Run the SQL
        $statement->execute();

        $user_id = (int)$connection->insert_id;
        BuildDefaultPlayerData($connection,$user_id);

    }
    else if ( $purpose === "reset_password" ){
    
        $statement = $connection->prepare( "UPDATE accounts SET password_hash = ? WHERE user_email = ?");
        $statement->bind_param( "ss", $passwordHash, $user_email );

        // Run the SQL
        $statement->execute();
    
    }
    else { throw new Exception( "Invalid certification purpose"); }

    // delete the verified and used data from auth_codes
    $statement = $connection->prepare( "DELETE FROM auth_codes WHERE user_email = ?");

    // get the data
    $statement->bind_param( "s", $user_email );

    //delete the data
    $statement->execute();

    // save in MySQL
    $connection->commit();
    
    if ($purpose === "create_account") {

        http_response_code(201);

        echo json_encode(["success" => true, "user_id" => $user_id, "user_name" => $user_name]);
    }
    else if ($purpose === "reset_password") {

        http_response_code(200);

        echo json_encode([ "success" => true, "message" => "Password reset successful"]);
    }

}// if the request have any problems turn to error, catch it

// SQL error
catch (mysqli_sql_exception $error) {

    // Cancel database changes
    if ($connection !== null) { $connection->rollback(); }
    // Detailed error only goes to Server log
    error_log($error->getMessage());
    // send to the Unity 
    // tell some things happened
    // 500 is means the request was run in error       
    // may the database have the coding problem
    http_response_code(500);

    echo json_encode([
        "success" => false,
        "message" => "Database error"
    ]);


}
// others error
catch (Throwable $error) {

    if ($connection !== null) { $connection->rollback(); }

    error_log($error->getMessage());
    // send to the Unity 
    // tell some things happened
    // 500 is means the request was run in error       
    http_response_code(500);

    echo json_encode([
        "success" => false,
        "message" => "Server error"
    ]);


}
// stop the connection in final
finally {

    if ($connection !== null) {
        $connection->close();
    }
}
