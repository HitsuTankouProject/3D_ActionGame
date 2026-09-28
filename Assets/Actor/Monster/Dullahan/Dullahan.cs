using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class Dullahan : Monster
{
    [Header("Dullahan")]
    [SerializeField] private Weapon subWeapon;
    public override MonsterType monsterType => MonsterType.Dullahan;
    protected override float trackDistance => 6.0f;
    protected override float attackDistance => 1.25f;

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
