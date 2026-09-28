using UnityEngine;


public enum WeaponType { Sword, Shield, Staff, Knife, Greatsword }

public abstract class Weapon : MonoBehaviour
{
    public abstract WeaponType weaponType { get; }
    public Actor weaponUser;
    public MeshRenderer weaponRenderer;
    public BoxCollider weaponCollider;
    public WeaponStatus weaponStatus;
    public void WeaponInit(Actor master, WeaponStatus status)
    {
        weaponUser = master;
        weaponStatus = status;
    }
    public virtual void OpenTheBox() => weaponCollider.enabled = true;

    public virtual void CloseTheBox() => weaponCollider.enabled = false;

    public int DoDamage() => Mathf.FloorToInt(weaponUser != null ? weaponUser.atkIndex : 0 * weaponStatus.atkBuffIndex);

    public virtual void DoDefend() { }
    public virtual void EndDefend() { }


    public abstract void WeaponReaction(Collider other);

    private void OnTriggerEnter(Collider other) => WeaponReaction(other);

}
