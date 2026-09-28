using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Shield : Weapon
{
    public override WeaponType weaponType => WeaponType.Shield;

    public override void DoDefend() 
    {
        OpenTheBox();
    }

    public override void EndDefend()
    {
        weaponUser.ReturnToMinDefScale();
        CloseTheBox();
    }

    public override void WeaponReaction(Collider other) 
    {
        weaponUser.ChangeDefScale(weaponUser.nowDefScale * (1 + weaponStatus.defBuffIndex));
    }

}
