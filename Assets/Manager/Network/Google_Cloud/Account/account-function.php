<?php
function Code_EnCoding(string $change_code) : string {

    $byteArray = unpack('C*', $change_code);
    $numberRules = 0x42;
    $new_code = "";

    foreach ($byteArray as $code)
    {
        $random_number = random_int(0, 9); 
        $newString = $code ^ $random_number;
        $random_number = $random_number ^ $numberRules;
        $new_code .= chr($newString);
        $new_code .= chr($random_number);
    }

    return $new_code;
}

function Code_Decoding(string $change_code) : string {

    $numberRules = 0x42;
    $new_code = "";
    $byteArray = array_values(unpack('C*', $change_code));
    $code_lenght = count($byteArray);

    if($code_lenght % 2 != 0) throw new Exception("Code Cant Decode");

    for ($i = 0; $i < $code_lenght; $i += 2){

        $code = $byteArray[$i];
        $random_index = $byteArray[$i+1];
        $random_index = $random_index ^ $numberRules;
        $code = $code ^ $random_index;

        $new_code .= chr($code);
    }

    return $new_code;
}



function AccessDataBase(mysqli $connection, int $user_id) : string{

    $access_code = base64_encode(random_bytes(16));
    $access_code_hash = password_hash( $access_code, PASSWORD_DEFAULT);
    $access_expires_at = date( "Y-m-d H:i:s", time() + 600);

    $statement = $connection->prepare( "UPDATE player_data SET access_code = ?, access_expires_at = ? WHERE user_id = ?");
    $statement->bind_param( "ssi", $access_code_hash, $access_expires_at, $user_id );
    $statement->execute();

    if ($statement->affected_rows !== 1) 
    throw new Exception("Failed to update database access");


    return Code_EnCoding($access_code);

}

function AccessAccount(mysqli $connection, int $user_id) : string{

    $stage = "login";
    $security_code = base64_encode(random_bytes(16));
    $security_code_hash = password_hash( $security_code, PASSWORD_DEFAULT);
    $statement = $connection->prepare( "UPDATE accounts SET stage = ?, security_code = ? WHERE user_id = ?");
    $statement->bind_param( "ssi", $stage, $security_code_hash, $user_id );
    $statement->execute();

    if ($statement->affected_rows !== 1) {
    throw new Exception("Failed to update account access");}

    return Code_EnCoding($security_code);
}

function LogoutDataBase(mysqli $connection, int $user_id) : void{

    $statement = $connection->prepare( "UPDATE player_data SET access_code = NULL,access_expires_at = NULL WHERE user_id = ?");
    $statement->bind_param( "i", $user_id );
    $statement->execute();
}

function LogoutAccount(mysqli $connection, int $user_id) : void{

    $stage = "logout";
    $statement = $connection->prepare( "UPDATE accounts SET stage = ?, security_code = NULL WHERE user_id = ?");
    $statement->bind_param( "si", $stage, $user_id );
    $statement->execute();
}
