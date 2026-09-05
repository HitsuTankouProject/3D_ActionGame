using UnityEngine;

public struct WeaponStatus
{
    public float atkBuffInfex;
    public float defBuffInfex;
    public WeaponStatus(float atk, float def)
    {
        atkBuffInfex = atk;
        defBuffInfex = def;
    }
}
public enum WeaponType { Sword, Shield, Staff, Knife, Greatsword }

public abstract class Weapon : MonoBehaviour
{
    public abstract WeaponType weaponType { get; }
    public Character weaponUser;
    public BoxCollider weaponCollider;
    public abstract WeaponStatus weaponStatus {  get; }

    public virtual void OpenTheBox() => weaponCollider.enabled = true;

    public virtual void CloseTheBox() => weaponCollider.enabled = false;

    public int DoDamage() => Mathf.FloorToInt(weaponUser.atkIndex * weaponStatus.atkBuffInfex);
    public abstract void WeaponReaction(Collider other);

    private void OnTriggerEnter(Collider other) => WeaponReaction(other);

}
