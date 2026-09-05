using Cysharp.Threading.Tasks;
using Fusion;

using System;
using System.Text;
using System.Threading;

using UnityEngine;
using UnityEngine.Networking;

using DataBase;
using static DataBase.DatabaseConnectJson;

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

    [SerializeField] private LoginData loginData = new();

    private CancellationTokenSource dataBaseAccess;

    private bool isDataBaseAccessing = false;
    private async UniTask<bool> IsDataBaseAccessSuccess(Func<CancellationToken, UniTask> access, int tryCount = 10)
    {
        if (isDataBaseAccessing)
        {
            Debug.LogWarning("Database access is already in progress.");
            return false;
        }
        isDataBaseAccessing = true;

        try
        {
            dataBaseAccess = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);

            for (int attempt = 1; attempt <= tryCount; attempt++)
            {
                try
                {
                    dataBaseAccess.Token.ThrowIfCancellationRequested();

                    await access(dataBaseAccess.Token);

                    Debug.Log("Database access successful.");
                    return true;
                }
                catch (OperationCanceledException)
                {
                    Debug.LogWarning("Database access canceled.");
                    return false;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Attempt {attempt}/{tryCount} failed: {exception.Message}");
                }
            }

            return false;
        }
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
        return await IsDataBaseAccessSuccess(token => AccountApply(token, username));
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
        return await IsDataBaseAccessSuccess(token => AccountCertification(token, email, authCode));
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
        return await IsDataBaseAccessSuccess(token => AccountCreate(token, email, name, inputPassword));
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
        return await IsDataBaseAccessSuccess(token => AccountLogin(token, account, inputPassword));
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
        return await IsDataBaseAccessSuccess(token => AccountOut(token, loginData.security_code));
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
        return await IsDataBaseAccessSuccess(token => UpdateCharacterData(token, name, status));
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
        return await IsDataBaseAccessSuccess(token => UpdateBagData(token, changeItems));
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
        string data = null;
        await IsDataBaseAccessSuccess(async token => { data = await GetData(token, allowedPurpose); }, 15);
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
