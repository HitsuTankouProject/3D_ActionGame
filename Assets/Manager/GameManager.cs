using Cysharp.Threading.Tasks;
using Fusion;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Unity.Collections.Unicode;


/// <summary>
/// ゲーム内で使用するSceneの種類を表す。
/// </summary>
public enum GameScene { None = -1, GameTitle = 0, Lobby = 1, InGame = 2, Release = 3, Error}
/// <summary>
/// ゲームを実行するデバイスの種類を表す。
/// </summary>
public enum GameDevice { Pc, Mobile }

/// <summary>
/// ゲーム内で使用する各武器のWeaponStatusを保持する構造体。
/// 武器の種類とレアリティごとにステータスを管理する。
/// </summary>
[System.Serializable]
public struct AllWeaponStatus
{
    /// <summary> Swordステータス。</summary>
    [Header("Sword")]
    public WeaponStatus sword_rare_01;
    public WeaponStatus sword_rare_02;
    public WeaponStatus sword_rare_03;
    public WeaponStatus sword_rare_04; 
    public WeaponStatus sword_rare_05;
    /// <summary> Shieldステータス。</summary>
    [Header("Shield")]
    public WeaponStatus shield_rare_01;
    public WeaponStatus shield_rare_02;
    public WeaponStatus shield_rare_03;
    public WeaponStatus shield_rare_04;
    public WeaponStatus shield_rare_05;
}

/// <summary>
/// ゲーム全体で共通して使用するデータと設定を管理するクラス。
/// Scene情報、Player情報、キャラクターが装備可能な武器、
/// WeaponStatus、FPS設定などを管理する。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private NetworkManager _networkManager => NetworkManager.Instance;
    /// <summary>
    /// このゲームで使用するPlayerデータ。
    /// </summary>
    public Player gamePlayer;

    /// <summary>
    /// ゲームの目標FPS。
    /// </summary>
    public const int gameFps = 60;
    /// <summary>
    /// コマンド受付時間の計算に使用するフレーム数。
    /// </summary>
    public const int commandFps = gameFps / 10;
    /// <summary>
    /// 次のコマンドを待機する時間。
    /// </summary>
    public const float commandTime = (float)commandFps / gameFps;

    #region GameScene
    /// <summary>
    /// 現在のGameScene。
    /// </summary>
    public GameScene nowGameScene/* { get; private set; } */= GameScene.None;
    /// <summary>
    /// 現在のGameScene情報を更新する。
    /// </summary>
    /// <param name="gameScene">変更後のGameScene。</param>
    public void UpdateGameScene(GameScene gameScene) => nowGameScene = gameScene;

    #endregion

    #region Character Data
    /// <summary>
    /// 各キャラクターが装備可能な武器の種類を管理する。
    /// </summary>
    public readonly Dictionary<CharacterType, WeaponType[]> characterCanPickWeapon = new()
    {
        { CharacterType.Adventurer, new WeaponType[] { WeaponType.Sword, WeaponType.Shield } },
        { CharacterType.Magician, new WeaponType[] { WeaponType.Staff } },
        { CharacterType.Thief, new WeaponType[] { WeaponType.Knife } },
        { CharacterType.Warrior, new WeaponType[] { WeaponType.Greatsword} }
    };


    #endregion

    #region Weapon Data
    [Header("AllWeaponStatus")]
    /// <summary>
    /// ゲーム内で使用する武器のWeaponStatus一覧。
    /// </summary>
    public AllWeaponStatus allWeaponStatus;

    /// <summary>
    /// 指定されたレアリティに対応するSwordのWeaponStatusを取得する。
    /// </summary>
    /// <param name="rare">取得するSwordのレアリティ。</param>
    /// <param name="swordStatus">取得したSwordのWeaponStatus。</param>
    /// <returns>
    /// 対応するWeaponStatusを取得できた場合はtrue。
    /// </returns>
    private bool TryGetSwordStatus(uint rare, out WeaponStatus swordStatus)
    {
        swordStatus = default;
        // 使用可能なレアリティの範囲外の場合は取得しない。
        if (rare <= 0 || rare > 5) return false;
        // レアリティに対応するSwordのWeaponStatusを取得する。
        swordStatus = rare switch
        {
            1 => allWeaponStatus.sword_rare_01,
            2 => allWeaponStatus.sword_rare_02,
            3 => allWeaponStatus.sword_rare_03,
            4 => allWeaponStatus.sword_rare_04,
            5 => allWeaponStatus.sword_rare_05,
            _ => default
        };
        // WeaponStatusが設定されている場合のみ取得成功とする。
        return swordStatus != default;
    }

    /// <summary>
    /// 指定されたレアリティに対応するShieldのWeaponStatusを取得する。
    /// </summary>
    /// <param name="rare">取得するShieldのレアリティ。</param>
    /// <param name="swordStatus">取得したShieldのWeaponStatus。</param>
    /// <returns>
    /// 対応するWeaponStatusを取得できた場合はtrue。
    /// </returns>
    private bool TryGetShieldStatus(uint rare, out WeaponStatus swordStatus)
    {
        swordStatus = default;
        // 使用可能なレアリティの範囲外の場合は取得しない。
        if (rare <= 0 || rare > 5) return false;
        // レアリティに対応するShieldのWeaponStatusを取得する。
        swordStatus = rare switch
        {
            1 => allWeaponStatus.shield_rare_01,
            2 => allWeaponStatus.shield_rare_02,
            3 => allWeaponStatus.shield_rare_03,
            4 => allWeaponStatus.shield_rare_04,
            5 => allWeaponStatus.shield_rare_05,
            _ => default
        };
        // WeaponStatusが設定されている場合のみ取得成功とする。
        return swordStatus != default;
    }

    /// <summary>
    /// 武器の種類とレアリティから、
    /// 対応するWeaponStatusを取得する。
    /// </summary>
    /// <param name="weaponType">取得する武器の種類。</param>
    /// <param name="rare">取得する武器のレアリティ。</param>
    /// <param name="weaponStatus">取得したWeaponStatus。</param>
    /// <returns>
    /// 対応するWeaponStatusを取得できた場合はtrue。
    /// </returns>
    public bool TryGetTargetWeaponStatus(WeaponType weaponType, uint rare, out WeaponStatus weaponStatus)
    {
        weaponStatus = default;

        // 武器の種類に対応するWeaponStatusを取得する。
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

        // Sceneが変更されてもGameManagerを保持する。

        DontDestroyOnLoad(this);
        // VSyncを無効化する。/垂直同步off
        QualitySettings.vSyncCount = 0;
        // ゲームの目標Frame Rateを設定する。
        Application.targetFrameRate = gameFps;
    }


}
