using UnityEngine;

public class Sword : Weapon
{
    public override WeaponType weaponType => WeaponType.Sword;
    [Header("Sword")]
    public float atkBuffIndex;
    public override WeaponStatus weaponStatus => new WeaponStatus(atkBuffIndex, 0);

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
