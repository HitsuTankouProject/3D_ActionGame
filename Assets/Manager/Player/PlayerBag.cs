using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public struct PlayerItemData
{
    public string item_type;
    public uint rare;
    public uint item_id;

    public PlayerItemData(string item_code)
    {
        item_type = string.Empty;
        rare = 0;
        item_id = 0;

        if (string.IsNullOrEmpty(item_code)) return;
        string[] parts = item_code.Split('_');
        if (parts.Length != 3
            || !uint.TryParse(parts[1], out uint rarity)
            || !uint.TryParse(parts[2], out uint itemId))
            return;

        item_type = parts[0];
        rare = rarity;
        item_id = itemId;

    }
}

[System.Serializable]
public struct PlayerItem
{
    public string item_code;
    public int item_numbers;
    public PlayerItemData item_data => new PlayerItemData(item_code);
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

    private bool IsRareAcceptable(uint rare) => rare >= 1 && rare <= 5;
    private bool IsIDAcceptable(uint id) => id >= 1 && id <= 42;

    public List<PlayerItemData> GetAllTargetWeaponData(WeaponType weaponType)
    {
        List<PlayerItemData> allTargetWeaponData = new();
        string targetWeaponType = weaponType.ToString();

        foreach (PlayerItem item in items)
        {
            PlayerItemData itemData = item.item_data;
            if (itemData.item_type != targetWeaponType
                || !IsRareAcceptable(itemData.rare)
                || !IsIDAcceptable(itemData.item_id)) continue;

            allTargetWeaponData.Add(itemData);
        }
        return allTargetWeaponData;
    }

}
