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

// Get username
$user_email = $data["user_email"] ?? "";
$auth_code = $data["auth_code"] ?? "";

if (!filter_var($useremail, FILTER_VALIDATE_EMAIL)
    ||! preg_match('/^\d{6}$/', $auth_code)) {
    
    // send to the Unity 
    // tell some things happened
    // 400 is means the result have somethings wrong
    http_response_code(400);

    echo json_encode(
    [
        "success" => false,
        "message" => "Email or auth_code Worng"
    ]
    );

    exit;
}

// connect to MySQL -> database
$connection = null;

try {

    // sign in to the database
    require "/var/www/unity-config/db.php";

    // find authentication data by player_uid
    // SELECT => witch data I want 
    // FROM => the tatget database's table
    // WHERE => the condition used to find the target row
    $statement = $connection->prepare( "SELECT auth_code, expires_at FROM auth_codes WHERE user_email = ?");

    // i = int
    // white the uid 
    $statement->bind_param( "s", $user_email );

    // run SQL
    $statement->execute();

    // get SQL result
    // SELECT
    $result = $statement->get_result();

    // check whether useremail exists
    // from the SQL result
    // should be 1 and only 1
    if ($result->num_rows !== 1) {

        http_response_code(401);

        echo json_encode([
            "success" => false,
            "message" => "Invalid email or auth_code"
        ]);

        exit;
    }

    // get authentication data
    // the auth code
    $authData = $result->fetch_assoc();

    // checking the auth code is right or not
    // checking the auth code was expired or not
    if ($auth_code !== $authData["auth_code"]
        || strtotime($authData["expires_at"]) <= time()) {

        http_response_code(401);

        echo json_encode([
            "success" => false,
            "message" => "Invalid auth_code or expired"
        ]);

        exit;
    }
     
    // authentication passed
    // set verified to 1
    // tell the database that id passed the authentication
    $statement = $connection->prepare( "UPDATE auth_codes SET verified = 1 WHERE user_email = ?");

    $statement->bind_param("s", $user_email);

    $statement->execute();

    // authentication successful
    echo json_encode([
        "success" => true,
        "message" => "Authentication successful"
    ]);
}
// if the request have any problems turn to error, catch it

// SQL error
catch (mysqli_sql_exception $error) {

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
