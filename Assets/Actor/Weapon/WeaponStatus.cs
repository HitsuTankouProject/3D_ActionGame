using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponStatus", menuName = "Game/Weapon Status")]
public class WeaponStatus : ScriptableObject
{
    [Range(0.0f,2.0f)]public float atkBuffIndex;
    [Range(0.0f, 0.7f)] public float defBuffIndex;

    public Material weaponMaterial;
    public Sprite weaponIcon;


}
