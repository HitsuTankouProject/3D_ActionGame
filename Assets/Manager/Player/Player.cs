using DataBase;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// プレイヤーが所有するゲームデータを管理するクラス。
/// キャラクターステータス、バッグ、Lobbyでの選択情報、
/// InGame中のキャラクター情報、モンスター討伐情報などを管理する。
/// </summary>
public class Player : MonoBehaviour
{
    [Header("Player Data")]
    /// <summary>
    /// プレイヤーが所有しているバッグデータ。
    /// </summary>
    public PlayerBag bag { get; private set; }
    /// <summary>
    /// プレイヤーのバッグデータを設定する。
    /// </summary>
    /// <param name="targetData">設定するバッグデータ。</param>
    public void SetPlayerBag(PlayerBag targetData)
    {
        // バッグデータが存在しない、またはアイテムが存在しない場合は設定しない。
        if (targetData == null || targetData.Items.Length == 0) return;
        bag = targetData;
    }
    /// <summary>
    /// Adventurerの現在のステータス。
    /// </summary>
    private Status adventurerStatus;
    /// <summary>
    /// Magicianの現在のステータス。
    /// </summary>
    private Status magicianStatus;
    /// <summary>
    /// Thiefの現在のステータス。
    /// </summary>
    private Status thiefStatus;
    /// <summary>
    /// Warriorの現在のステータス。
    /// </summary>
    private Status warriorStatus;

    /// <summary>
    /// すべてのキャラクターステータスを初期状態へ戻す。
    /// ログインしてないなら、初期状態に戻す際に使用する。
    /// </summary>
    private void ResetAllCharacterData()
    {
        adventurerStatus = new(50, 0, 20, 20, 10, 1, 1, 1);
        magicianStatus = new(50, 0, 5, 5, 20, 5, 10, 5);
        thiefStatus = new(50, 0, 5, 5, 5, 15, 10, 10);
        warriorStatus = new(50, 0, 15, 15, 5, 10, 2, 3);

    }
    /// <summary>
    /// プレイヤーのバッグを初期状態へ戻す。
    /// </summary>
    private void ResetPlayerBag()
    {
        bag = new PlayerBag
        {
            Items = new PlayerItem[]
            {
                new PlayerItem("Sword_1_1", 1),
                new PlayerItem("Sword_2_1", 1),
                new PlayerItem("Sword_3_1", 1),
                new PlayerItem("Sword_4_1", 1),
                new PlayerItem("Sword_5_1", 1),

                new PlayerItem("Shield_1_1", 1),
                new PlayerItem("Shield_2_1", 1),
                new PlayerItem("Shield_3_1", 1),
                new PlayerItem("Shield_4_1", 1),
                new PlayerItem("Shield_5_1", 1)
            }
        };
    }

    /// <summary>
    /// プレイヤーのキャラクターデータとバッグデータを
    /// すべて初期状態へ戻す。
    /// </summary>
    public void ResetAllPlayerData()
    {
        ResetAllCharacterData();
        ResetPlayerBag();
    }

    /// <summary>
    /// すべてのキャラクターステータスを設定する。
    /// </summary>
    /// <param name="targetData">
    /// 設定するキャラクターステータスデータ。
    /// </param>
    public void SetAllStatus(AllCharacterData targetData)
    {
        if (targetData == null) return;
        adventurerStatus = targetData.Adventurer;
        magicianStatus = targetData.Magician;
        thiefStatus = targetData.Thief;
        warriorStatus = targetData.Warrior;
    }
    /// <summary>
    /// 指定されたキャラクターのステータスを取得する。
    /// </summary>
    /// <param name="characterType">取得するキャラクターの種類。</param>
    /// <param name="status">取得したキャラクターステータス。</param>
    /// <returns>
    /// 対応するキャラクターステータスを取得できた場合はtrue。
    /// </returns>
    public bool TryGetCharacterStatus(CharacterType characterType, out Status status)
    {
        status = default;
        // CharacterTypeに対応するステータスを取得する。
        switch (characterType)
        {
            case CharacterType.Adventurer: status = adventurerStatus; break;
            case CharacterType.Magician: status = magicianStatus; break;
            case CharacterType.Thief: status = thiefStatus; break;
            case CharacterType.Warrior: status = warriorStatus; break;

            default: Debug.LogError("How????????"); break;
        }
        return !status.Equals(default);
    }

