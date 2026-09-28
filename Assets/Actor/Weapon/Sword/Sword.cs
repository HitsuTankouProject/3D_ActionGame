using UnityEngine;

public class Sword : Weapon
{
    public override WeaponType weaponType => WeaponType.Sword;

    public override void WeaponReaction(Collider other)
    {
        if (weaponCollider != null && other.TryGetComponent<IDamageable>(out IDamageable iDamageable))
        {
            int damage = DoDamage();
            iDamageable.TakeDamage(damage);
            CloseTheBox();
        }
    }

}
