using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using TMPro;
public static class DataBaseConnectURL
{
    public enum URLType
    {
        AccountBuild,
        AccountCertification,
        AccountPassword,

        AccountLogin,
        AccountLogout,

        AccountResetPassword
    }

    private static string account_buildURL = "http://35.233.145.121/unity-api/account-build.php";
    private static string account_certificationURL = "http://35.233.145.121/unity-api/account-certification.php";
    private static string account_passwordURL = "http://35.233.145.121/unity-api/account-password.php";

    private static string account_loginURL = "http://35.233.145.121/unity-api/account-login.php";
    private static string account_logoutURL = "http://35.233.145.121/unity-api/account-logout.php";

    private static string account_resetPasswordURL = "http://35.233.145.121/unity-api/account-resetPassword.php";

    public static string GetURL(URLType urlType)
    {
        return urlType switch
        {
            URLType.AccountBuild => account_buildURL,
            URLType.AccountCertification => account_certificationURL,
            URLType.AccountPassword => account_passwordURL,

            URLType.AccountLogin => account_loginURL,
            URLType.AccountLogout => account_logoutURL,

            URLType.AccountResetPassword => account_resetPasswordURL,

            _ => throw new ArgumentOutOfRangeException(nameof(urlType), urlType, null)
        };
    }

}


public class DataBaseConnecter : MonoBehaviour
{
    public TMP_InputField name_InputField;
    [System.Serializable] private class AccountBuildRequest 
    { 
        public string username;

        public AccountBuildRequest(string username) { this.username = username; }
    }

    public TMP_InputField uid_InputField;
    public TMP_InputField authCode_InputField;
    [System.Serializable]
    private class AccountCertificationRequest
    {
        public int player_uid;
        public string auth_code;
        public AccountCertificationRequest(int playerUid, string authCode)
        {
            player_uid = playerUid;
            auth_code = authCode;
        }
    }

    public TMP_InputField password_InputField;
    [System.Serializable] private class AccountPasswordRequest 
    { 
        public int player_uid; public string password;
    
        public AccountPasswordRequest(int playerUid, string player_password) { 
            player_uid = playerUid;
            password = player_password;
        }
    }

    private async UniTask AccountBuild()
    {
        AccountBuildRequest requestData = new (name_InputField.text);
        string json = JsonUtility.ToJson(requestData);
        string url = DataBaseConnectURL.GetURL(DataBaseConnectURL.URLType.AccountBuild);

        using UnityWebRequest request = new UnityWebRequest( url, UnityWebRequest.kHttpVerbPOST);
        byte[] body = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        await request.SendWebRequest();

        Debug.Log("HTTP Code: " + request.responseCode);
        Debug.Log("Response: " + request.downloadHandler.text);


        if (request.result != UnityWebRequest.Result.Success) Debug.LogError(request.error);
    }
    public void CreateAccount() => AccountBuild().Forget();

    private async UniTask AccountCertification()
    {
        if (!int.TryParse(uid_InputField.text, out int playerUid))
        {
            Debug.LogError("Invalid UID");
            return;
        }
        AccountCertificationRequest requestData = new(playerUid, authCode_InputField.text);

        string json = JsonUtility.ToJson(requestData);
        Debug.Log("Send JSON: " + json);

        string url = DataBaseConnectURL.GetURL(DataBaseConnectURL.URLType.AccountCertification);

        using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        byte[] body = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        await request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            return;
        }

        uid = playerUid;



        Debug.Log("HTTP Code: " + request.responseCode);
        Debug.Log("Response: " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success) { Debug.LogError(request.error); return; }
    }
    public void Certification() => AccountCertification().Forget();

    public int uid = 0;
    private async UniTask AccountPassword()
    {
        string password = password_InputField.text;

        if (string.IsNullOrWhiteSpace(password))
        {
            Debug.LogError("Password is empty");
            return;
        }

        AccountPasswordRequest requestData = new(uid, password_InputField.text);

        string json = JsonUtility.ToJson(requestData);
        string url = DataBaseConnectURL.GetURL(DataBaseConnectURL.URLType.AccountPassword);

        using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        byte[] body = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        await request.SendWebRequest();

        Debug.Log("HTTP Code: " + request.responseCode);
        Debug.Log("Response: " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success) { Debug.LogError(request.error); return; }
    }

    public void SetupPassword() => AccountPassword().Forget();

}