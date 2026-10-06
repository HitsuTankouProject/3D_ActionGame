using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 特定レベルにおけるステータス倍率を保持する構造体。
/// ステータスのレベル補正を計算するための基準値として使用する。
/// </summary>
[System.Serializable]
public struct LevelScalePair
{
    /// <summary>倍率が設定される基準レベル。</summary>
    [Range(1, 99)] public int level;
    /// <summary>基準レベルにおけるステータス倍率。</summary>
    [Range(1f, 3f)] public float scale;


    /// <summary>
    /// ステータス補正に使用する基準値を生成します。
    /// </summary>
    /// <param name="level">基準となるレベル。</param>
    /// <param name="scale">基準レベルにおけるステータス倍率。</param>
    public LevelScalePair(int level, float scale)
    {
        this.level = level;
        this.scale = scale;
    }
    public static bool operator ==(LevelScalePair a, LevelScalePair b)
        => a.level.Equals(b.level) && a.scale.Equals(b.scale);
    public static bool operator !=(LevelScalePair a, LevelScalePair b)
        => !a.level.Equals(b.level) || !a.scale.Equals(b.scale);

    public static bool operator >(LevelScalePair a, LevelScalePair b)
        => a.level > b.level && a.scale > b.scale;
    public static bool operator <(LevelScalePair a, LevelScalePair b)
        => a.level < b.level && a.scale < b.scale;

    public override int GetHashCode() => HashCode.Combine(level, scale);

    public override bool Equals(object obj)
    {
        if (obj is LevelScalePair other)
        {
            return Equals(level, other.level) && Equals(scale, other.scale);
        }
        return false;
    }
}

/// <summary>
/// アクターの基本ステータスと、
/// レベルに応じたステータス補正倍率を管理するScriptableObject。
/// </summary>
[CreateAssetMenu(fileName = "NewActorBasicStatus", menuName = "Game/Actor Basic Status")]
public class ActorBasicStatus : ScriptableObject
{
    /// <summary>
    /// アクターの基本移動速度。
    /// </summary>
    [Range(1.0f, 10.0f)] public float actorMoveSpeed;
    /// <summary>
    /// アクターの基本HP。
    /// </summary>
    [Range(1, 10000)] public int actorBasicHp;
    /// <summary>
    /// アクターの基本攻撃力。
    /// </summary>
    [Range(1, 10000)] public int actorBasicAtk;
    /// <summary>
    /// アクターの基本防御力。
    /// </summary>
    [Range(1, 10000)] public int actorBasicDef;
    /// <summary>
    /// アクターが設定可能な最小防御倍率。
    /// </summary>
    [Range(0.1f, 0.9f)] public float actorMinDefScale;
    /// <summary>
    /// レベルごとのステータス補正倍率を設定する基準データ。
    /// </summary>
    public List<LevelScalePair> allLevelScalePairs = new();
    /// <summary>
    /// Inspector上で値が変更された際に、
    /// levelとscaleが前の要素より大きくなるように値を補正する。
    /// </summary>
    private void OnValidate()
    {
        // 前の倍率との差として最低限必要な増加量。
        const float minimumScaleIncrease = 0.01f;

        for (int i = 0; i < allLevelScalePairs.Count; i++)
        {
            LevelScalePair current = allLevelScalePairs[i];
            // 最初の要素はレベル1以上、
            // それ以降は前の要素より1以上大きいレベルにする。
            int minimumLevel = i == 0 ? 1: allLevelScalePairs[i - 1].level + 1;
            // 最初の倍率は1.0以上、
            // それ以降は前の倍率より最低0.01大きくする。
            float minimumScale = i == 0 ? 1f: allLevelScalePairs[i - 1].scale + minimumScaleIncrease;
            // 設定可能な最大値を超えた場合、
            // これ以上正しい昇順データを作成できないため処理を終了する。
            if (minimumLevel > 99 || minimumScale > 3f)
            {
                Debug.LogWarning( $"LevelScalePairの第{i}項は、増加条件と範囲制限を同時に満たせません。項目数を減らすか、前の項目の値を小さくしてください。", this);
                return;
            }
            // levelとscaleを有効範囲内に制限する。
            current.level = Mathf.Clamp(current.level, minimumLevel, 99);
            current.scale = Mathf.Clamp(current.scale, minimumScale, 3f);
            // 変更した値をリストへ戻す。
            allLevelScalePairs[i] = current;
        }
    }
}
