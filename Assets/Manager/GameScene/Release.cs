using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;


public class Release : MonoBehaviour
{
    public static Release Instance { get; private set; }
    private const GameScene sceneName = GameScene.Release;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }
    private GameManager _gameManager => GameManager.Instance;
    private Player _player => _gameManager.gamePlayer;

    private NetworkManager _networkManager => NetworkManager.Instance;
    private bool isLogin => _networkManager.logMode == LogMode.Login;

    [Header("Message Box")]
    [SerializeField] private TMP_Text messageBox;
    [SerializeField] private float messageBoxShowTime = 0.5f;
    private CancellationTokenSource messageBoxTokenSource;
    private void CancelMessageBox()
    {
        if (messageBoxTokenSource == null) return;

        messageBoxTokenSource.Cancel();
        messageBoxTokenSource = null;
        messageBox.gameObject.SetActive(false);

    }
    private async UniTask MessageBoxOn(string message, Color messageColor, float time = 0.5f)
    {
        if (messageBox == null) return;
        CancelMessageBox();
        CancellationTokenSource currentTokenSource = 
            CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        messageBoxTokenSource = currentTokenSource;
        messageBox.text = message;
        messageBox.color = messageColor;

        messageBox.gameObject.SetActive(true);
        float showTime = time > 0.0f ? time : messageBoxShowTime;

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(showTime), cancellationToken: currentTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (messageBoxTokenSource == currentTokenSource)
            {
                messageBox.gameObject.SetActive(false);
                messageBoxTokenSource = null;
            }

            currentTokenSource.Dispose();
        }



    }

    private enum ReleaseStage { Init, RewardTime, RewardTimeEnd }
    [Header("Release Stage")]
    [SerializeField] private ReleaseStage gameStage = ReleaseStage.Init;

    [Header("Reward Time")]
    [SerializeField] private int minRewardLevelPoint = 5;
    [SerializeField] private int maxRewardLevelPoint = 10;
    private async UniTask<bool> IsAllowRewardProcess()
    {
        await _networkManager.TestNetworkConnection();
        bool result = _networkManager.networkMode == NetworkMode.Online && isLogin;

        if (!result) Debug.LogWarning("Cant Allow Reward Process");
        return result;
    }


    private bool TryGetRewardStatus(out Status newStatus)
    {
        newStatus = default;
        if(!_player.TryGetCharacterStatus(_player.choseCharacter, out Status targetStatus))
        {
            Debug.LogError($"[Release] {_player.choseCharacter}`s Status Cant Find");
            return false;
        }
        newStatus = targetStatus;

        int rewardLevelPoint = _player.GetAverageKilledMonsterLevel();
        int playerCharacterLevel = newStatus.Lv;
        rewardLevelPoint -= playerCharacterLevel;
        rewardLevelPoint = Mathf.Clamp(rewardLevelPoint, minRewardLevelPoint, maxRewardLevelPoint);

        newStatus.Lv += rewardLevelPoint;
        newStatus.LevelPoint += rewardLevelPoint;

        return _player.TrySetCharacterStatus(_player.choseCharacter, newStatus);
    }
    private async UniTask StatusRewardProcess()
    {
        int averageKilledMonsterLeve = _player.GetAverageKilledMonsterLevel();
        if (averageKilledMonsterLeve <= 0) return;

        if (!TryGetRewardStatus(out Status newStatus))
        {
            MessageBoxOn("Reward Status Calculate Error", Color.red).Forget();

            Debug.LogError(" [Release] Reward Status Calculate Error");
            gameStage = ReleaseStage.RewardTimeEnd;
            return;
        }

        bool canDataBaseUpdate = await _networkManager.RequestUpdateCharacterData(_player.choseCharacter, newStatus);

        if (!canDataBaseUpdate)
        {
            MessageBoxOn("DataBase Cant Update the newStatus", Color.red,2.0f).Forget();
            Debug.LogWarning(" [Release] DataBase Cant Update the newStatus");
        }
        else MessageBoxOn("NewStatus Update Success", Color.green).Forget();

    }

    private async UniTask ItemsRewardProcess()
    {
        PlayerItem[] inGameBag = _player.inGameBag.Items;

        bool canDataBaseUpdate = await _networkManager.RequestUpdateBagData(inGameBag);

        if (!canDataBaseUpdate)
        {
            MessageBoxOn("Bag Data Update Failed", Color.red, 2.0f).Forget();
            Debug.LogWarning("[Release] Bag Data Update Failed");
            return;
        }

        MessageBoxOn("Bag Data Update Success", Color.green).Forget();
    }

    [Header("Reward UI")]
    [SerializeField] private GameObject loadingPanel;

    private bool initFinish = false;
    private async UniTask Init()
    {
        gameStage = ReleaseStage.Init;
        loadingPanel.SetActive(true);

        bool isAllowReward = await IsAllowRewardProcess();
        if (isAllowReward)
        {
            gameStage = ReleaseStage.RewardTime;
            await StatusRewardProcess();
            await ItemsRewardProcess();
        }
        gameStage = ReleaseStage.RewardTimeEnd;

        loadingPanel.SetActive(false);
        InputManager.OnMoveMousePad();

        initFinish = true;
    }

    private async UniTask BackToGameTitle()
    {
        if (!initFinish) return;

        await _networkManager.LeaveRoomAndReturnToLobby();

        _networkManager.TryGoGameTitle();

    }
    public void Button_BackToGameTitle()=> BackToGameTitle().Forget();
    public async UniTask BackToLobby()
    {
        if (!initFinish) return;

        await _networkManager.LeaveRoomAndReturnToLobby();

        _networkManager.TryGoLobby();

    }
    public void Button_BackToLobby()
    {
        if (!initFinish) return;

        BackToLobby().Forget();
    }


    private void Start()
    {
        Init().Forget();
    }

}
