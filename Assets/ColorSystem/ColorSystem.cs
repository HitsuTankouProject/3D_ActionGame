using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct ColorMaterial
{
    /// <summary>
    /// メインのボディメッシュに使用するマテリアル。
    /// </summary>
    public Material bodyMaterial;

    /// <summary>
    /// この色レベルで、その他のボディパーツに使用するマテリアル。
    /// </summary>
    public Material[] otherMaterials;

    /// <summary>
    /// メインボディを含むマテリアル設定枠の合計数を取得します。
    /// 配列が設定されていない場合は、メインボディ分の1を返します。
    /// </summary>
    public int materialSlotCount => (otherMaterials?.Length ?? 0) + 1;
}
/// <summary> 色チャージを変更する方向を表します。 </summary>
public enum ColorChargeType { Decrease = -1, Increase = 1 }
/// <summary> 色チャージを減少させることができるオブジェクトを表します。 </summary>
public interface IColorDamageable
{
    /// <summary>
    /// このオブジェクトが使用する色システムを取得します。
    /// </summary>
    ColorSystem actorColorSystem { get; }

    /// <summary>
    /// 指定された量の色チャージを減少させます。
    /// 色チャージが不足している場合は変更しません。
    /// </summary>
    /// <param name="reduceAmount">減少させる色チャージ量。</param>
    /// <returns>
    /// 色チャージを減少できた場合は <see langword="true"/>。
    /// </returns>
    bool TryReduceColor(uint value);

}

[System.Serializable]
public class ColorSystem : MonoBehaviour
{
    /// <summary> 各色喪失レベルで使用するマテリアル。 </summary>
    public List<ColorMaterial> m_AllColorLevels;
    /// <summary> 現在の色喪失レベル。 </summary>
    public int lostColorLevel /*{ get; private set; } */= 0;

    /// <summary>
    /// 現在の色喪失レベルに対応するマテリアルを取得します。
    /// 設定が存在しない場合は空の値を返します。
    /// </summary>
    public ColorMaterial m_colorLevel
    {
        get
        {
            if (m_AllColorLevels == null || lostColorLevel<0|| lostColorLevel >= m_AllColorLevels.Count) return default;
            return m_AllColorLevels[lostColorLevel];
        }
    }

    /// <summary> 各色喪失レベルにおける色チャージの最大値。 </summary>
    public const int maxColorCharge = 100;
    /// <summary> 現在の色チャージ量。 </summary>
    public int nowColorCharge/* { get; private set; }*/ = maxColorCharge;

    /// <summary>
    /// 色チャージを変更できるか判定し、変更後の値を計算します。
    /// このメソッド内では現在値を変更しません。
    /// </summary>
    /// <param name="chargeType">色チャージを変更する方向。</param>
    /// <param name="changeValue">変更する色チャージ量。</param>
    /// <param name="canPassNext">
    /// 不足分または超過分を隣接する色喪失レベルへ繰り越すか。
    /// </param>
    /// <param name="nextCharge">変更後の色チャージ量。</param>
    /// <param name="nextLostLevel">変更後の色喪失レベル。</param>
    /// <returns>
    /// 指定された変更が可能な場合は
    /// <see langword="true"/>。
    /// </returns>
    public bool CanColorCharge(ColorChargeType chargeType, uint changeValue, bool canPassNext, out int nextCharge, out int nextLostLevel)
    {
        nextCharge = nowColorCharge;
        nextLostLevel = lostColorLevel;

        if (m_AllColorLevels == null || m_AllColorLevels.Count == 0)
        {
            Debug.LogError(" 色レベルが設定されていません。");
            return false;
        }
        int signedChangeValue = checked((int)changeValue) * (int)chargeType;
        int result = nowColorCharge + signedChangeValue;
        int maximumLostLevel = m_AllColorLevels.Count - 1;

        switch (chargeType)
        {
            case ColorChargeType.Decrease:

                if (!canPassNext)
                {
                    if (result < 0) return false;

                    nextCharge = result;
                    return true;
                }

                // 不足分を次の色喪失レベルへ繰り越す。
                while (result < 0 && nextLostLevel < maximumLostLevel)
                {
                    nextLostLevel++;
                    result += maxColorCharge;
                }

                // 最大色喪失レベルでも不足する場合は変更できない。
                if (result < 0) return false;

                nextCharge = Mathf.Clamp(result, 0, maxColorCharge);

                return true;

            case ColorChargeType.Increase:

                if (!canPassNext)
                {
                    nextCharge = Mathf.Min(result, maxColorCharge);
                    return true;
                }
                // 余剰チャージを前の色喪失レベルへ繰り越す。
                while (result > maxColorCharge && nextLostLevel > 0)
                {
                    nextLostLevel--;
                    result -= maxColorCharge;
                }
                nextCharge = Mathf.Clamp(result, 0, maxColorCharge);
                return true;

            default: Debug.LogError("How????"); return false;

        }
        
    }

    /// <summary>
    /// 計算済みの色チャージ量と色喪失レベルを反映します。
    /// </summary>
    /// <param name="nextCharge">反映する色チャージ量。</param>
    /// <param name="nextLostLevel">反映する色喪失レベル。</param>
    public void ColorCharge(int nextCharge, int nextLostLevel)
    {
        nowColorCharge = nextCharge;
        lostColorLevel = nextLostLevel;
    }
}
