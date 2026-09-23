using UnityEngine;

public struct WeaponStatus
{
    public float atkBuffIndex;
    public float defBuffIndex;
    public WeaponStatus(float atk, float def)
    {
        atkBuffIndex = atk;
        defBuffIndex = def;
    }
}
public enum WeaponType { Sword, Shield, Staff, Knife, Greatsword }

public abstract class Weapon : MonoBehaviour
{
    public abstract WeaponType weaponType { get; }
    public Actor weaponUser;

    public BoxCollider weaponCollider;
    public abstract WeaponStatus weaponStatus {  get; }

    public virtual void OpenTheBox() => weaponCollider.enabled = true;

    public virtual void CloseTheBox() => weaponCollider.enabled = false;

    public int DoDamage() => Mathf.FloorToInt(weaponUser != null ? weaponUser.atkIndex : 0 * weaponStatus.atkBuffIndex);

    public virtual void DoDefend() { }
    public virtual void EndDefend() { }


    public abstract void WeaponReaction(Collider other);

    private void OnTriggerEnter(Collider other) => WeaponReaction(other);

}
