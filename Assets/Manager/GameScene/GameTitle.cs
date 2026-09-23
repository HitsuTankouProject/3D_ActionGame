using Cysharp.Threading.Tasks;
using DataBase;
using System;
using System.Diagnostics;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WebSocketSharp;

public class GameTitle : MonoBehaviour
{
    private const GameScene sceneName = GameScene.GameTitle;
    private GameManager _gameManager => GameManager.Instance;
    private Player _player => _gameManager.gamePlayer;

    private NetworkManager _networkManager => NetworkManager.Instance;


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

    [Header("Main Game Title")]
    [SerializeField] private GameObject mainTitle;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button acCreateButton;
    [SerializeField] private Button logoutButton;

    [SerializeField] private Image loginIcon;
    [SerializeField] private Image internetIcon;

    [SerializeField] private Sprite sp_Login;
    [SerializeField] private Sprite sp_Logout;

    [SerializeField] private Sprite sp_HaveInternet;
    [SerializeField] private Sprite sp_NoInternet;

    private void MainTitleButtonSwitch()
    {
        if (_networkManager == null || _networkManager.networkMode == NetworkMode.Offline)
        {
            loginButton.gameObject.SetActive(false);
            acCreateButton.gameObject.SetActive(false);
            logoutButton .gameObject.SetActive(false);
            return;
        }

        if(_networkManager.logMode == LogMode.Logout)
        {
            loginButton.gameObject.SetActive(true);
            acCreateButton.gameObject.SetActive(true);
            logoutButton.gameObject.SetActive(false);

            loginIcon.sprite = sp_Logout;
        }
        else
        {
            loginButton.gameObject.SetActive(false);
            acCreateButton.gameObject.SetActive(false);
            logoutButton.gameObject.SetActive(true);

            loginIcon.sprite = sp_Login;

        }

    }

    public void OpenMainTitlePanel()
    {
        OffAllThePanel();
        MainTitleButtonSwitch();
        mainTitle.SetActive(true);
    }

    public async void AccountLogout()
    {
        bool success = await _networkManager.RequestAccountOut();
        if (success)
        {
            MessageBoxOn("　アカウントログアウト成功　", Color.black, 0.5f).Forget();
            _player.ResetAllCharacterData();
        }
        else MessageBoxOn("　アカウントログアウト失敗　", Color.red, 0.5f).Forget();

        MainTitleButtonSwitch();
    }

    [Header("Login")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private TMP_InputField login_user_email;
    [SerializeField] private TMP_InputField login_user_password;

    public void OpenLoginPanel()
    {
        OffAllThePanel();
        loginPanel.SetActive(true);
    }
    private async UniTask GetAccountData()
    {
        if (_networkManager.logMode != LogMode.Login)
        {
            MessageBoxOn("アカウントログインしていません　", Color.red).Forget();
            return;
        }
        PlayerBag playerBagData = await _networkManager.GotData_Bag();
        AllCharacterData characterData = await _networkManager.GotData_AllCharacter();

        _player.SetPlayerBag(playerBagData);
        _player.SetAllStatus(characterData);
    }


    public async void LoginPanel_AccountLogin()
    {
        if(_networkManager.logMode == LogMode.Login)
        {
            MessageBoxOn("アカウント既にログインしました　", Color.green).Forget();
        }

        bool success = await _networkManager.RequestAccountLogin(login_user_email.text, login_user_password.text);
        if (success)
        {
            MessageBoxOn("　アカウントログイン成功　", Color.green).Forget();
            OffAllThePanel();
            await GetAccountData();
            MoveToLobby();
        }
        else MessageBoxOn("　アカウントログイン失敗　", Color.red).Forget();
    }
    public void LoginPanel_AccountDataClear()
    {
        if (login_user_email == null || login_user_password == null) return;
        login_user_email.text = "";
        login_user_password.text = "";
    }



    [Header("Account Create")]
    [SerializeField] private GameObject accountCreatePanel;
    [SerializeField] private TMP_InputField ac_user_email;
    [SerializeField] private TMP_InputField ac_user_authCode;
    [SerializeField] private TMP_InputField ac_user_name;
    [SerializeField] private TMP_InputField ac_user_password;

    public void OpenAccountCreatePanel()
    {
        OffAllThePanel();
        accountCreatePanel.SetActive(true);
    }

    private string email;
    public async void ACPanel_AccountApply()
    {
        if(ac_user_email.text.Length == 0) return;
        email = ac_user_email.text;
        bool success = await _networkManager.RequestAccountApply(ac_user_email.text);
        if (success)
        {
            MessageBoxOn(" アカウント申請成功 [迷惑メールボックスに送る可能性がありますbbvbbbbbhbbb] ", Color.green).Forget();
        }
        else MessageBoxOn("　アカウント申請失敗　", Color.red).Forget();
    }

    public async void ACPanel_AccountCertification()
    {
        if (ac_user_email.text.Length == 0 || ac_user_authCode.text.Length == 0) return;

        if(ac_user_email.text != email)
        {
            MessageBoxOn("　メール、または認証コード違います　", Color.red).Forget();
            return;
        }

        bool success = await _networkManager.RequestAccountCertification(email, ac_user_authCode.text);
        if (success) MessageBoxOn("　メール認証成功　", Color.green).Forget();
        else MessageBoxOn("　メール、または認証コード違います　", Color.red).Forget();
    }

    public async void ACPanel_AccountCreate()
    {
        if (ac_user_email.text.Length == 0 || ac_user_authCode.text.Length == 0
            || ac_user_name.text.Length == 0 || ac_user_password.text.Length == 0) return;

        bool success = await _networkManager.RequestAccountCreate(email, ac_user_name.text, ac_user_password.text);
        if (!success) MessageBoxOn("　アカウント作成失敗　", Color.red).Forget();

        MessageBoxOn("　アカウント作成成功　", Color.green).Forget();
        OpenMainTitlePanel();

    }



    private void OffAllThePanel()
    {
        mainTitle.gameObject.SetActive(false);
        loginPanel.gameObject.SetActive(false);
        accountCreatePanel.gameObject.SetActive(false);
    }

    private void Init()
    {
        
        loginIcon.sprite = sp_Logout;
        internetIcon.sprite = sp_NoInternet;
        
        if (_gameManager == null) return;
        _gameManager.UpdateGameScene(sceneName);
    }

    private async UniTask TestNetworkConnection()
    {
        if (_networkManager == null) return;
        await _networkManager.TestNetworkConnection();
        if (_networkManager.networkMode != NetworkMode.Online)
        {
            MessageBoxOn("ネットワーク接続失敗", Color.red, 0.5f).Forget();
            return;
        }
        else
        {
            internetIcon.sprite = sp_HaveInternet;
            MessageBoxOn("ネットワーク接続成功", Color.green, 0.5f).Forget();
        }
        OpenMainTitlePanel();
    }
    public void TryNetworkConnection()=> TestNetworkConnection().Forget();

    public void MoveToLobby()
    {
        _networkManager.TryGoLobby();
    }
    private void Start()
    {
        Init();
        TryNetworkConnection();

    }











}
