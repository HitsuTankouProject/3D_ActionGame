using System.Collections.Generic;

/// <summary>
/// item_codeから解析したアイテム情報を保持する構造体。
/// アイテムの種類、レアリティ、IDを管理する。
/// </summary>
[System.Serializable]
public struct PlayerItemData
{
    /// <summary>
    /// アイテムの種類。
    /// </summary>
    public string item_type;
    /// <summary>
    /// アイテムのレアリティ。
    /// </summary>
    public uint rare;
    /// <summary>
    /// アイテム固有のID。
    /// </summary>
    public uint item_id;
    /// <summary>
    /// item_codeを解析し、アイテムの種類、
    /// レアリティ、IDを設定する。
    /// </summary>
    /// <param name="item_code">解析するアイテムコード。</param>
    public PlayerItemData(string item_code)
    {
        // 解析に失敗した場合に備えて初期値を設定する。
        item_type = string.Empty;
        rare = 0;
        item_id = 0;
        // item_codeが設定されていない場合は解析しない。
        if (string.IsNullOrEmpty(item_code)) return;
        // "_"を区切りとしてitem_codeを分割する。
        string[] parts = item_code.Split('_');
        // 必要な3つのデータへ分割できない場合、
        // または数値へ変換できない場合は解析を終了する。
        if (parts.Length != 3
            || !uint.TryParse(parts[1], out uint rarity)
            || !uint.TryParse(parts[2], out uint itemId))
            return;
        // 解析したデータを設定する。
        item_type = parts[0];
        rare = rarity;
        item_id = itemId;

    }
}

/// <summary>
/// プレイヤーが所持しているアイテムの情報を保持する構造体。
/// アイテムコードと所持数を管理する。
/// </summary>
[System.Serializable]
public struct PlayerItem
{
    /// <summary>
    /// アイテムの種類、レアリティ、IDを含むアイテムコード。
    /// </summary>
    public string item_code;
    /// <summary>
    /// アイテムの所持数。
    /// </summary>
    public int item_numbers;
    /// <summary>
    /// item_codeを解析したアイテム情報を取得する。
    /// </summary>
    public PlayerItemData item_data => new PlayerItemData(item_code);
    /// <summary>
    /// アイテムコードと所持数を指定してPlayerItemを生成する。
    /// </summary>
    /// <param name="code">設定するアイテムコード。</param>
    /// <param name="numbers">設定するアイテムの所持数。</param>
    public PlayerItem(string code, int numbers)
    {
        item_code = code;
        item_numbers = numbers;
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
    public PlayerItem[] Items;
    /// <summary>
    /// 指定されたレアリティが使用可能な範囲内か確認する。
    /// </summary>
    /// <param name="rare">確認するレアリティ。</param>
    /// <returns>1～5の範囲内の場合はtrue。</returns>
    private bool IsRareAcceptable(uint rare) => rare >= 1 && rare <= 5;
    /// <summary>
    /// 指定されたアイテムIDが使用可能な範囲内か確認する。
    /// </summary>
    /// <param name="id">確認するアイテムID。</param>
    /// <returns>1～42の範囲内の場合はtrue。</returns>
    private bool IsIDAcceptable(uint id) => id >= 1 && id <= 42;
    /// <summary>
    /// バッグ内から指定された武器種類に一致する
    /// 有効な武器データをすべて取得する。
    /// </summary>
    /// <param name="weaponType">取得する武器の種類。</param>
    /// <returns>
    /// 条件に一致した武器データの一覧。
    /// </returns>
    public List<PlayerItemData> GetAllTargetWeaponData(WeaponType weaponType)
    {
        // 条件に一致した武器データを保存するList。
        List<PlayerItemData> allTargetWeaponData = new();
        // WeaponTypeをitem_typeとの比較に使用する文字列へ変換する。
        string targetWeaponType = weaponType.ToString();

        foreach (PlayerItem item in Items)
        {
            // item_codeからアイテム情報を取得する。
            PlayerItemData itemData = item.item_data;

            // 武器種類、レアリティ、IDのいずれかが
            // 条件を満たしていない場合は対象外とする。
            if (itemData.item_type != targetWeaponType
                || !IsRareAcceptable(itemData.rare)
                || !IsIDAcceptable(itemData.item_id)) continue;

            // 条件を満たした武器データを追加する。
            allTargetWeaponData.Add(itemData);
        }
        return allTargetWeaponData;
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
