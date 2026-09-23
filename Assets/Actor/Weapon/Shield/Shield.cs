using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Shield : Weapon
{
    public override WeaponType weaponType => WeaponType.Shield;
    [Header("Shield")]
    public float defBuffIndex = 0.5f;
    public override WeaponStatus weaponStatus => new WeaponStatus(0, defBuffIndex);

    public override void DoDefend() 
    {
        weaponUser.ChangeDefScale(weaponUser.nowDefScale * (1 + defBuffIndex)); 
    }

    public override void EndDefend()
    {
        weaponUser.ReturnToMinDefScale();
    }

    public override void WeaponReaction(Collider other) { }

}
