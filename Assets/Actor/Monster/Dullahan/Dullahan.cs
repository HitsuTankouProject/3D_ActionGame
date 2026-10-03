using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class Dullahan : Monster
{
    [Header("Dullahan")]
    [SerializeField] private Weapon subWeapon;
    public override MonsterType monsterType => MonsterType.Dullahan;
    protected override float trackDistance => 6.0f;

    protected override int[] allNormalAttackHashes { get; } =
    {
        Animator.StringToHash("Attack_01"), Animator.StringToHash("Attack_02"),
    };

    protected override void AttackAction()
    {
        Turn(trackingActor.transform.position);
        if (TryUseSkill())
        {
            SetNetworkAction(MonsterAction.Skill);
            return;
        }
        else NormalAttack();

    }


    public virtual void UseSubWeapon()
    {
        if (subWeapon != null) subWeapon.OpenTheBox();
    }
    public virtual void NoneUseSubMainWeapon()
    {
        if (subWeapon != null) subWeapon.CloseTheBox();
    }

    public void UseBothWeapon()
    {
        UseMainWeapon();
        UseSubWeapon();
    }
    public void NoneUseBothWeapon()
    {
        NoneUseMainWeapon();
        NoneUseSubMainWeapon();
    }

}
