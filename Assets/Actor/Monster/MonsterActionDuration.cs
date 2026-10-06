using UnityEngine;


[CreateAssetMenu(fileName = "MonsterActionDuration", menuName = "Game/Monster Action Duration")]
/// <summary>
/// モンスターAIの行動時間、攻撃距離、
/// 攻撃の重み、スキルのクールダウンなどを管理するScriptableObject。
/// </summary>
public class MonsterActionDuration : ScriptableObject
{


    [Header("Patrol")]
    /// <summary>
    /// 巡回地点へ到着した後の待機時間。
    /// </summary>
    [Range(0.1f, 2.0f)] public float patrolSpotWaitSeconds;
    [Header("Tracking")]
    /// <summary>
    /// 追跡対象を見失った後、
    /// 巡回状態へ戻るまでの待機時間。
    /// </summary>
    [Range(0.1f, 2.0f)] public float lostTrackingWaitSeconds;
    /// <summary>
    /// 次の行動を決定するまでの思考時間。
    /// </summary>
    [Range(0.1f, 1.0f)] public float actionThinkingSeconds;
    [Header("Attack")]
    /// <summary>
    /// 各通常攻撃が選択される確率計算に使用する重み。
    /// </summary>
    public uint[] normalAttackWeights;
    /// <summary>
    /// 通常攻撃を実行可能な距離。
    /// </summary>
    [Range(0.1f, 10.0f)] public float normalAttackDistance;
    /// <summary>
    /// スキル攻撃を実行可能な距離。
    /// </summary>
    [Range(0.1f, 10.0f)] public float skillAttackDistance;
    /// <summary>
    /// スキルを再使用可能になるまでのクールダウン時間。
    /// </summary>
    [Range(0.1f, 50.0f)] public float skillCooldownSeconds;
    /// <summary>
    /// 通常攻撃距離とスキル攻撃距離を比較し、
    /// より近い攻撃可能距離を取得する。
    /// </summary>
    /// <returns>
    /// 通常攻撃距離とスキル攻撃距離のうち、小さい方の値。
    /// </returns>
    public float ClosestAttackDistance()
        => normalAttackDistance < skillAttackDistance ? normalAttackDistance : skillAttackDistance;
}
