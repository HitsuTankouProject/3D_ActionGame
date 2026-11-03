<?php
// Build The Default Data
function BuildDefaultPlayerData(mysqli $connection, int $user_id): void
{
    // Default character data
    $character_data = json_encode([  
        "Adventurer" => [
            "Lv" => 1,
            "LevelPoint" => 0,
            "HpLv" => 1,
            "DefLv" => 1,
            "AtkLv" => 1,
            "PassiveLv" => 1,
            "ActiveLv" => 1,
            "UltLv" => 1
        ],

        "Magician" => [
            "Lv" => 1,
            "LevelPoint" => 0,
            "HpLv" => 1,
            "DefLv" => 1,
            "AtkLv" => 1,
            "PassiveLv" => 1,
            "ActiveLv" => 1,
            "UltLv" => 1
        ],

        "Thief" => [
            "Lv" => 1,
            "LevelPoint" => 0,
            "HpLv" => 1,
            "DefLv" => 1,
            "AtkLv" => 1,
            "PassiveLv" => 1,
            "ActiveLv" => 1,
            "UltLv" => 1
        ],

        "Warrior" => [
            "Lv" => 1,
            "LevelPoint" => 0,
            "HpLv" => 1,
            "DefLv" => 1,
            "AtkLv" => 1,
            "PassiveLv" => 1,
            "ActiveLv" => 1,
            "UltLv" => 1
        ]
    ]);

 // Default bag data
    $bag_data = json_encode([
        "Items" => [
            [
                "item_code" => "Sword_1_1",
                "item_numbers" => 1
            ],
            [
                "item_code" => "Shield_1_1",
                "item_numbers" => 1
            ]
        ]
    ]);
    // Save player data
    $statement = $connection->prepare(
        "INSERT INTO player_data
        (user_id, character_data, bag_data)
        VALUES (?, ?, ?)"
    );

    // Create Default Player's Data
    $statement->bind_param( "iss", $user_id, $character_data, $bag_data );

    $statement->execute();

} 
// Checking the Account can be access by the security_code
function CanAccessAccount(mysqli $connection, string $security_code): array{

    $return_result =
    [
        "canAccessAccount" => false,
        "user_id" => NULL,
        "reason" => "Cannot Find The Acount"
    ];

    $statement = $connection->prepare( "SELECT user_id, security_code, stage, update_at FROM accounts WHERE security_code IS NOT NULL");
    $statement->execute();
    $result = $statement->get_result();

    
    while ($row = $result->fetch_assoc()) {

        if (password_verify($security_code, $row["security_code"])) {

            $return_result["user_id"] = $row["user_id"];

            $database_time = time();
            $account_update_at = strtotime($row["update_at"]);
            if($row["stage"] !== "login" )  $return_result["reason"] = "Acount haven't login";
            else if(($database_time - $account_update_at) >= 86400) $return_result["reason"] = "Acount Access Time out";
            else {

                $return_result["canAccessAccount"] = true;
                $return_result["reason"] = "Account Assess Success";
            }
            break;
        }
    }

    return $return_result;
}
// Checking the dasebase can be access by the access_code
function CanAssessDataBase(mysqli $connection, string $access_code): array{

    $return_result =
    [
        "canAccessDataBase" => false,
        "user_id" => NULL,
        "reason" => "Cannot Find The Database"
    ];


    $statement = $connection->prepare( "SELECT user_id, access_code, access_expires_at FROM player_data WHERE access_code IS NOT NULL");
    $statement->execute();
    $result = $statement->get_result();

    while ($row = $result->fetch_assoc()) {

        if (password_verify($access_code, $row["access_code"])) {
            
            $return_result["user_id"] = $row["user_id"];
            $database_time = time();
            if($database_time > strtotime($row["access_expires_at"])){

                $return_result["reason"] = "DataBase Access_code was expired";
            }
            else {

                $return_result["canAccessDataBate"] = true;
                $return_result["reason"] = "DataBase Assess Success";
            }
            break;
        }
    }

    return $return_result;

}

// Checking the formal is correct for the characters_data update
function CheckData_Characters(array $data) : bool {

    $character_name = $data["character_name"] ?? "";
    $character_status = $data["character_status"] ?? [];

    $allowed_characters = [ "Adventurer", "Magician", "Thief", "Warrior" ];

    if (!in_array($character_name, $allowed_characters, true)) return false;

    // The Data Should From Like That
    $character_status_rules = [
        "Lv"         => ["min" => 1, "max" => 99],
        "LevelPoint" => ["min" => 0, "max" => 99],
        "HpLv"       => ["min" => 1, "max" => 99],
        "DefLv"      => ["min" => 1, "max" => 99],
        "AtkLv"      => ["min" => 1, "max" => 99],
        "PassiveLv"  => ["min" => 1, "max" => 99],
        "ActiveLv"   => ["min" => 1, "max" => 99],
        "UltLv"      => ["min" => 1, "max" => 99]
    ];
    // Checking the Data's size were the same as the rules or not
    if (count($character_status) !== count($character_status_rules)) return false;

    foreach ($character_status_rules as $status_name => $rule) {

        if (!array_key_exists($status_name, $character_status)) return false;

        $value = $character_status[$status_name];

        if (!is_int($value)) return false;

        if ($value < $rule["min"] || $value > $rule["max"]) return false;
    }
    
    return true;

}

