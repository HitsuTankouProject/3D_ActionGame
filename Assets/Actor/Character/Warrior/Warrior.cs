using UnityEngine;

public class Warrior : Character
{
    public override CharacterType characterType => CharacterType.Warrior;

    protected override float trackDistance => 3.0f;

    public override uint activeSkillCost => 40;

    protected override int[] allNormalAttackHashes { get; } =
{
        Animator.StringToHash("Attack_01"), Animator.StringToHash("Attack_02"),
    };
}
