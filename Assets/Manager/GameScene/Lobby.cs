using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static UnityEditor.SceneView;

public struct PlayerPickWeapon
{
    public WeaponStatus mainWeaponStatus;
    public WeaponStatus supWeaponStatus;

    public PlayerPickWeapon(WeaponStatus mainWeapon, WeaponStatus supWeapon = null)
    {
        mainWeaponStatus = mainWeapon;
        supWeaponStatus = supWeapon;
    }


}

public class Lobby : MonoBehaviour
{
    public static Lobby Instance { get; private set; }
    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

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

    #region WeaponPick
    [Header("WeaponPick")]
    [SerializeField] private GameObject weaponPickObject;
    [System.Serializable] private struct WeaponCanPickData
    {
        public MeshRenderer weaponMeshRenderer;
        public GameObject weaponObject => weaponMeshRenderer.gameObject;
        public Sprite weaponBagIcon;
    }
    private enum NowPickingWeapon { Main, Sup }
    private readonly Dictionary<WeaponType, NowPickingWeapon> nowPickingWeaponType = new()
    {
        { WeaponType.Sword ,NowPickingWeapon.Main},
        { WeaponType.Shield ,NowPickingWeapon.Sup},
        { WeaponType.Staff ,NowPickingWeapon.Main},
        { WeaponType.Knife ,NowPickingWeapon.Main},
        { WeaponType.Greatsword ,NowPickingWeapon.Main},
    };
    [Header("Camera")]
    [SerializeField] private Animator cameraAnimator;
    private const string turnRightTigger = "TurnRight";
    private const string turnLeftTigger = "TurnLeft";
    private const string turnReturnTigger = "Return";

    private void CameraMove(WeaponType weapon)
    {
        if (!nowPickingWeaponType.ContainsKey(weapon))
        {
            cameraAnimator.ResetTrigger(turnRightTigger);
            cameraAnimator.ResetTrigger(turnLeftTigger);
            cameraAnimator.SetTrigger(turnReturnTigger);
        }

        else
            cameraAnimator.SetTrigger(
                nowPickingWeaponType[weapon] == NowPickingWeapon.Main ? 
                turnLeftTigger : 
                turnRightTigger);
    }

    [Header("Weapon Pick Button")]
    [SerializeField] private Button mainWeaponButton;
    [SerializeField] private Button supWeaponButton;

    [SerializeField] private PickWeaponButton[] pickWeaponButtons;
    [SerializeField] private WeaponType nowShowingWeaponType = WeaponType.None;
    [Header("Main Weapon")]
    [SerializeField] private WeaponCanPickData swordData;
    [Header("Sup Weapon")]
    [SerializeField] private WeaponCanPickData shieldData;
    private void CloseAllWeaponButtons()
    {
        mainWeaponButton.gameObject.SetActive(false);
        supWeaponButton.gameObject.SetActive(false);
    }
    private bool TryGetWeaponCanPickData(WeaponType weaponType, out WeaponCanPickData weaponCanPickData)
    {
        weaponCanPickData = default;

        switch (weaponType)
        {
            case WeaponType.Sword: weaponCanPickData = swordData; return true;
            case WeaponType.Shield: weaponCanPickData = shieldData; return true;
            default: return false;
        }

    }
    private UnityAction Button_NowPickWeapon(WeaponType weaponType)
    {
        return weaponType switch
        {
            WeaponType.Sword => Button_NowPickSword,
            WeaponType.Shield => Button_NowPickShield,
            _ => null
        };
    }

    private void OpenWeaponButton()
    {
        CloseAllWeaponButtons();
        WeaponType[] allWeaponCharacterCanPick = _gameManager.characterCanPickWeapon[nowShowingCharacterType];

        if (!TryGetWeaponCanPickData(allWeaponCharacterCanPick[0],out WeaponCanPickData mainWeaponCanPickData))
        {
            Debug.LogError("mainWeaponCanPickData == null");
            return;
        }

        mainWeaponButton.image.sprite = mainWeaponCanPickData.weaponBagIcon;
        mainWeaponButton.onClick.AddListener(() => Button_NowPickWeapon(allWeaponCharacterCanPick[0])());
        mainWeaponButton.gameObject.SetActive(true);

        if (allWeaponCharacterCanPick.Length == 2)
        {
            if (!TryGetWeaponCanPickData(allWeaponCharacterCanPick[1], out WeaponCanPickData supWeaponCanPickData))
            {
                Debug.LogError("supWeaponCanPickData == null");
                return;
            }
            supWeaponButton.image.sprite = supWeaponCanPickData.weaponBagIcon;
            supWeaponButton.onClick.AddListener(() => Button_NowPickWeapon(allWeaponCharacterCanPick[1])());
            supWeaponButton.gameObject.SetActive(true);
        }

        mainWeaponButton.onClick.Invoke();
    }