function IsAcceptItemData(array $item) : bool {

    $item_rules = [
        "Sword" => [
            "rare_min" => 1,
            "rare_max" => 5,
            "id_min"   => 1,
            "id_max"   => 42
        ],
        "Shield" => [
            "rare_min" => 1,
            "rare_max" => 5,
            "id_min"   => 1,
            "id_max"   => 42
        ],
        "Staff" => [
            "rare_min" => 1,
            "rare_max" => 3,
            "id_min"   => 1,
            "id_max"   => 42
        ],
        "Knife" => [
            "rare_min" => 1,
            "rare_max" => 3,
            "id_min"   => 1,
            "id_max"   => 42
        ],
        "Greatsword" => [
            "rare_min" => 1,
            "rare_max" => 3,
            "id_min"   => 1,
            "id_max"   => 42
        ],
        "AlchemyStone" => [
            "rare_min" => 1,
            "rare_max" => 3,
            "id_min"   => 1,
            "id_max"   => 42
        ]
    ];
    if (!isset($item["item_code"])  || !isset($item["item_numbers"]) ) return false;

    $item_code = $item["item_code"];
    $item_numbers = $item["item_numbers"];

    // Checking the Item Code
    // whatIsThis_rare_code
    $parts = explode("_", $item_code);
    if (count($parts) !== 3) return false;
    [$item_type, $rare_code, $item_id] = $parts;
    if (!isset($item_rules[$item_type])) return false;

    $rule = $item_rules[$item_type];
    if (!ctype_digit($rare_code) || !ctype_digit($item_id)) return false;
    $rare = (int)$rare_code;
    $id = (int)$item_id;

    if (  $rare < $rule["rare_min"] ||  $rare > $rule["rare_max"]
       || $id < $rule["id_min"]     ||  $id > $rule["id_max"]) 
       return false;

    if (!is_int($item_numbers)) return false;
    $amount = abs($item_numbers);

    if ($amount > 42) return false;

    return true;

}

// Checking the formal is correct for the bag_data update
function CheckData_Bag(array $data): bool{
    
    if(!array_key_exists("change_items", $data))  return false;
    $change_items = $data["change_items"];
    if (!is_array($change_items)) return false;
    if (count($change_items) === 0) return false;

    foreach ($change_items as $item) {

        if (!is_array($item)) return false;
        if (!IsAcceptItemData($item)) return false;
    }

    return true;
}

//Update the characters_data
function UpdateData_Character(mysqli $connection,int $user_id, array $data) : void{

    $statement = $connection->prepare( "SELECT character_data FROM player_data WHERE user_id = ?" ); 
    $statement->bind_param( "i", $user_id );
    $statement->execute();

    $result = $statement->get_result();

    // if no character_data
    // It should created in account build
    if ($result->num_rows !== 1) throw new Exception("Player data not found");

    $row = $result->fetch_assoc();
    $character_data = json_decode($row["character_data"], true);
    //if Cant turn to json
    if (!is_array($character_data)) throw new Exception("Invalid characters data");

    $character_name = $data["character_name"] ?? "";
    $character_status = $data["character_status"] ?? [];

    $json_path = '$.' . $character_name;
    $character_status_json = json_encode($character_status);

    $statement = $connection->prepare(
        "UPDATE player_data
         SET character_data = JSON_SET( character_data, ?, JSON_EXTRACT(?, '$') )
         WHERE user_id = ?"
    );

    $statement->bind_param( "ssi", $json_path, $character_status_json, $user_id );

    $statement->execute();
}

//Update the bag_data
function UpdateData_Bag(mysqli $connection,int $user_id, array $data) : void{

    $statement = $connection->prepare( "SELECT bag_data FROM player_data WHERE user_id = ?" ); 
    $statement->bind_param( "i", $user_id );
    $statement->execute();

    $result = $statement->get_result();
    // if no bag_data
    // It should created in account build
    if ($result->num_rows !== 1) throw new Exception("Player data not found");

    $row = $result->fetch_assoc();
    $bag_data = json_decode($row["bag_data"], true);
    //if Cant turn to json
    if ( !isset($bag_data["items"]) || !is_array($bag_data)) throw new Exception("Invalid bag data");

    $change_items = $data["change_items"];

    foreach ($change_items as $change_item){
    
        $change_item_code = $change_item["item_code"];
        $change_item_numbers = $change_item["item_numbers"];

        $item_index = null;

        foreach ($bag_data["items"] as $index => $item) {

            if ($item["item_code"] === $change_item_code) {
                $item_index = $index;
                break;
            }
        }

        if ($item_index !== null) {

            $current_amount = $bag_data["items"][$item_index]["item_numbers"];

            $new_amount = $current_amount + $change_item_numbers;

            if ($new_amount < 0) throw new Exception( "Not enough item: " . $item_code);

            $bag_data["items"][$item_index]["item_numbers"] = $new_amount;
        }
        else {

            if ($change_item_numbers <= 0) throw new Exception("Cannot reduce non-existing item: " . $change_item_code);

            $bag_data["items"][] = 
            [
                "item_code" => $change_item_code,
                "item_numbers" => $change_item_numbers
            ];
        }
    }

    $new_bag_data = json_encode($bag_data);
    if ($new_bag_data === false) throw new Exception("Bag JSON encode failed");

    $statement = $connection->prepare( "UPDATE player_data SET bag_data = ? WHERE user_id = ?" );
    $statement->bind_param( "si", $new_bag_data, $user_id );

    $statement->execute();
}