<?php
//Return to UTF-8 JSON format
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

// Read JSON data from Unity
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

if (!filter_var($user_email, FILTER_VALIDATE_EMAIL)) {
    
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
// Connect to MySQL -> database
$connection = null;

try{
    // sign in to the database
    require "/var/www/unity-config/db.php";
    require_once "/var/www/send-auth-mail.php";

    $statement = $connection->prepare( "SELECT 1 FROM accounts WHERE user_email = ?");
    $statement->bind_param( "s", $user_email );
    $statement->execute();

    $result = $statement->get_result();
    if($result->num_rows >= 1){
        // send to the Unity 
        // tell some things happened
        // 400 is means the result have somethings wrong
        http_response_code(400);

        echo json_encode(
        [
            "success" => false,
            "message" => "Email has been used"
        ]
        );
        exit;
    }

    // Remove expired authentication data
    $connection->query( "DELETE FROM auth_codes WHERE expires_at < NOW()");

    // create authentication code
    $authCode = (string) random_int( 100000, 999999);

    // set expiration time
    // Valid for 10 minutes
    $expirationMinutes = 10;
    $expiresAt = date("Y-m-d H:i:s",time() + ($expirationMinutes * 60));

    $verified = 0;

    // the purpose of certification
    $purpose = "create_account";

    // Start transaction
    $connection->begin_transaction();

    $statement = $connection->prepare( "SELECT 1 FROM auth_codes WHERE user_email = ?");
    $statement->bind_param( "s", $useremail );
    $statement->execute();

    $authExiste = $statement->get_result()->num_rows === 1;

    if($authExiste){

        $statement = $connection->prepare( 
            "UPDATE auth_codes 
            SET auth_code = ?, expires_at = ?, verified = ?, purpose = ?
            WHERE user_email = ?"
            );
    }
    else {
        
        $statement = $connection->prepare(
            "INSERT INTO auth_codes
            ( auth_code, expires_at, verified, purpose, user_email)
            VALUES (?, ?, ?, ?, ?)"
        );
    
    }

    $statement->bind_param( "ssiss", $authCode, $expiresAt, $verified, $purpose, $user_email );
    // Run the SQL
    $statement->execute();

    // Send email
    sendAuthMail( $user_email, $authCode, $purpose, $expirationMinutes);

    // Database + email successful
    $connection->commit();

    // Return result to Unity
    echo json_encode([
        "success" => true,
        "message" => "Authentication code created"
    ]);

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



