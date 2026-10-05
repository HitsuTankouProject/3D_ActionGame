using DataBase;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;


public class Player : MonoBehaviour
{
    [Header("Player Data")]
    public PlayerBag bag/* { get; private set; } = new PlayerBag()*/;
    public void SetPlayerBag(PlayerBag targetData)
    {
        if (targetData == null || targetData.items.Length == 0) return;
        bag = targetData;
    }

    private Status adventurerStatus = new(50, 0, 10, 99, 99, 10, 5, 5);
    private Status magicianStatus = new(50, 0, 5, 5, 20, 5, 10, 5);
    private Status thiefStatus = new(50, 0, 5, 5, 5, 15, 10, 10);
    private Status warriorStatus = new(50, 0, 15, 15, 5, 10, 2, 3);

    private void ResetAllCharacterData()
    {
        adventurerStatus = new(50, 0, 20, 20, 10, 1, 1, 1);
        magicianStatus = new(50, 0, 5, 5, 20, 5, 10, 5);
        thiefStatus = new(50, 0, 5, 5, 5, 15, 10, 10);
        warriorStatus = new(50, 0, 15, 15, 5, 10, 2, 3);

    }
    private void ResetPlayerBag()
    {
        bag = new PlayerBag
        {
            items = new PlayerItem[]
            {
                new PlayerItem("Sword_1_1", 1),new PlayerItem("Sword_2_1", 1),new PlayerItem("Sword_3_1", 1),new PlayerItem("Sword_4_1", 1),new PlayerItem("Sword_5_1", 1),
                new PlayerItem("Shield_1_1", 1),new PlayerItem("Shield_1_1", 1),new PlayerItem("Shield_1_1", 1),new PlayerItem("Shield_1_1", 1),new PlayerItem("Shield_1_1", 1)
            }
        };
    }

    public void ResetAllPlayerData()
    {
        ResetAllCharacterData();
        ResetPlayerBag();
    }


    public void SetAllStatus(AllCharacterData targetData)
    {
        if (targetData == null) return;
        adventurerStatus = targetData.Adventurer;
        magicianStatus = targetData.Magician;
        thiefStatus = targetData.Thief;
        warriorStatus = targetData.Warrior;
    }
    public Status TargetCharacterStatus(CharacterType characterType)
    {
        switch (characterType)
        {
            case CharacterType.Adventurer: return adventurerStatus;
            case CharacterType.Magician: return magicianStatus;
            case CharacterType.Thief: return thiefStatus;
            case CharacterType.Warrior: return warriorStatus;

            default: Debug.LogError("How????????"); return default;
        }
    }


    public bool TrySetCharacterStatus(CharacterType characterType, Status status)
    {
        int targetStatus = TargetCharacterStatus(characterType).Lv;
        if (status.Lv < targetStatus) return false;

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
    public CharacterType choseCharacter { get; private set; } = CharacterType.Adventurer;
    public void PickAdventurer() => choseCharacter = CharacterType.Adventurer;
    public void PickMagician() => choseCharacter = CharacterType.Magician;
    public void PickThief() => choseCharacter = CharacterType.Thief;
    public void PickWarrior() => choseCharacter = CharacterType.Warrior;


    [Header("InGame")]
    public Character controlling_Character;
    public PlayerBag inGameBag;
    public PlayerPickWeapon playerPickWeapon { get; private set; } = new PlayerPickWeapon();
    public void SetPlayerPickWeapon(WeaponStatus mainWeaponData, WeaponStatus supWeaponData = null)
    {
        if (choseCharacter == CharacterType.Adventurer)
            playerPickWeapon = new PlayerPickWeapon(mainWeaponData, supWeaponData);
        else
            playerPickWeapon = new PlayerPickWeapon(mainWeaponData);
    }
    
    private List<int> killedMonsterLevel = new();
    public void SetUpCharacter(Character character, HealthBar healthBar, ColorBar colorBar)
    {
        controlling_Character = character;
        controlling_Character.ChangeCanvasBar(healthBar, colorBar);
    }

    public void AddKillMonsterStatus(int level) => killedMonsterLevel.Add(level);

    #region Release
    public int GetTotalKilledMonster() => killedMonsterLevel.Count;
    public int GetTotalKilledMonsterLevel()
    {
        int result = 0;

        if (killedMonsterLevel == null || killedMonsterLevel.Count == 0) return result;

        foreach (int level in killedMonsterLevel) result += level;

        return result;
    }
    public int GetAverageKilledMonsterLevel()
    {
        int result = 0;
        int total = GetTotalKilledMonsterLevel();
        if (total == 0) return 0;

        result = Mathf.RoundToInt(total / killedMonsterLevel.Count);

        return result;
    }


    #endregion

    private void Start()
    {
        ResetAllPlayerData();
    }

}
