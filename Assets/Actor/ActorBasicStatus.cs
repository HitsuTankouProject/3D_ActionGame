using System;
using System.Collections.Generic;
using UnityEngine;


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
[CreateAssetMenu(fileName = "NewActorBasicStatus", menuName = "Game/Actor Basic Status")]
public class ActorBasicStatus : ScriptableObject
{
    [Range(1.0f, 10.0f)] public float actorMoveSpeed;
    [Range(1, 10000)] public int actorBasicHp;

    [Range(1, 10000)] public int actorBasicAtk;

    [Range(1, 10000)] public int actorBasicDef;
    [Range(0.1f, 0.9f)] public float actorMinDefScale;

    public List<LevelScalePair> allLevelScalePairs = new();

    private void OnValidate()
    {
        const float minimumScaleIncrease = 0.01f;

        for (int i = 0; i < allLevelScalePairs.Count; i++)
        {
            LevelScalePair current = allLevelScalePairs[i];

            int minimumLevel = i == 0
                ? 1
                : allLevelScalePairs[i - 1].level + 1;

            float minimumScale = i == 0
                ? 1f
                : allLevelScalePairs[i - 1].scale + minimumScaleIncrease;

            if (minimumLevel > 99 || minimumScale > 3f)
            {
                Debug.LogWarning(
                    $"LevelScalePair 第 {i} 項無法同時符合遞增與範圍限制。請減少項目或調低前一項的值。",
                    this);
                return;
            }

            current.level = Mathf.Clamp(current.level, minimumLevel, 99);
            current.scale = Mathf.Clamp(current.scale, minimumScale, 3f);
            allLevelScalePairs[i] = current;
        }
    }
}
