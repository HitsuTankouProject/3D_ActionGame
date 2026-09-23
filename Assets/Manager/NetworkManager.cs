using Cysharp.Threading.Tasks;
using DataBase;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using static DataBase.DatabaseConnectJson;

public enum RoomMode : uint { Single = 1 , Multiplayer = 2 }
public enum NetworkMode { Offline, Online } 
public enum LogMode { Login, Logout }
public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        if (networkRunner == null)
        {
            Debug.LogError(" NetworkManager [networkRunner == null]");
            return;
        }

        if (networkSceneManager == null)
        {
            Debug.LogError(" NetworkManager [networkSceneManager == null]");
            return;

        }

        networkRunner.AddCallbacks(this);
    }

    public bool HasNetwork() => Application.internetReachability != NetworkReachability.NotReachable;
    public async UniTask<bool> TryConnectToLobby()
    {
        if (networkRunner == null)
        {
            Debug.LogError("NetworkRunner is not assigned.", this);
            return false;
        }

        if (networkSceneManager == null)
        {
            Debug.LogError("NetworkSceneManagerDefault is not assigned.", this);
            return false;
        }

        if (networkRunner.IsRunning)
        {
            bool isInLobby = networkRunner.LobbyInfo.IsValid;

            if (isInLobby) Debug.LogWarning("Already connected to Photon Lobby.", this);
            else Debug.LogWarning("NetworkRunner is already running.", this);
            return isInLobby;
        }

        var result = await networkRunner.JoinSessionLobby(SessionLobby.Shared);

        if (!result.Ok)
        {
            Debug.LogError($"Photon Lobbyへの接続に失敗しました。\n Shutdown Reason：{result.ShutdownReason}", this);
            return false;
        }

        Debug.Log("Photon Shared Lobbyへの接続に成功しました。", this);
        return true;
    }

    public NetworkMode networkMode { get; private set; } = NetworkMode.Offline;
    public async UniTask TestNetworkConnection()
    {
        bool hasNetwork = HasNetwork();
        bool canConnectToLobby = await TryConnectToLobby();
        if(hasNetwork && canConnectToLobby) networkMode = NetworkMode.Online;
        else networkMode = NetworkMode.Offline;
    }

    #region Create Room

    private bool CheckStartGameResult(StartGameResult result, string targetName)
    {
        if (!result.Ok)
        {
            Debug.LogError($"{targetName}の開始に失敗しました。\n Shutdown Reason：{result.ShutdownReason}\n Error Message：{result.ErrorMessage}", this);
            return false;
        }
        Debug.Log($"{targetName}の開始に成功しました。", this);
        return true;
    }

    private async UniTask<bool> StartOfflineRoom()
    {
        StartGameResult result = await networkRunner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Single,
                SceneManager = networkSceneManager,
                ObjectProvider = PoolManager.Instance

        });

        return CheckStartGameResult(result, "[Offline Room]");
    }

    private async UniTask<bool> StartOnlineRoom(string sessionName, uint playerLimit, bool isVisible)
    {
        StartGameResult result = await networkRunner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = sessionName,
                PlayerCount = (int)playerLimit,
                IsOpen = true,
                IsVisible = isVisible,
                SceneManager = networkSceneManager,
                ObjectProvider = PoolManager.Instance


        });


        return CheckStartGameResult(result, "[Online Room]");
    }


    #endregion


    #region Server

    [Header("Network")]
    [SerializeField] private NetworkRunner networkRunner;
    public NetworkRunner runner => networkRunner;
    [SerializeField] private NetworkSceneManagerDefault networkSceneManager;

    [Header("Scene")]
    private readonly List<SessionInfo> sessionList = new();

    private bool isMatchmaking;
    private bool isLoadingGameScene;


    #region Room Matchmaking
    /// <summary>
    /// 選択されたゲームモードでマッチングを開始する。
    /// </summary>
    public async UniTask<bool> StartMatchmaking(RoomMode roomMode)
    {
        if (isMatchmaking)
        {
            Debug.LogWarning("Matchmaking is already running.", this);
            return false;
        }

        if (!networkRunner.LobbyInfo.IsValid)
        {
            Debug.LogError("Photon Lobbyに接続していません。", this);

            return false;
        }

        isMatchmaking = true;

        try
        {
            if (networkMode == NetworkMode.Offline) return await StartOfflineRoom();

            switch (roomMode)
            {
                case RoomMode.Single:

                    string roomName = $"Single_{Guid.NewGuid():N}";
                    return await StartOnlineRoom(roomName, (uint)roomMode, false);

                case RoomMode.Multiplayer: return await JoinOrCreateMultiplayerRoom((uint)roomMode);

                default:
                    Debug.LogError($"Unsupported game mode: {roomMode}", this);
                    return false;
            }
        }
        finally { isMatchmaking = false; }
    }

    private async UniTask<bool> JoinOrCreateMultiplayerRoom(uint playerLimit)
    {
        if (playerLimit < 2)
        {
            Debug.LogError("Multiplayer requires at least two players.", this);

            return false;
        }

        SessionInfo availableRoom = FindAvailableRoom(playerLimit);

        if (availableRoom != null)
        {
            Debug.Log($"参加可能なRoomを発見しました：{availableRoom.Name}", this);

            return await JoinRoom(availableRoom.Name);
        }

        string roomName = $"Multi_{playerLimit}P_{Guid.NewGuid():N}";

        Debug.Log("参加可能なRoomが存在しないため、新しいRoomを作成します。", this);

        return await StartOnlineRoom(roomName, playerLimit, true);
    }

    private SessionInfo FindAvailableRoom( uint playerLimit)
    {
        foreach (SessionInfo sessionInfo in sessionList)
        {
            bool isCorrectPlayerLimit = sessionInfo.MaxPlayers == (int)playerLimit;

            bool hasAvailableSpace = sessionInfo.PlayerCount < sessionInfo.MaxPlayers;

            if (sessionInfo.IsOpen && sessionInfo.IsVisible && isCorrectPlayerLimit && hasAvailableSpace)
            {
                return sessionInfo;
            }
        }

        return null;
    }

    private async UniTask<bool> JoinRoom(string sessionName)
    {
        StartGameResult result = await networkRunner.StartGame(
                new StartGameArgs
                {
                    GameMode = GameMode.Shared,
                    SessionName = sessionName,
                    SceneManager = networkSceneManager,
                    ObjectProvider = PoolManager.Instance

                });

        if (!result.Ok)
        {
            Debug.LogError($"Roomへの参加に失敗しました：{sessionName}\nShutdown Reason：{result.ShutdownReason}", this);

            return false;
        }

        Debug.Log($"Roomへの参加に成功しました：{sessionName}", this);

        return true;
    }


    #endregion

    private bool TryGetSceneRef(GameScene sceneName, out SceneRef sceneRef)
    {
        int sceneAmount = SceneManager.sceneCountInBuildSettings;

        for (int i = 0; i < sceneAmount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);

            string registeredSceneName = Path.GetFileNameWithoutExtension(scenePath);

            if (registeredSceneName == sceneName.ToString())
            {
                sceneRef = SceneRef.FromIndex(i);
                return true;
            }
        }

        Debug.LogError($"Scene '{sceneName}' is not registered in the Build Profile.");

        sceneRef = default;
        return false;
    }

    private async UniTask LoadScene(GameScene gameScene)
    {
        if (!TryGetSceneRef(gameScene, out SceneRef sceneRef))
        {
            return;
        }

        if (gameScene == GameScene.InGame)
        {
            if (networkRunner == null || !networkRunner.IsRunning)
            {
                Debug.LogError("無法載入 InGame：NetworkRunner 尚未啟動。", this);
                return;
            }

            if (!networkRunner.IsSceneAuthority)
            {
                Debug.LogWarning("只有 Scene Authority 可以載入 InGame。", this);
                return;
            }

            isLoadingGameScene = true;

            try
            {
                if (networkMode == NetworkMode.Online &&
                    networkRunner.SessionInfo.IsValid)
                {
                    networkRunner.SessionInfo.IsOpen = false;
                    networkRunner.SessionInfo.IsVisible = false;
                }

                await networkRunner.LoadScene(sceneRef, LoadSceneMode.Single);
            }
            finally
            {
                isLoadingGameScene = false;
            }

            return;
        }

        await SceneManager.LoadSceneAsync(sceneRef.AsIndex, LoadSceneMode.Single);
    }

    public void TryGoGameTitle()
    {
        LoadScene(GameScene.GameTitle).Forget();

    }
    public void TryGoLobby()
    {
        LoadScene(GameScene.Lobby).Forget();

    }

    private void TryStartGame()
    {
        if (isLoadingGameScene) return;

        if (!networkRunner.IsSceneAuthority) return;

        if (networkRunner.SessionInfo.PlayerCount != networkRunner.SessionInfo.MaxPlayers) return;

        LoadScene(GameScene.InGame).Forget();

    }

    #region Player Callbacks

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player joined：{player}\n Players：{runner.SessionInfo.PlayerCount}/ {runner.SessionInfo.MaxPlayers}", this);
        TryStartGame();
    }

    public void OnPlayerLeft( NetworkRunner runner, PlayerRef player) => Debug.Log($"Player left：{player}", this);

    public void OnSessionListUpdated( NetworkRunner runner, List<SessionInfo> updatedSessionList)
    {
        sessionList.Clear();
        sessionList.AddRange(updatedSessionList);

        foreach (SessionInfo sessionInfo in sessionList)
            Debug.Log($"Room：{sessionInfo.Name}, Players：{sessionInfo.PlayerCount}/{sessionInfo.MaxPlayers}, Open：{sessionInfo.IsOpen}", this);
    }
    #endregion

    #region Other Fusion Callbacks

    public void OnConnectedToServer(NetworkRunner runner) => Debug.Log("Connected to Photon Server.", this);
    public void OnDisconnectedFromServer( NetworkRunner runner, NetDisconnectReason reason) => Debug.LogWarning($"Disconnected from Photon Server：{reason}", this);

    public void OnShutdown(NetworkRunner runner,ShutdownReason shutdownReason)
    {
        Debug.LogWarning($"NetworkRunner shutdown：{shutdownReason}", this);

        sessionList.Clear();
        isMatchmaking = false;
        isLoadingGameScene = false;
    }

    public void OnSceneLoadStart(NetworkRunner runner) => Debug.Log("Network Scene loading started.", this);

    public void OnSceneLoadDone(NetworkRunner runner) => Debug.Log("Network Scene loading completed.", this);
    public void OnInput(NetworkRunner runner,NetworkInput input) { }

    public void OnInputMissing( NetworkRunner runner,PlayerRef player,NetworkInput input) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) => request.Accept();

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) => Debug.LogError($"Connection failed：{reason}", this);

    public void OnCustomAuthenticationResponse(NetworkRunner runner,Dictionary<string, object> data) { }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey reliableKey, ReadOnlySpan<byte> data) { }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey reliableKey, float progress) { }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject networkObject, PlayerRef player) { }

    public void OnObjectExitAOI(NetworkRunner runner,NetworkObject networkObject,PlayerRef player) { }





    #endregion

    #endregion

    #region Data Base

    [Header("Data Base")]
    private LoginData loginData;
    public LogMode logMode => loginData == null ? LogMode.Logout : LogMode.Login;

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

    private async UniTask<bool> UpdateCharacterData(CancellationToken token, CharacterType name, Status status)
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
    public async UniTask<bool> RequestUpdateCharacterData(CharacterType name, Status status)
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

    #endregion

}
