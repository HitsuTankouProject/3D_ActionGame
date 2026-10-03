using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class Lobby : MonoBehaviour
{
    private const GameScene sceneName = GameScene.Lobby;
    private NetworkManager _networkManager => NetworkManager.Instance;
    private GameManager _gameManager => GameManager.Instance;
    private Player _player => _gameManager.gamePlayer;

    private CharacterType nowShowingCharacterType => _player.choseCharacter;

    [Header("Character Type")]
    [SerializeField] private TMP_Text character_Type;
    [SerializeField] private Character characterModel;

    [SerializeField] private Adventurer adventurerModel;
    [SerializeField] private Warrior warriorModel;
    public void TurnOnTargetModel()
    {
        adventurerModel.gameObject.SetActive(nowShowingCharacterType == CharacterType.Adventurer);
        warriorModel.gameObject.SetActive(nowShowingCharacterType == CharacterType.Warrior);
    }

    public MeshRenderer rightWeapon => characterModel.mainWeapon.weaponRenderer;
    public MeshRenderer leftWeapon
    {
        get
        {
            if (characterModel.characterType != CharacterType.Adventurer) return null;
            if(!characterModel.gameObject.TryGetComponent<Adventurer>(out Adventurer adventurer)) return null;
            return adventurer.leftHand.weaponRenderer;
        }
    }


    #region Status

    [Header("Character Status")]
    [SerializeField] private GameObject statusObject;
    [SerializeField] private ActorBasicStatus adventurerBasicStatus;
    private ActorStatus adventurerStatus;
    [SerializeField] private ActorBasicStatus warriorBasicStatus;
    private ActorStatus warriorStatus;
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

    private void SetUpAllCharacterStatus()
    {
        Status adventurer_Status = _player.TargetCharacterStatus(CharacterType.Adventurer);
        Status warrior_Status = _player.TargetCharacterStatus(CharacterType.Warrior);

        adventurerStatus = new(adventurer_Status);
        adventurerStatus.actorBasicStatus = adventurerBasicStatus;
        warriorStatus = new(warrior_Status);
        warriorStatus.actorBasicStatus = warriorBasicStatus;


    }
    private bool TryGetActorStatus(out ActorStatus actorStatus)
    {
        actorStatus = default;

        actorStatus = nowShowingCharacterType switch
        {
            CharacterType.Adventurer => adventurerStatus,
            CharacterType.Warrior => warriorStatus,
            _ => default
        };

        return actorStatus != default;
    }

    private void ChangeNowShowingCharacterStatus()
    {
        if (!TryGetActorStatus(out ActorStatus actorStatus)) return;
        Status status = actorStatus.status;

        character_Type.text = nowShowingCharacterType.ToString();
        character_Lv.text = $"キャラクターレベル：{status.Lv}";
        character_LevelPoint.text = $"レベルポイント：{status.LevelPoint}";
        character_HpLv.text = $"HPレベル：{status.HpLv}";
        character_DefLv.text = $"防御レベル：{status.DefLv}";
        character_AtkLv.text = $"攻撃レベル：{status.AtkLv}";
        character_PassiveLv.text = $"パッシブレベル：{status.PassiveLv}";
        character_ActiveLv.text = $"アクティブレベル：{status.ActiveLv}";
        character_UltLv.text = $"アルティメットレベル：{status.UltLv}";

        int hp = actorStatus.FinalHpIndex();
        int atk = actorStatus.FinalAtkIndex();
        int def = actorStatus.FinalDefIndex();

        character_maxHp.text = $"HP：{hp}";
        character_maxDef.text = $"攻撃：{atk}";
        character_maxAtk.text = $"防御：{def}";

    }

    #endregion

    #region Bag
    [Header("Bag")]
    [SerializeField] private GameObject bagObject;


    [SerializeField] private Button mainWeaponButton;
    [SerializeField] private Button supWeaponButton;
    private void CloseAllWeaponButtons()
    {
        mainWeaponButton.gameObject.SetActive(false);
        supWeaponButton.gameObject.SetActive(false);
    }

    [SerializeField] private Button[] weaponButtons;
    private void ChangeWeaponMaterial(Material weaponMaterial)
    {
        nowShowingWeaponMeshRender.material = weaponMaterial;
    }
    private void ChangeWeaponButtons(List<WeaponStatus> weaponStatuses)
    {
        if (weaponStatuses.Count != weaponButtons.Length)
        {
            Debug.LogError("weaponStatuses.Count != weaponButtons.Length");
            return;
        }

        for (int i = 0; i < weaponStatuses.Count; i++)
        {
            int index = i;
            weaponButtons[index].image.sprite = weaponStatuses[index].weaponIcon;
            weaponButtons[index].onClick.AddListener(() => ChangeWeaponMaterial(weaponStatuses[index].weaponMaterial));
        }

    }


    [Header("Camera")]
    [SerializeField] private Animator cameraAnimator;
    private const string turnRightTigger = "TurnRight";
    private const string turnLeftTigger = "TurnLeft";
    private const string turnReturnTigger = "Return";
    private enum CameraShow { Left, Right };
    private void CameraMove(CameraShow cameraShow)
        => cameraAnimator.SetTrigger(cameraShow == CameraShow.Right ? turnRightTigger : turnLeftTigger);
    private void CameraReturn() => cameraAnimator.SetTrigger(turnReturnTigger);

    [Header("Sword Icon")]
    [SerializeField] private MeshRenderer sword;
    private GameObject swordObject => sword.gameObject;
    [SerializeField] private Sprite swordBagIcon;

    [Header("Shield Icon")]
    [SerializeField] private MeshRenderer shield;
    private GameObject shieldObject => shield.gameObject;
    [SerializeField] private Sprite shieldBagIcon;

    private MeshRenderer nowShowingWeaponMeshRender;

    private WeaponType nowShowingWeaponType = WeaponType.Sword;

    private void CloseAllTheWeapon()
    {
        swordObject.SetActive(false);
        shieldObject.SetActive(false);
    }
    private void OpenTargetWeapon()
    {
        CloseAllTheWeapon();
        switch (nowShowingWeaponType)
        {
            case WeaponType.Sword:
                swordObject.SetActive(true);
                nowShowingWeaponMeshRender = sword;
                return;
            case WeaponType.Shield:
                shieldObject.SetActive(true);
                nowShowingWeaponMeshRender = shield;
                return;
            default:
                nowShowingWeaponMeshRender = null;
                return;
        }

    }

    public void OpenSwordBag()
    {
        if (_gameManager == null) return;
        if (!_gameManager.TryGetSwordStatus(out List<WeaponStatus> allSwordStatus)) return;
        nowShowingWeaponType = WeaponType.Sword;
        OpenTargetWeapon();
        ChangeWeaponButtons(allSwordStatus);
        CameraMove(CameraShow.Left);
    }
    public void OpenShieldBag()
    {
        if (_gameManager == null) return;
        if (!_gameManager.TryGetShieldStatus(out List<WeaponStatus> allShieldStatus)) return;
        nowShowingWeaponType = WeaponType.Shield;
        OpenTargetWeapon();
        ChangeWeaponButtons(allShieldStatus);
        CameraMove(CameraShow.Right);
    }

    #endregion

    public void OpenPlayerBag()
    {
        statusObject.gameObject.SetActive(false);
        bagObject.gameObject.SetActive(true);
    }

    public void OpenPlayerStatus()
    {
        statusObject.gameObject.SetActive(true);
        bagObject.gameObject.SetActive(false);
    }


    #region Choose Character
    private bool TryChangeShowModel()
    {
        characterModel = nowShowingCharacterType switch
        {
            CharacterType.Adventurer => adventurerModel,

            CharacterType.Warrior => warriorModel,


            _ => null
        };

        return characterModel != null;
    }
    public void PickAdventurer()
    {
        _player.PickAdventurer();
        if (!TryChangeShowModel()) return;
        CloseAllWeaponButtons();
        

        mainWeaponButton.image.sprite = swordBagIcon;
        supWeaponButton.image.sprite = shieldBagIcon;
        mainWeaponButton.onClick.AddListener(OpenSwordBag);
        supWeaponButton.onClick.AddListener(OpenShieldBag);

        mainWeaponButton.gameObject.SetActive(true);
        supWeaponButton.gameObject.SetActive(true);

        TurnOnTargetModel();
        ChangeNowShowingCharacterStatus();
    }
    public void PickMagician()
    {
        _player.PickMagician();
        CloseAllWeaponButtons();
        mainWeaponButton.gameObject.SetActive(true);

        TurnOnTargetModel();
        ChangeNowShowingCharacterStatus();
    }
    public void PickThief()
    {
        _player.PickThief();
        CloseAllWeaponButtons();
        mainWeaponButton.gameObject.SetActive(true);

        TurnOnTargetModel();
        ChangeNowShowingCharacterStatus();
    }
    public void PickWarrior()
    {
        _player.PickWarrior();
        if (!TryChangeShowModel()) return;
        CloseAllWeaponButtons();
        mainWeaponButton.gameObject.SetActive(true);


        TurnOnTargetModel();
        ChangeNowShowingCharacterStatus();
    }

    #endregion


    public void StartSinglePlayer() => StartPlayerAsync(RoomMode.Single).Forget();

    public void StartMultiplayer() => StartPlayerAsync(RoomMode.Multiplayer).Forget();

    private async UniTask StartPlayerAsync(RoomMode roomMode)
    {
        bool result = await _networkManager.StartMatchmaking(roomMode);
        Debug.Log(result ?
            $"{roomMode.ToString()} Roomへの参加に成功しました。" :
            $"{roomMode.ToString()} Roomへの参加に失敗しました。", this);
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

        SetUpAllCharacterStatus();
        PickWarrior();
        OpenPlayerStatus();
    }



}
