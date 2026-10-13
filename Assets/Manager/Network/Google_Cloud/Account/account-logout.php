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

$security_code = trim((string)($data["security_code"] ?? ""));
if ($security_code === "") {
    http_response_code(400);

    echo json_encode([
        "success" => false,
        "message" => "Security code is required"
    ]);

    exit;
}

// connect to MySQL -> database
$connection = null;

try{
    
    require "/var/www/unity-config/db.php";
    require_once "/var/www/html/unity-api/account-function.php";

    $security_code = Code_Decoding($security_code);

    $statement = $connection->prepare( "SELECT user_id, security_code, stage FROM accounts WHERE security_code IS NOT NULL");
    $statement->execute();
    $result = $statement->get_result();

    $user_id = null;

    while ($row = $result->fetch_assoc()) {

        if (password_verify($security_code, $row["security_code"])) {

            if($row["stage"] === "login") $user_id = (int)$row["user_id"];
            break;
        }
    }

    if($user_id === null){

        http_response_code(401);

        echo json_encode([
            "success" => false,
            "message" => "Cant Find Account or Account didnt login before"
        ]);

        exit;
    }

    // stop the auto save of the dataBase
    // ready to create a new account
    $connection->begin_transaction();
    LogoutDataBase($connection,$user_id);
    LogoutAccount($connection,$user_id);
    $connection->commit();

    // return to Unity
    echo json_encode([
        "success" => true,
        "message" => "Logout Success"
 
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
