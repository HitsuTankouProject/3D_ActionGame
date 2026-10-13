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

$user_email= $data["account"] ?? "";
$user_id =$data["account"] ?? "";
$password = $data["password"] ?? "";

$check_user_id = ctype_digit((string)$user_id);
$check_user_email = filter_var($user_email, FILTER_VALIDATE_EMAIL);

if(!$check_user_id && !$check_user_email) {

    http_response_code(400);

    echo json_encode([
        "success" => false,
        "message" => "Invalid account or wrong password"
    ]);

    exit;
}

if($check_user_id) $user_id=(int)$user_id;

// connect to MySQL -> database
$connection = null;

try{

    // sign in to the database
    require "/var/www/unity-config/db.php";
    require_once "/var/www/html/unity-api/account-function.php";

    // checking the email exsite
    if($check_user_email){

        $statement = $connection->prepare( "SELECT user_id FROM accounts WHERE user_email = ?");
	    $statement->bind_param( "s", $user_email );
	    $statement->execute();
        $result = $statement->get_result();

	    if($result->num_rows !== 1) {

    		http_response_code(400);

    		echo json_encode([
        		    "success" => false,
            		"message" => "Invalid account or wrong password"
    		]);

    		exit;
	    }
        $user_id = $result->fetch_assoc()["user_id"];

    }

    $statement = $connection->prepare( "SELECT stage, password_hash FROM accounts WHERE user_id = ?");
    $statement->bind_param( "i", $user_id );
    $statement->execute();
    $result = $statement->get_result();

    if($result->num_rows !== 1) {

    	http_response_code(400);

    	echo json_encode([
        	    "success" => false,
            	"message" => "Invalid account or wrong password"
    	]);

    	exit;
    }

    $password_hash = $result->fetch_assoc()["password_hash"];

    // check the password
    if (!password_verify($password, $password_hash)) {

        http_response_code(401);

        echo json_encode([
            "success" => false,
            "message" => "Invalid account or wrong password"
        ]);

        exit;
    }

    if($result->fetch_assoc()["stage"] === "login" ){

        http_response_code(401);

        echo json_encode([
            "success" => false,
            "message" => "Account already LogIn"
        ]);

        exit;
    }


    $statement = $connection->prepare( "SELECT 1 FROM player_data WHERE user_id = ?");
    $statement->bind_param( "i", $user_id );
    $statement->execute();
    $result = $statement->get_result();

    if($result->num_rows !== 1) {

    	http_response_code(400);

    	echo json_encode([
        	    "success" => false,
            	"message" => "Player data not found"
    	]);

    	exit;
    }


    $connection->begin_transaction();

    $security_code = AccessAccount( $connection, $user_id);
    $access_code = AccessDataBase( $connection, $user_id);

    // save in MySQL
    $connection->commit();

    // return to Unity
    echo json_encode([
        "success" => true,
        "user_id" => $user_id,
        "security_code" => $security_code,
        "access_code" => $access_code
 
    ]);
}
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
