using DataBase;
using UnityEngine;

[System.Serializable]
public struct PlayerItem
{
    public string item_code;
    public int item_numbers;
    public PlayerItem(string code, int numbers)
    {
        item_code = code;
        item_numbers = numbers;
    }
}

[System.Serializable]
public class PlayerBag
{
    public PlayerItem[] items;
}


public class Player : MonoBehaviour
{
    [Header("Player Data")]
    public PlayerBag bag { get; private set; } = new PlayerBag();
    public void SetPlayerBag(PlayerBag targetData)
    {
        if (targetData == null || targetData.items.Length == 0) return;
        bag = targetData;
    }
    private Status adventurerStatus = new(50, 0, 10, 10, 10, 10, 5, 5);
    private Status magicianStatus = new(50, 0, 5, 5, 20, 5, 10, 5);
    private Status thiefStatus = new(50, 0, 5, 5, 5, 15, 10, 10);
    private Status warriorStatus = new(50, 0, 15, 15, 5, 10, 2, 3);

    public void ResetAllCharacterData()
    {
        adventurerStatus = new(50, 0, 20, 20, 10, 1, 1, 1);
        magicianStatus = new(50, 0, 5, 5, 20, 5, 10, 5);
        thiefStatus = new(50, 0, 5, 5, 5, 15, 10, 10);
        warriorStatus = new(50, 0, 15, 15, 5, 10, 2, 3);

    }


    public void SetAllStatus(AllCharacterData targetData)
    {
        if (targetData == null) return;
        adventurerStatus = targetData.Adventurer;
        magicianStatus = targetData.Magician;
        thiefStatus = targetData.Thief;
        warriorStatus = targetData.Warrior;
    }



    [Header("Lobby")]
    public CharacterType choseCharacter { get; private set; } = CharacterType.Adventurer;

    public void PickAdventurer() => choseCharacter = CharacterType.Adventurer;
    public void PickMagician() => choseCharacter = CharacterType.Magician;
    public void PickThief() => choseCharacter = CharacterType.Thief;
    public void PickWarrior() => choseCharacter = CharacterType.Warrior;



    [Header("InGame")]
    public PlayerBag inGameBag;
    public Character controlling_Character;

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
    public void SetUpCharacter(Character character, HealthBar healthBar, ColorBar colorBar)
    {
        controlling_Character = character;
        controlling_Character.ChangeCanvasBar(healthBar, colorBar);
    }


    public void StartGame()
    {
        //controlling_Character.ChangeHealthBar()
    }



}
