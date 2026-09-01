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
    public PlayerBag bag;
    public Character controlling_Character;


}
