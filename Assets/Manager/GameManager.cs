using Cysharp.Threading.Tasks;
using Fusion;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Unity.Collections.Unicode;
public enum GameScene { None = -1, GameTitle = 0, Lobby = 1, InGame = 2, Release = 3, Error}
public enum GameDevice { Pc, Mobile }

[System.Serializable]
public struct AllWeaponStatus
{
    [Header("Sword")]
    public WeaponStatus sword_rare_01;
    public WeaponStatus sword_rare_02;
    public WeaponStatus sword_rare_03;
    public WeaponStatus sword_rare_04; 
    public WeaponStatus sword_rare_05;
    [Header("Shield")]
    public WeaponStatus shield_rare_01;
    public WeaponStatus shield_rare_02;
    public WeaponStatus shield_rare_03;
    public WeaponStatus shield_rare_04;
    public WeaponStatus shield_rare_05;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }


    private NetworkManager _networkManager => NetworkManager.Instance;
    public Player gamePlayer;

    public static int player_uid { get; private set; } = -1;
    public const int gameFps = 60;
    public const int commandFps = gameFps / 10;
    public const float commandTime = (float)commandFps / gameFps;

    #region GameScene
    public GameScene nowGameScene { get; private set; } = GameScene.None;
    public void UpdateGameScene(GameScene gameScene) => nowGameScene = gameScene;

    #endregion

    #region Character Data

    public readonly Dictionary<CharacterType, WeaponType[]> characterCanPickWeapon = new()
    {
        { CharacterType.Adventurer, new WeaponType[] { WeaponType.Sword, WeaponType.Shield } },
        { CharacterType.Magician, new WeaponType[] { WeaponType.Staff } },
        { CharacterType.Thief, new WeaponType[] { WeaponType.Knife } },
        { CharacterType.Warrior, new WeaponType[] { WeaponType.Sword } } // testing

        //{ CharacterType.Warrior, new WeaponType[] { WeaponType.Greatsword} }
    };


    #endregion

    #region Weapon Data
    [Header("AllWeaponStatus")]
    public AllWeaponStatus allWeaponStatus;
    private bool TryGetSwordStatus(uint rare, out WeaponStatus swordStatus)
    {
        swordStatus = default;
        if (rare <= 0 || rare > 5) return false;
        swordStatus = rare switch
        {
            1 => allWeaponStatus.sword_rare_01,
            2 => allWeaponStatus.sword_rare_02,
            3 => allWeaponStatus.sword_rare_03,
            4 => allWeaponStatus.sword_rare_04,
            5 => allWeaponStatus.sword_rare_05,
            _ => default
        };
        
        return swordStatus != default;


    }
    private bool TryGetShieldStatus(uint rare, out WeaponStatus swordStatus)
    {
        swordStatus = default;
        if (rare <= 0 || rare > 5) return false;
        swordStatus = rare switch
        {
            1 => allWeaponStatus.shield_rare_01,
            2 => allWeaponStatus.shield_rare_02,
            3 => allWeaponStatus.shield_rare_03,
            4 => allWeaponStatus.shield_rare_04,
            5 => allWeaponStatus.shield_rare_05,
            _ => default
        };

        return swordStatus != default;


    }

    public bool TryGetTargetWeaponStatus(WeaponType weaponType, uint rare, out WeaponStatus weaponStatus)
    {
        weaponStatus = default;

        switch (weaponType)
        {
            case WeaponType.Sword:
                if (TryGetSwordStatus(rare, out WeaponStatus swordStatus))
                {
                    weaponStatus = swordStatus;
                    return true;
                }
                else Debug.LogError($"[GameManager] Sword Status Not Found! Rare: {rare}");
                break;
            case WeaponType.Shield:
                if (TryGetShieldStatus(rare, out WeaponStatus shieldStatus))
                {
                    weaponStatus = shieldStatus;
                    return true;
                }
                else Debug.LogError($"[GameManager] Shield Status Not Found! Rare: {rare}");
                break;

            default:
                Debug.LogError($"[GameManager] Weapon Type Not Supported! Type: {weaponType}");
                break;

        }
        return false;
    }

    #endregion

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(this);
            return;
        }

        DontDestroyOnLoad(this);
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = gameFps;
    }


}