    /// <summary>
    /// 指定されたキャラクターのステータスを更新する。
    /// 現在のレベルより低いレベルのデータには更新しない。
    /// </summary>
    /// <param name="characterType">更新するキャラクターの種類。</param>
    /// <param name="status">更新後のステータス。</param>
    /// <returns>ステータスを更新できた場合はtrue。</returns>
    public bool TrySetCharacterStatus(CharacterType characterType, Status status)
    {
        // 現在のキャラクターステータスを取得する。
        if (!TryGetCharacterStatus(characterType, out Status targetStatus)) return false;
        // 現在のレベルより低い場合は更新しない。
        if (status.Lv < targetStatus.Lv) return false;
        // CharacterTypeに対応するステータスを更新する。
        switch (characterType)
        {
            case CharacterType.Adventurer:  adventurerStatus = status;  break;
            case CharacterType.Magician:    magicianStatus = status;    break;
            case CharacterType.Thief:       thiefStatus = status;       break;
            case CharacterType.Warrior:     warriorStatus = status;     break;
        }
        return true;
    }



    [Header("Lobby")]
    /// <summary>
    /// Lobbyで現在選択しているキャラクター。
    /// </summary>
    public CharacterType choseCharacter { get; private set; } = CharacterType.Adventurer;
    /// <summary>
    /// Adventurerを使用キャラクターとして選択する。
    /// </summary>
    public void PickAdventurer() => choseCharacter = CharacterType.Adventurer;
    /// <summary>
    /// Magicianを使用キャラクターとして選択する。
    /// </summary>
    public void PickMagician() => choseCharacter = CharacterType.Magician;
    /// <summary>
    /// Thiefを使用キャラクターとして選択する。
    /// </summary>
    public void PickThief() => choseCharacter = CharacterType.Thief;
    /// <summary>
    /// Warriorを使用キャラクターとして選択する。
    /// </summary>
    public void PickWarrior() => choseCharacter = CharacterType.Warrior;
    /// <summary>
    /// Lobbyでプレイヤーが選択した武器ステータス一覧。
    /// </summary>
    public WeaponStatus[] allWeaponStatus { get; private set; }
    /// <summary>
    /// プレイヤーが選択した武器ステータスを設定する。
    /// </summary>
    /// <param name="mainWeaponData">選択した武器ステータス一覧。</param>
    public void SetPlayerPickWeapon(WeaponStatus[] mainWeaponData) => allWeaponStatus = mainWeaponData;


    [Header("InGame")]
    /// <summary>
    /// 現在プレイヤーが操作しているキャラクター。
    /// </summary>
    public Character controlling_Character;
    /// <summary>
    /// InGame中に使用するバッグデータ。
    /// </summary>
    public PlayerBag inGameBag;
    /// <summary>
    /// 倒したモンスターのレベル一覧。
    /// </summary>
    private List<int> killedMonsterLevel = new();
    /// <summary>
    /// プレイヤーが操作するキャラクターと、
    /// キャラクターが使用するUIバーを設定する。
    /// </summary>
    /// <param name="character">操作するキャラクター。</param>
    /// <param name="healthBar">使用するHealthBar。</param>
    /// <param name="colorBar">使用するColorBar。</param>
    public void SetUpCharacter(Character character, HealthBar healthBar, ColorBar colorBar)
    {
        controlling_Character = character;
        // キャラクターが使用するCanvas上のBarを設定する。
        controlling_Character.ChangeCanvasBar(healthBar, colorBar);
    }
    /// <summary>
    /// 倒したモンスターのレベルを記録する。
    /// </summary>
    /// <param name="level">倒したモンスターのレベル。</param>
    public void AddKillMonsterStatus(int level) => killedMonsterLevel.Add(level);

    #region Release
    /// <summary>
    /// 倒したモンスターの総数を取得する。
    /// </summary>
    /// <returns>倒したモンスターの総数。</returns>
    public int GetTotalKilledMonster() => killedMonsterLevel.Count;
    /// <summary>
    /// 倒したすべてのモンスターのレベル合計を取得する。
    /// </summary>
    /// <returns>倒したモンスターのレベル合計。</returns>
    public int GetTotalKilledMonsterLevel()
    {
        int result = 0;
        // 討伐データが存在しない場合は0を返す。
        if (killedMonsterLevel == null || killedMonsterLevel.Count == 0) return result;
        // 倒したすべてのモンスターのレベルを加算する。
        foreach (int level in killedMonsterLevel) result += level;

        return result;
    }
    /// <summary>
    /// 倒したモンスターの平均レベルを取得する。
    /// </summary>
    /// <returns>倒したモンスターの平均レベル。</returns>
    public int GetAverageKilledMonsterLevel()
    {
        int result = 0;
        // 倒したモンスターのレベル合計を取得する。
        int total = GetTotalKilledMonsterLevel();
        if (total == 0) return 0;
        // レベル合計を討伐数で割り、平均レベルを取得する。
        result = Mathf.RoundToInt(total / killedMonsterLevel.Count);

        return result;
    }


    #endregion

    private void Start()
    {
        ResetAllPlayerData();
    }

}
