using UnityEngine;


[CreateAssetMenu(fileName = "MonsterActionDuration", menuName = "Game/Monster Action Duration")]
public class MonsterActionDuration : ScriptableObject
{
    [Header("Patrol")]
    [Range(0.1f, 2.0f)] public float patrolSpotWaitSeconds;
    [Header("Tracking")]
    [Range(0.1f, 2.0f)] public float lostTrackingWaitSeconds;
    [Range(0.1f, 1.0f)] public float actionThinkingSeconds;
    [Header("Attack")]
    public uint[] normalAttackWeights;
    [Range(0.1f, 10.0f)] public float normalAttackDistance;
    [Range(0.1f, 10.0f)] public float skillAttackDistance;
    [Range(0.1f, 50.0f)] public float skillCooldownSeconds;

    public float ClosestAttackDistance()
        => normalAttackDistance < skillAttackDistance ? normalAttackDistance : skillAttackDistance;
}
