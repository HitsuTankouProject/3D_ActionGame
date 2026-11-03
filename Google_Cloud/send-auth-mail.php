<?php

use PHPMailer\PHPMailer\PHPMailer;

require_once "/var/www/vendor/autoload.php";
require_once "/var/www/unity-config/mail.php";

function sendAuthMail( string $useremail, string $authCode, string $purpose, int $expiration_time
): void {

    global $mailHost;
    global $mailUsername;
    global $mailPassword;
    global $mailPort;

    $mail = new PHPMailer(true);

    $mail->isSMTP();

    $mail->Host = $mailHost;
    $mail->SMTPAuth = true;

    $mail->Username = $mailUsername;
    $mail->Password = $mailPassword;
    $mail->SMTPSecure = PHPMailer::ENCRYPTION_STARTTLS;

    $mail->Port = $mailPort;

    $mail->setFrom( $mailUsername,  "Unity Games Certification" );

    // Currently send to email
    $mail->addAddress( $useremail );

    $mail->Subject = "Account Authentication";

    $mail->Body =
        "Yours Authentication Code: " . $authCode . "\n\n" .
        "Purpose: " . $purpose . "\n" .
        "This code will expire in ".$expiration_time . " minutes.";

    $mail->send();
}
