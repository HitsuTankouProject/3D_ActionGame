using Cysharp.Threading.Tasks;
using Fusion;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Gate;
using static UnityEngine.Rendering.DebugUI;

public class Release : MonoBehaviour
{
    public static Release Instance { get; private set; }
    private const GameScene sceneName = GameScene.Release;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(Instance);
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
        CancellationTokenSource currentTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
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

    [Header("Reward Level Point")]
    [SerializeField] private int minRewardLevelPoint = 5;
    [SerializeField] private int maxRewardLevelPoint = 10;
    private async UniTask<bool> IsAllowRewardProcess()
    {
        await _networkManager.TestNetworkConnection();

        int averageKilledMonsterLeve = _player.GetAverageKilledMonsterLevel();

        return (_networkManager.networkMode == NetworkMode.Online
            || isLogin
            || averageKilledMonsterLeve > 0);



    }
    private bool TryGetRewardStatus(out Status newStatus)
    {
        newStatus = _player.TargetCharacterStatus(_player.choseCharacter);

        int rewardLevelPoint = _player.GetAverageKilledMonsterLevel();
        int playerCharacterLevel = newStatus.Lv;
        rewardLevelPoint -= playerCharacterLevel;
        rewardLevelPoint = Mathf.Clamp(rewardLevelPoint, minRewardLevelPoint, maxRewardLevelPoint);

        newStatus.Lv += rewardLevelPoint;
        newStatus.LevelPoint += rewardLevelPoint;

        return _player.TrySetCharacterStatus(_player.choseCharacter, newStatus);
    }
    private async UniTask RewardProcess()
    {
        gameStage = ReleaseStage.RewardTime;
        bool needReward = await IsAllowRewardProcess();
        if (!needReward)
        {
            gameStage = ReleaseStage.RewardTimeEnd;
            return;
        }

        if(!TryGetRewardStatus(out Status newStatus))
        {
            MessageBoxOn("Reward Status Calculate Error", Color.red).Forget();

            Debug.LogError(" [Release] Reward Status Calculate Error");
            return;
        }

        bool canDataBaseUpdate = await _networkManager.RequestUpdateCharacterData(_player.choseCharacter, newStatus);

        if (!canDataBaseUpdate)
        {
            MessageBoxOn("DataBase Cant Update the newStatus", Color.red).Forget();
            Debug.LogError(" [Release] DataBase Cant Update the newStatus");
            return;
        }

        MessageBoxOn("NewStatus Update Sucessed", Color.green).Forget();
        gameStage = ReleaseStage.RewardTime;
    }


    [Header("Reward UI")]
    [SerializeField] private GameObject loadingPanel;

    private bool initFinish = false;
    private void Init()
    {
        gameStage = ReleaseStage.Init;
        loadingPanel.SetActive(true);
        RewardProcess().Forget();
    }

    public async UniTask Button_BackToGameTitle()
    {
        await _networkManager.LeaveRoomAndReturnToLobby();

        _networkManager.TryGoGameTitle();

    }
    public async UniTask Button_BackToLobby()
    {
        await _networkManager.LeaveRoomAndReturnToLobby();

        _networkManager.TryGoLobby();

    }

    private void Start()
    {
        Init();
    }

    private void Update()
    {
        if (gameStage != ReleaseStage.RewardTime) return;
        if (!initFinish)
        {
            initFinish = true;
        }
    }
}