    private MeshRenderer nowShowingWeaponMeshRender
    {
        get
        {
            return nowShowingWeaponType switch
            {
                WeaponType.Sword => swordData.weaponMeshRenderer,
                WeaponType.Shield => shieldData.weaponMeshRenderer,
                _ => null
            };
        }
    }

    private void CloseAllTheWeapon()
    {
        swordData.weaponObject.SetActive(false);
        shieldData.weaponObject.SetActive(false);
    }
    private void OpenTargetWeapon()
    {
        CloseAllTheWeapon();
        switch (nowShowingWeaponType)
        {
            case WeaponType.Sword: swordData.weaponObject.SetActive(true); return;
            case WeaponType.Shield: shieldData.weaponObject.SetActive(true); return;
            default: return;
        }

    }
    private void ChangePickWeaponButtons()
    {
        OpenTargetWeapon();
        List<PlayerItemData> allTargetWeaponData = _player.bag.GetAllTargetWeaponData(nowShowingWeaponType);

        for (int i = 0; i < pickWeaponButtons.Length;)
        {
            if (i >= allTargetWeaponData.Count)
            {
                pickWeaponButtons[i].gameObject.SetActive(false);
                i++;
                continue;
            }
            PlayerItemData data = allTargetWeaponData[i];
            if (!_gameManager.TryGetTargetWeaponStatus(nowShowingWeaponType, data.rare, out WeaponStatus weaponStatus)) continue;
            pickWeaponButtons[i].SetupWeaponPick(weaponStatus);
            i++;
        }
    }

    public void Button_NowPickSword()
    {
        nowShowingWeaponType = WeaponType.Sword;

        ChangePickWeaponButtons();
        CameraMove(nowShowingWeaponType);
    }
    public void Button_NowPickShield()
    {
        nowShowingWeaponType = WeaponType.Shield;

        ChangePickWeaponButtons();
        CameraMove(nowShowingWeaponType);
    }


    [Header("Weapon Pick")]
    [SerializeField] private WeaponStatus mainWeaponStatus;
    [SerializeField] private WeaponStatus supWeaponStatus;

    public void ChangeWeapon(WeaponStatus weaponStatus)
    {
        if (nowPickingWeaponType[nowShowingWeaponType] == NowPickingWeapon.Main)
        {
            mainWeaponStatus = weaponStatus;
            nowShowingWeaponMeshRender.material = mainWeaponStatus.weaponMaterial;

            return;
        }
        else if (nowPickingWeaponType[nowShowingWeaponType] == NowPickingWeapon.Sup)
        {
            supWeaponStatus = weaponStatus;
            nowShowingWeaponMeshRender.material = supWeaponStatus.weaponMaterial;

            return;
        }

        Debug.LogError($"nowPickingWeaponType[nowShowingWeaponType] == {nowPickingWeaponType[nowShowingWeaponType]}");
    }

    #endregion

    #region Bag
    //[Header("Bag")]
   /* [SerializeField] */private GameObject bagObject;
    #endregion

    private void ChooseAllTheObject()
    {
        statusObject.SetActive(false);
        weaponPickObject.SetActive(false);
        //bagObject.SetActive(false);

    }

    public void OpenCharacterStatusObject()
    {
        ChooseAllTheObject();
        nowShowingWeaponType = WeaponType.None;
        CameraMove(nowShowingWeaponType);
        ChangeNowShowingCharacterStatus();

        statusObject.SetActive(true);

    }
    public void OpenWeaponPickObject()
    {
        ChooseAllTheObject();
        OpenWeaponButton();
        weaponPickObject.SetActive(true);

    }



    public void OpenPlayerBagObject()
    {
        ChooseAllTheObject();
        bagObject.gameObject.SetActive(true);
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
    private Action ChangePickingCharacter(CharacterType characterType)
    {
        switch (characterType)
        {
            case CharacterType.Adventurer: return _player.PickAdventurer;
            case CharacterType.Magician: return _player.PickMagician;
            case CharacterType.Thief: return _player.PickThief;
            case CharacterType.Warrior: return _player.PickWarrior;
            default:
                Debug.LogError($"{characterType} is not a valid CharacterType");
                return null;
        }

    }

    private void PickCharacter(CharacterType characterType)
    {
        ChangePickingCharacter(characterType)?.Invoke();
        if (!TryChangeShowModel()) return;

        TurnOnTargetModel();
        OpenCharacterStatusObject();
    }

    public void PickAdventurer()=> PickCharacter(CharacterType.Adventurer);
    public void PickMagician()=> PickCharacter(CharacterType.Magician);
    public void PickThief() => PickCharacter(CharacterType.Thief);
    public void PickWarrior() => PickCharacter(CharacterType.Warrior);

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
        PickAdventurer();
        OpenCharacterStatusObject();
    }



}
