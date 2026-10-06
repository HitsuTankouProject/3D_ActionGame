using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// アクターのレベルや各ステータスのレベル情報を保持する構造体。
/// </summary>
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

    /// <summary>
    /// 各ステータスの初期値を設定する。
    /// 値はそれぞれ有効範囲内に制限される。
    /// </summary>
    /// <param name="level">キャラクターレベル。</param>
    /// <param name="levelPoint">使用可能なレベルポイント。</param>
    /// <param name="hpLv">HPのレベル。</param>
    /// <param name="defLv">防御力のレベル。</param>
    /// <param name="atkLv">攻撃力のレベル。</param>
    /// <param name="passiveLv">パッシブスキルのレベル。</param>
    /// <param name="activeLv">アクティブスキルのレベル。</param>
    /// <param name="ultLv">アルティメットスキルのレベル。</param>
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
    /// <summary>
    /// 現在のすべてのステータス情報を文字列として取得する。
    /// </summary>
    /// <returns>各ステータスの値をまとめた文字列。</returns>
    public string AllStatus() =>
        $" Lv: {Lv}, LevelPoint: {LevelPoint} \n HpLv: {HpLv}, DefLv: {DefLv}\n" +
        $"AtkLv: {AtkLv}, PassiveLv: {PassiveLv}\n ActiveLv: {ActiveLv}, UltLv: {UltLv}";


}

/// <summary>
/// アクターの基本ステータスとレベル情報を使用して、
/// 最終的なステータス値を計算するクラス。
/// </summary>
[System.Serializable]
public class ActorStatus
{
    /// <summary>
    /// 現在のアクターのステータス情報。
    /// </summary>
    public Status status { get; private set; } = new();
    /// <summary>
    /// アクターの基本ステータスとレベル補正情報。
    /// </summary>
    public ActorBasicStatus actorBasicStatus;
        /// <summary>
    /// Inspector上で値が変更された際に、
    /// 必要なデータが設定されているか確認する。
    /// </summary>
    private void OnValidate()
    {
        if (actorBasicStatus == null) Debug.LogError("Actor Basic Status == null");
     
    }

    /// <summary>
    /// 指定されたレベルを挟む2つのレベル補正データを取得する。
    /// </summary>
    /// <param name="level">検索対象のレベル。</param>
    /// <param name="before">対象レベル以下の補正データ。</param>
    /// <param name="after">対象レベル以上の補正データ。</param>
    /// <returns>対象となる範囲を取得できた場合はtrue。</returns>
    private bool TryGetLevelScaleRange(int level, out LevelScalePair before, out LevelScalePair after)
    {
        before = default;
        after = default;
        List<LevelScalePair> allLevelScalePairs = actorBasicStatus.allLevelScalePairs;
        // 補間には最低2つのレベル補正データが必要
        if (allLevelScalePairs == null || allLevelScalePairs.Count <= 1)
        {
            Debug.LogError("LevelScalePair list is null or empty or Count less then 2.");
            return false;
        }
        // リストの最小レベルと最大レベルを取得する。
        before = allLevelScalePairs[0];
        after = allLevelScalePairs[allLevelScalePairs.Count - 1];
        // 指定されたレベルが補正データの範囲外の場合は計算できない。
        if (level > after.level || level < before.level)
        {
            Debug.LogError($"Level is out off range, Level : {level}");
            return false;
        }
        // 指定されたレベルを挟む2つの補正データを検索する。
        for (int i = 0, j = i + 1; i < allLevelScalePairs.Count - 1 && j < allLevelScalePairs.Count; i++, j++)
        {
            before = allLevelScalePairs[i];
            after = allLevelScalePairs[j];

            if (level >= before.level && level <= after.level) return true;
        }
        // 通常はここまで到達しない。
        Debug.LogError($"How???, Level : {level}");
        return false;


    }

    /// <summary>
    /// 基本ステータスとステータスレベルから、
    /// レベル補正後の最終ステータス値を計算する。
    /// </summary>
    /// <param name="status_init">ステータスの基本値。</param>
    /// <param name="status_lv">計算対象となるステータスレベル。</param>
    /// <returns>レベル補正後の最終ステータス値。</returns>
    private int FinalStatus(int status_init, int status_lv)
    {
        if (!TryGetLevelScaleRange(status_lv, out LevelScalePair before, out LevelScalePair after))
        {
            return 0;
        }
        // before～afterの間で、現在のレベルがどの位置にあるかを0～1で取得する。
        float interpolationRate = Mathf.InverseLerp(before.level, after.level, status_lv);
        // レベルの位置に応じて補正倍率を線形補間する。
        float scale = Mathf.Lerp(before.scale, after.scale, interpolationRate);
        // 基本ステータスに補正倍率を適用して最終値を求める。
        return Mathf.RoundToInt(status_init * scale);
    }
    /// <summary>
    /// 現在のHPレベルから最終HP値を取得する。
    /// </summary>
    public int FinalHpIndex() => FinalStatus(actorBasicStatus.actorBasicHp, status.HpLv);
    /// <summary>
    /// 現在の攻撃力レベルから最終攻撃力を取得する。
    /// </summary>
    public int FinalAtkIndex() => FinalStatus(actorBasicStatus.actorBasicAtk, status.AtkLv);
    /// <summary>
    /// 現在の防御力レベルから最終防御力を取得する。
    /// </summary>
    public int FinalDefIndex() => FinalStatus(actorBasicStatus.actorBasicDef, status.DefLv);

    // これから使う予定
    //public int FinalPassiveIndex(int passive_init) => FinalStatus(passive_init, status.PassiveLv);
    //public int FinalActiveIndex(int active_init) => FinalStatus(active_init, status.ActiveLv);
    //public int FinalUltIndex(int ult_init) => FinalStatus(ult_init, status.UltLv);

    /// <summary>
    /// アクターのステータス情報を初期化する。
    /// </summary>
    /// <param name="targetStatus">設定するステータス情報。</param>
    public void StatusInit(Status targetStatus) => status = targetStatus;
    /// <summary>
    /// 指定されたステータス情報を使用してActorStatusを生成する。
    /// </summary>
    /// <param name="targetStatus">初期ステータス情報。</param>
    public ActorStatus(Status targetStatus) => StatusInit(targetStatus);
}
