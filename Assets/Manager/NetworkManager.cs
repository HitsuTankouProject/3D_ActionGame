using Cysharp.Threading.Tasks;
using Fusion;

using System;
using System.Text;
using System.Threading;

using UnityEngine;
using UnityEngine.Networking;

using DataBase;
using WebSocketSharp;
using static DataBase.DatabaseConnectJson;
using Mono.Cecil.Cil;
using UnityEditor.PackageManager.Requests;



public class NetworkManager : MonoBehaviour
{
    // Photon Fusion Network Manager
    [SerializeField]
    private NetworkRunner networkRunner;
    private async UniTask ServerConnection()
    {
        StartGameResult result = await networkRunner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = "TestRoom"
        });

        if (result.Ok)
        {
            Debug.Log("Connected!");
        }
        else
        {
            Debug.LogError($"Failed: {result.ShutdownReason}");
        }
    }

    public void ConnectToServer() => ServerConnection().Forget();

    // Database Connection

    [SerializeField] private LoginData loginData = new();

    private CancellationTokenSource dataBaseAccess;

    private bool isDataBaseAccessing = false;

    private async UniTask<bool> IsDataBaseAccessSuccess(Func<CancellationToken, UniTask> access, float timeoutSeconds)
    {
        if (isDataBaseAccessing)
        {
            Debug.LogWarning("Database In Progress.");
            return false;
        }

        isDataBaseAccessing = true;

        dataBaseAccess?.Dispose();
        dataBaseAccess = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        dataBaseAccess.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await access(dataBaseAccess.Token);
            //Å@The Token's Task Run As Usually
            Debug.Log("Database Access Successful.");
            return true;
        }
        //Å@The Token's Task Closed As Non Usually
        catch (OperationCanceledException)
        {
            if (destroyCancellationToken.IsCancellationRequested)
                Debug.Log("Database Access Canceled Because The Object Was Destroyed.");
            else Debug.LogError("Database Access Timeout.");

            return false;

        }
        // Any Others Errors likes Network Error, JSON Error, etc. 
        catch (Exception exception)
        {
            Debug.LogError($"Database access failed: {exception.Message}");
            return false;

        }
        // Finally Block to Clean Up Resources
        finally
        {
            isDataBaseAccessing = false;

            dataBaseAccess?.Dispose();
            dataBaseAccess = null;

        }

    }

    #region Account 
    // 
    private async UniTask AccountApply(CancellationToken token, string email)
    {
        string json = AccountConnectJson.Json_AccountApply(email);
        string url = AccessConnectURL.ConnectURL(DatabaseConnectAction.AccountApply);

        //Connect start
        using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        byte[] body = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");

        // Can Able To Cancel The Request When Add The Token
        await request.SendWebRequest().ToUniTask(cancellationToken: token);

        //Connect end

        Debug.Log("HTTP Code: " + request.responseCode);
        Debug.Log("Response: " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success)
            throw new Exception($"Account build failed: {request.error}");

    }
    public async UniTask<bool> RequestAccountApply(string username)
    {
        float timeoutSeconds = 10f;
        return await IsDataBaseAccessSuccess(token => AccountApply(token, username), timeoutSeconds);
    }
    //
    private async UniTask<bool> AccountCertification(CancellationToken token, string email, string authCode)
    {
        string json = AccountConnectJson.Json_AccountCertification(email, authCode);
        string url = AccessConnectURL.ConnectURL(DatabaseConnectAction.AccountCertification);

        using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        byte[] body = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        await request.SendWebRequest().ToUniTask(cancellationToken: token);

        Debug.Log("HTTP Code: " + request.responseCode);
        Debug.Log("Response: " + request.downloadHandler.text);

        return true;
    }
    public async UniTask<bool> RequestAccountCertification(string email, string authCode)
    {
        float timeoutSeconds = 10f;
        return await IsDataBaseAccessSuccess(token => AccountCertification(token, email, authCode), timeoutSeconds); ;
    }
    //
    private async UniTask<bool> AccountCreate(CancellationToken token, string email, string name, string inputPassword)
    {
        string json = AccountConnectJson.Json_AccountCreate(email, name, inputPassword);
        string url = AccessConnectURL.ConnectURL(DatabaseConnectAction.AccountCreate);

        using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        byte[] body = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        await request.SendWebRequest().ToUniTask(cancellationToken: token);

        Debug.Log("HTTP Code: " + request.responseCode);
        Debug.Log("Response: " + request.downloadHandler.text);

        return true;
    }
    public async UniTask<bool> RequestAccountCreate(string email, string name, string inputPassword)
    {
        float timeoutSeconds = 10f;
        return await IsDataBaseAccessSuccess(token => AccountCreate(token, email, name, inputPassword), timeoutSeconds);
    }

    //
    private async UniTask<bool> AccountLogin(CancellationToken token, string account, string inputPassword)
    {
        string json = AccountConnectJson.Json_AccountLogin(account, inputPassword);
        string url = AccessConnectURL.ConnectURL(DatabaseConnectAction.AccountLogin);
        using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        byte[] body = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        await request.SendWebRequest().ToUniTask(cancellationToken: token);

        loginData = JsonUtility.FromJson<LoginData>(request.downloadHandler.text);

        return true;

    }
    public async UniTask<bool> RequestAccountLogin(string account, string inputPassword)
    {
        float timeoutSeconds = 10f;
        return await IsDataBaseAccessSuccess(token => AccountLogin(token, account, inputPassword), timeoutSeconds);
    }
    //
    private async UniTask<bool> AccountOut(CancellationToken token, string code)
    {
        string json = AccountConnectJson.Json_AccountLogout(code);
        string url = AccessConnectURL.ConnectURL(DatabaseConnectAction.AccountLogout);
        using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        byte[] body = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        await request.SendWebRequest().ToUniTask(cancellationToken: token);

        loginData = null;

        return true;

    }
    public async UniTask<bool> RequestAccountOut()
    {
        float timeoutSeconds = 10f;
        return await IsDataBaseAccessSuccess(token => AccountOut(token, loginData.security_code), timeoutSeconds);
    }

    #endregion

    #region Player data

    private async UniTask<bool> UpdateCharacterData(CancellationToken token, CharacterType name, CharacterStatus status)
    {
        string json = DatabaseConnectJson.Json_UpdateData_Character(loginData.access_code, loginData.security_code, name, status);
        string url = AccessConnectURL.ConnectURL(DatabaseConnectAction.DatabaseUpdate);
        using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        byte[] body = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        await request.SendWebRequest().ToUniTask(cancellationToken: token);

        GetPlayerData<string> resultData = JsonUtility.FromJson<GetPlayerData<string>>(request.downloadHandler.text);
        loginData.access_code = resultData.access_code;

        return true;
    }
    public async UniTask<bool> RequestUpdateCharacterData(CharacterType name, CharacterStatus status)
    {
        float timeoutSeconds = 10f;
        return await IsDataBaseAccessSuccess(token => UpdateCharacterData(token, name, status), timeoutSeconds);
    }

    private async UniTask<bool> UpdateBagData(CancellationToken token, PlayerItem[] changeItems)
    {
        string json = DatabaseConnectJson.Json_UpdateData_Bag(loginData.access_code, loginData.security_code, changeItems);
        string url = AccessConnectURL.ConnectURL(DatabaseConnectAction.DatabaseUpdate);
        using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        byte[] body = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        await request.SendWebRequest().ToUniTask(cancellationToken: token);

        GetPlayerData<string> resultData = JsonUtility.FromJson<GetPlayerData<string>>(request.downloadHandler.text);
        loginData.access_code = resultData.access_code;

        return true;
    }
    public async UniTask<bool> RequestUpdateBagData(PlayerItem[] changeItems)
    {
        float timeoutSeconds = 10f;
        return await IsDataBaseAccessSuccess(token => UpdateBagData(token, changeItems), timeoutSeconds);
    }

    private async UniTask<string> GetData(CancellationToken token, AllowedPurpose allowedPurpose)
    {
        
        string json = DatabaseConnectJson.Json_GetData(loginData.access_code, loginData.security_code, allowedPurpose);
        string url = AccessConnectURL.ConnectURL(DatabaseConnectAction.DatabaseGetData);
        using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        byte[] body = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        await request.SendWebRequest().ToUniTask(cancellationToken: token);

        if (request.result != UnityWebRequest.Result.Success) return null;

        GetPlayerData<string> resultData = JsonUtility.FromJson<GetPlayerData<string>>(request.downloadHandler.text);

        loginData.access_code = resultData.access_code;

        return request.downloadHandler.text;
    }
    private async UniTask<(bool isSuccess, string gotData)> RequestGetData(AllowedPurpose allowedPurpose)
    {
        float timeoutSeconds = 15f;
        string data = null;

        await IsDataBaseAccessSuccess(async token => { data = await GetData(token, allowedPurpose); }, timeoutSeconds);
        bool success = data != null;


        return (success, data);

    }

    public async UniTask<PlayerBag> GotData_Bag()
    {
        var result = await RequestGetData(AllowedPurpose.Bag);

        if(!result.isSuccess) return null;
        Debug.Log("RAW:");
        Debug.Log(result.gotData);

        GetPlayerData<PlayerBag> resultData = JsonUtility.FromJson<GetPlayerData<PlayerBag>>(result.gotData);
        
        Debug.Log("DESERIALIZED:");
        Debug.Log(JsonUtility.ToJson(resultData.get_data, true));
        if (!resultData.success) return null;

        return resultData.get_data;
    }

    public async UniTask<AllCharacterData> GotData_AllCharacter()
    {
        var result = await RequestGetData(AllowedPurpose.Character);
        if (!result.isSuccess) return null;
        GetPlayerData<AllCharacterData> resultData = JsonUtility.FromJson<GetPlayerData<AllCharacterData>>(result.gotData);

        if (!resultData.success) return null;

        return resultData.get_data;
    }





    #endregion 
}
