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
    protected override float moveSpeed => 1.3f;
    protected override float minDefScale => 0.05f;
    public override int initHp => 2000;
    public override int initDef => 100;
    public override int initAtk => 150;
    public override List<LevelScalePair> allLevelScalePairs =>
    new List<LevelScalePair>()
    {
            { new( 20,1.1f) },
            { new( 50,1.7f) },
            { new( 75,2.0f) },
            { new( 99,2.3f) },
    };

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
