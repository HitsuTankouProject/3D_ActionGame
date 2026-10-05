using UnityEngine;

public interface IDefend
{
    float GetDefendScale();
    void DoDefend();
    void EndDefend();
}
public interface IAttack
{
    public int DoDamage();
}

public enum WeaponType { None, Sword, Shield, Staff, Knife, Greatsword }

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

    protected bool isFirstReaction = true;
    public virtual void OpenTheBox()
    {
        isFirstReaction = true;
        weaponCollider.enabled = true;
    }

    public virtual void CloseTheBox()
    {
        isFirstReaction = true;
        weaponCollider.enabled = false;
    }

    public abstract void WeaponReaction(Collider other);

    private void OnTriggerEnter(Collider other) => WeaponReaction(other);

}
