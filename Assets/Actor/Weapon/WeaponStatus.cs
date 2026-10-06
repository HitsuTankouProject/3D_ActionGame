using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponStatus", menuName = "Game/Weapon Status")]
/// <summary>
/// 武器ごとのステータスと表示情報を管理するScriptableObject。
/// 攻撃補正、防御補正、マテリアル、アイコンを保持する。
/// </summary>
public class WeaponStatus : ScriptableObject
{

    /// <summary>
    /// 武器による攻撃力の補正値。
    /// </summary>
    [Range(0.0f,2.0f)]public float atkBuffIndex;
    /// <summary>
    /// 武器による防御力の補正値。
    /// </summary>
    [Range(0.0f, 0.7f)] public float defBuffIndex;
    /// <summary>
    /// 武器の表示に使用するマテリアル。
    /// </summary>
    public Material weaponMaterial;
    /// <summary>
    /// UI上で武器を表示するために使用するアイコン。
    /// </summary>
    public Sprite weaponIcon;


}
