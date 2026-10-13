using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

/// <summary>
/// プレイヤーが所持しているアイテムの情報を保持する構造体。
/// アイテムコードと所持数を管理する。
/// </summary>
[System.Serializable]
public class PlayerItem
{
    /// <summary>
    /// アイテムの種類、レアリティ、IDを含むアイテムコード。
    /// </summary>
    public string item_code;

    /// <summary>
    /// アイテムの種類。
    /// </summary>
    public string item_type {  get; private set; }
    /// <summary>
    /// アイテムのレアリティ。
    /// </summary>
    public uint rare { get; private set; }
    /// <summary>
    /// アイテム固有のID。
    /// </summary>
    public uint item_id { get; private set; }

    public void SeparateAllElements()
    {
        // 解析に失敗した場合に備えて初期値を設定する。
        item_type = "";
        rare = 0;
        item_id = 0;
        // item_codeが設定されていない場合は解析しない。
        if (string.IsNullOrEmpty(item_code)) return;
        // "_"を区切りとしてitem_codeを分割する。
        string[] parts = item_code.Split('_');
        // 必要な3つのデータへ分割できない場合、
        // または数値へ変換できない場合は解析を終了する。
        if (parts.Length != 3 || !uint.TryParse(parts[1], out uint rarity) || !uint.TryParse(parts[2], out uint itemId))
        {
            UnityEngine.Debug.LogWarning($"アイテムコードの形式が不正です: {item_code}");
            return;
        }
        // 解析したデータを設定する。
        item_type = parts[0];
        rare = rarity;
        item_id = itemId;

    }

    /// <summary>
    /// アイテムの所持数。
    /// </summary>
    public int item_numbers;

    /// <summary>F
    /// アイテムコードと所持数を指定してPlayerItemを生成する。
    /// </summary>
    /// <param name="code">設定するアイテムコード。</param>
    /// <param name="numbers">設定するアイテムの所持数。</param>
    public PlayerItem(string code, int numbers)
    {
        item_code = code;
        item_numbers = numbers;
        SeparateAllElements();
    }


}

/// <summary>
/// プレイヤーが所持しているアイテムを管理するクラス。
/// バッグ内のアイテムから、条件に一致する武器情報を取得する。
/// </summary>
[System.Serializable]
public class PlayerBag
{
    /// <summary>
    /// プレイヤーが所持しているアイテム一覧。
    /// </summary>
    public PlayerItem[] Items = Array.Empty<PlayerItem>();

    /// <summary>
    /// バッグ内のアイテムをすべて削除する。
    /// </summary>
    public void ResetPlayerBag() => Items = Array.Empty<PlayerItem>();
    /// <summary>
    /// 所持済みのアイテムは数量を増やし、未所持なら追加する。
    /// </summary>
    public void AddToPlayerBag(string addItemCode)
    {
        Items ??= System.Array.Empty<PlayerItem>();

        for (int i = 0; i < Items.Length; i++)
        {
            if (Items[i].item_code != addItemCode) continue;
            Items[i].item_numbers += 1;
            return;
        }

        // 未所持のアイテムを配列の末尾に追加する。
        System.Array.Resize(ref Items, Items.Length + 1);

        Items[Items.Length - 1] = new PlayerItem(addItemCode, 1);
    }
    /// <summary>
    /// 指定されたレアリティが使用可能な範囲内か確認する。
    /// </summary>
    /// <param name="rare">確認するレアリティ。</param>
    /// <returns>1～5の範囲内の場合はtrue。</returns>
    private bool IsRareAcceptable(uint rare) => rare > 0  && rare < 6;
    /// <summary>
    /// 指定されたアイテムIDが使用可能な範囲内か確認する。
    /// </summary>
    /// <param name="id">確認するアイテムID。</param>
    /// <returns>1～42の範囲内の場合はtrue。</returns>
    private bool IsIDAcceptable(uint id) => id > 0 && id < 43;
    /// <summary>
    /// バッグ内から指定された武器種類に一致する
    /// 有効な武器データをすべて取得する。
    /// </summary>
    /// <param name="weaponType">取得する武器の種類。</param>
    /// <returns>
    /// 条件に一致した武器データの一覧。
    /// </returns>
    public PlayerItem[] GetAllTargetWeaponData(WeaponType weaponType)
    {
        // 条件に一致した武器データを保存するList。
        List<PlayerItem> allTargetWeaponData = new();
        // WeaponTypeをitem_typeとの比較に使用する文字列へ変換する。
        string targetWeaponType = weaponType.ToString();

        foreach (PlayerItem item in Items)
        {
            // 武器種類、レアリティ、IDのいずれかが
            // 条件を満たしていない場合は対象外とする。
            if (item.item_type != targetWeaponType
                || !IsRareAcceptable(item.rare)
                || !IsIDAcceptable(item.item_id)) continue;

            // 条件を満たした武器データを追加する。
            allTargetWeaponData.Add(item);
        }
        return allTargetWeaponData.ToArray();
    }
    public string DeBugPrintAllItems()
    {
        string result = string.Empty;
        foreach (PlayerItem item in Items)
        {
            result += $"Item Code: {item.item_code}, Item Numbers: {item.item_numbers}\n";
        }
        return result;
    }

}
