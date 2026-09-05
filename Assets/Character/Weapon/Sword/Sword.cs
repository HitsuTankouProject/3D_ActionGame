using UnityEngine;

public class Sword : Weapon
{
    public override WeaponType weaponType => WeaponType.Sword;
    public float atkBuffInfex;
    public override WeaponStatus weaponStatus => new WeaponStatus(atkBuffInfex, 0);

    public override void WeaponReaction(Collider other)
    {
        if (weaponCollider != null && other.TryGetComponent<IDamage>(out IDamage iDamage))
        {
            int damage = DoDamage();
            iDamage.GotDamage(damage);
        }
    }

}
