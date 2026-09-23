using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class Lobby : MonoBehaviour
{
    private const GameScene sceneName = GameScene.Lobby;
    private NetworkManager _networkManager => NetworkManager.Instance;
    private GameManager _gameManager => GameManager.Instance;
    private Player _player => _gameManager.gamePlayer;

    private CharacterType nowShowingCharacterType => _player.choseCharacter;
    [Header("Character Type")]
    [SerializeField] private TMP_Text character_Type;
    [Header("Character Status Lv")]
    [SerializeField] private TMP_Text character_Lv;
    [SerializeField] private TMP_Text character_LevelPoint;
    [SerializeField] private TMP_Text character_HpLv;
    [SerializeField] private TMP_Text character_DefLv;
    [SerializeField] private TMP_Text character_AtkLv;
    [SerializeField] private TMP_Text character_PassiveLv;
    [SerializeField] private TMP_Text character_ActiveLv;
    [SerializeField] private TMP_Text character_UltLv;
    [Header("Character Status Index")]
    [SerializeField] private TMP_Text character_maxHp;
    [SerializeField] private TMP_Text character_maxDef;
    [SerializeField] private TMP_Text character_maxAtk;


    private Adventurer adventurer = new();
    private ActorStatus adventurerStatus;
    private void SetUpAdventurerStatus()
    {
        Status status = _player.TargetCharacterStatus(CharacterType.Adventurer);
        adventurerStatus = new(status, adventurer.allLevelScalePairs);

    }
    private void SetNowShowingCharacterType()
    {
        character_Type.text = nowShowingCharacterType.ToString();
    }
    private void SetNowShowingCharacterLv()
    {
        Status status = _player.TargetCharacterStatus(nowShowingCharacterType);

        character_Lv.text           = $"キャラクターレベル：{status.Lv}";
        character_LevelPoint.text   = $"レベルポイント：{status.LevelPoint}";
        character_HpLv.text         = $"HPレベル：{status.HpLv}";
        character_DefLv.text        = $"防御レベル：{status.DefLv}";
        character_AtkLv.text        = $"攻撃レベル：{status.AtkLv}";
        character_PassiveLv.text    = $"パッシブレベル：{status.PassiveLv}";
        character_ActiveLv.text     = $"アクティブレベル：{status.ActiveLv}";
        character_UltLv.text        = $"アルティメットレベル：{status.UltLv}";
    }
    private void SetNowShowingCharacterStatus()
    {
        int hp = adventurerStatus.FinalHpIndex(adventurer.initAtk);
        int atk = adventurerStatus.FinalAtkIndex(adventurer.initAtk);
        int def = adventurerStatus.FinalDefIndex(adventurer.initAtk);

        character_maxHp.text = $"HP：{hp}";
        character_maxDef.text = $"攻撃：{atk}";
        character_maxAtk.text = $"防御：{def}";

    }

    public void StartSinglePlayer() => StartPlayerAsync(RoomMode.Single).Forget();

    public void StartMultiplayer() => StartPlayerAsync(RoomMode.Multiplayer).Forget();


    private async UniTask StartPlayerAsync(RoomMode roomMode)
    {
        if (_networkManager.networkMode != NetworkMode.Online)
        {
            Debug.LogError("Photon Lobbyに接続していません。", this);
            return;
        }

        bool result = await _networkManager.StartMatchmaking(roomMode);
        Debug.Log(result ?
            $"{roomMode.ToString()} Roomへの参加に成功しました。" :
            $"{roomMode.ToString()} Roomへの参加に失敗しました。", this);
    }

    private void ChangeNowShowingCharacter()
    {
        SetNowShowingCharacterType();
        SetNowShowingCharacterLv();
        SetNowShowingCharacterStatus();
    }


    public void MoveToGameTitle()
    {
        _networkManager.TryGoGameTitle();
    }

    private void Start()
    {
        if(_gameManager == null || _networkManager == null)
        {
            Debug.LogError("_gameManager == null || _networkManager == null");
            return;
        }
        _gameManager.UpdateGameScene(sceneName);

        SetUpAdventurerStatus();
        ChangeNowShowingCharacter();
    }


}
