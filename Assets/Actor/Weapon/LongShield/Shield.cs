using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Shield : Weapon, IDefend
{
    public override WeaponType weaponType => WeaponType.Shield;


    public float GetDefendScale()
    {
        float defScale = 1 + (isFirstReaction ? weaponStatus.defBuffIndex : weaponStatus.defBuffIndex / 3);
        return weaponUser.nowDefScale * defScale;
    }

    public void DoDefend()
    {
        OpenTheBox();
    }

    public void EndDefend()
    {
        weaponUser.ReturnToMinDefScale();
        CloseTheBox();
    }

    public override void WeaponReaction(Collider other)
    {
        if (!other.gameObject.TryGetComponent<Weapon>(out Weapon attackingWeapon)
            || attackingWeapon.weaponUser == weaponUser) return;

        if (!attackingWeapon.TryGetComponent<IAttack>(out _)) return;

        weaponUser.ChangeDefScale(GetDefendScale());
        isFirstReaction = false;
    }

}
