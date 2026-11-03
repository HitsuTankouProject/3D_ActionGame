<?php

require_once "/var/www/html/unity-api/account-function.php";
require_once "/var/www/html/unity-api/database-function.php";

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

$access_code = $data["access_code"] ?? "";
$security_code = $data["security_code"] ?? "";
$purpose = $data["purpose"] ?? "";
$update_data = $data["update_data"] ?? [];


$allowed_purpose = [ "Character", "Bag" ];
if (!is_array($update_data)) {

    http_response_code(400);

    echo json_encode([
        "success" => false,
        "message" => "Invalid update data"
    ]);

    exit;
}

$data_allow = false;
if($purpose === "Character") $data_allow = CheckData_Characters($update_data);
else if ($purpose === "Bag") $data_allow = CheckData_Bag($update_data);

if(!$data_allow){

    http_response_code(400);

    echo json_encode([
        "success" => false,
        "message" => "No Allow Data"
    ]);

    exit;
}

$connection = null;

try{

    require "/var/www/unity-config/db.php";
    require_once "/var/www/html/unity-api/account-function.php";

    $access_code = Code_Decoding($access_code);


    $user_id = null;
    $new_access_code = "";

    $connection->begin_transaction();

    $check_access_database = CanAccessDataBase($connection, $access_code);
    if(!$check_access_database["canAccessDataBase"]){
        
        $security_code = Code_Decoding($security_code);

        $check_access_account = CanAccessAccount($connection, $security_code);
        if(!$check_access_account["canAccessAccount"]){


            if($check_access_account["user_id"] === NULL){
                
                $connection->rollback();

                http_response_code(401);

                echo json_encode([
                    "success" => false,
                    "message" => "How????(access_code and security_code wrong)and still want to accect"
                ]);

                exit;
            }

            LogoutDataBase($connection,$check_access_account["user_id"]);
            LogoutAccount($connection, $check_access_account["user_id"]);

            $connection->commit();

            http_response_code(401);

            echo json_encode([
                "success" => false,
                "message" => " The Account login Time Over Then 24h, already auto Logout"
            ]);

            exit;
        }

        $user_id = $check_access_account["user_id"];
        $new_access_code = AccessDataBase($connection, $user_id);
    }
    else $user_id = $check_access_database["user_id"];

    if($purpose === "Character") UpdateData_Character($connection, $user_id, $update_data);
    else if ($purpose === "Bag") UpdateData_Bag($connection, $user_id, $update_data);

    $connection->commit();

    // return to Unity
    echo json_encode([
        "success" => true,
        "message" => "database update Success",
        "access_code" => $new_access_code
    ]);


}// SQL error
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
