using UnityEngine;

public class Sword : Weapon, IAttack
{
    public override WeaponType weaponType => WeaponType.Sword;

    public int DoDamage()
    {
        if (weaponUser == null) return 0;

        return Mathf.FloorToInt(weaponUser.atkIndex * weaponStatus.atkBuffIndex);
    }

    public override void WeaponReaction(Collider other)
    {
        if (!isFirstReaction) return;
        IDamageable iDamageable = other.GetComponentInParent<IDamageable>();
        if (iDamageable == null) return;

        int damage = DoDamage();
        iDamageable.TakeDamage(damage);
        isFirstReaction = false;

    }

}
