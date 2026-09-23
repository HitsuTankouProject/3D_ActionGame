using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct LevelScalePair
{
    /// <summary>倍率が設定される基準レベル。</summary>
    public int level;
    /// <summary>基準レベルにおけるステータス倍率。</summary>
    public float scale;


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
[System.Serializable]
public struct Status
{
    public int Lv;
    public int LevelPoint;
    public int HpLv;
    public int DefLv;
    public int AtkLv;
    public int PassiveLv;
    public int ActiveLv;
    public int UltLv;

    public Status(
        int level,
        int levelPoint,
        int hpLv, int defLv,
        int atkLv, int passiveLv,
        int activeLv, int ultLv)
    {

        this.Lv = Mathf.Clamp(level, 1, 99);
        this.LevelPoint = Mathf.Clamp(levelPoint, 0, 99); ;
        this.HpLv = Mathf.Clamp(hpLv, 1, 99); ;
        this.DefLv = Mathf.Clamp(defLv, 1, 99); ;
        this.AtkLv = Mathf.Clamp(atkLv, 1, 99); ;
        this.PassiveLv = Mathf.Clamp(passiveLv, 1, 99); ;
        this.ActiveLv = Mathf.Clamp(activeLv, 1, 99); ;
        this.UltLv = Mathf.Clamp(ultLv, 1, 99); ;
    }
    public string AllStatus() =>
        $" Lv: {Lv}, LevelPoint: {LevelPoint} \n HpLv: {HpLv}, DefLv: {DefLv}\n" +
        $"AtkLv: {AtkLv}, PassiveLv: {PassiveLv}\n ActiveLv: {ActiveLv}, UltLv: {UltLv}";


}


[System.Serializable]
public class ActorStatus
{
    public Status status { get; private set; } = new();
    public List<LevelScalePair> allLevelScalePairs { get; private set; } = new();

    private bool IsAllLevelScalePairsPass()
    {
        allLevelScalePairs.Sort((firstPair, secondPair) => firstPair.level.CompareTo(secondPair.level));
        LevelScalePair check = new LevelScalePair(int.MaxValue, float.PositiveInfinity);
        for (int i = allLevelScalePairs.Count - 1; i >= 0; i--)
        {
            if (check < allLevelScalePairs[i])
            {
                Debug.LogError($"allLevelScalePairsの設定が不正です");
                return false;
            }
            check = allLevelScalePairs[i];
        }
        return true;


    }
    private void SetAllLevelScalePairs(List<LevelScalePair> target)
    {
        allLevelScalePairs.Clear();

        if (target == null) return;
        allLevelScalePairs.Add(new(1, 1.0f));
        allLevelScalePairs.AddRange(target);

        if (!IsAllLevelScalePairsPass()) return;
    }

    private bool TryGetLevelScaleRange(int level, out LevelScalePair before, out LevelScalePair after)
    {
        before = default;
        after = default;

        if (allLevelScalePairs == null || allLevelScalePairs.Count <=1)
        {
            Debug.LogError("LevelScalePair list is null or empty or Count less then 2.");
            return false;
        }

        before = allLevelScalePairs[0];
        after = allLevelScalePairs[allLevelScalePairs.Count - 1];
        if (level > after.level || level < before.level)
        {
            Debug.LogError($"Level is out off range, Level : {level}");
            return false;
        }

        for (int i = 0, j = i + 1; i < allLevelScalePairs.Count - 1 && j < allLevelScalePairs.Count; i++, j++)
        {
            before = allLevelScalePairs[i];
            after = allLevelScalePairs[j];

            if (level >= before.level && level <= after.level) return true;
        }

        Debug.LogError($"How???, Level : {level}");
        return false;


    }
    private int FinalStatus(int status_init, int status_lv)
    {
        if (!TryGetLevelScaleRange(status_lv, out LevelScalePair before, out LevelScalePair after))
        {
            return 0;
        }

        float interpolationRate = Mathf.InverseLerp(before.level, after.level, status_lv);
        float scale = Mathf.Lerp(before.scale, after.scale, interpolationRate);

        return Mathf.RoundToInt(status_init * scale);
    }

    public void StatusInit(Status targetStatus, List<LevelScalePair> levelScalePairs)
    {
        status = targetStatus;
        SetAllLevelScalePairs(levelScalePairs);
    }

    public int FinalHpIndex(int hp_init) => FinalStatus(hp_init, status.HpLv);
    public int FinalAtkIndex(int atk_init) => FinalStatus(atk_init, status.AtkLv);
    public int FinalDefIndex(int def_init) => FinalStatus(def_init, status.DefLv);
    public int FinalPassiveIndex(int passive_init) => FinalStatus(passive_init, status.PassiveLv);
    public int FinalActiveIndex(int active_init) => FinalStatus(active_init, status.ActiveLv);
    public int FinalUltIndex(int ult_init) => FinalStatus(ult_init, status.UltLv);


    public ActorStatus(Status targetStatus = default, List<LevelScalePair> levelScalePairs = default)
    {
        StatusInit(targetStatus, levelScalePairs);
    }
}
