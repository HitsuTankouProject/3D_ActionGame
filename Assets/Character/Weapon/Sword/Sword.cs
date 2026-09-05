using UnityEngine;

public class Sword : Weapon
{
    public override WeaponType weaponType => WeaponType.Sword;
    public float atkBuffInfex = 1.01f;
    public override WeaponStatus weaponStatus => new WeaponStatus(atkBuffInfex, 0);

    public override void WeaponReaction(Collider other)
    {
           //effect

        base.WeaponReaction(other);

    }

}
