using UnityEngine;

/// <summary>
/// 防御行動を行うオブジェクトが実装するインターフェース。
/// </summary>
public interface IDefend
{
    /// <summary>
    /// 防御時に適用するダメージ軽減倍率を取得する。
    /// </summary>
    /// <returns>防御時のダメージ軽減倍率。</returns>
    float GetDefendScale();
    /// <summary>
    /// 防御を開始する。
    /// </summary>
    void DoDefend();
    /// <summary>
    /// 防御を終了する。
    /// </summary>
    void EndDefend();
}
/// <summary>
/// 攻撃を行うオブジェクトが実装するインターフェース。
/// </summary>
public interface IAttack
{
    /// <summary>
    /// 攻撃によって与えるダメージ量を取得する。
    /// </summary>
    /// <returns>与えるダメージ量。</returns>
    public int DoDamage();
}
/// <summary>
/// 武器の種類を表す。
/// </summary>
public enum WeaponType { None, Sword, Shield, Staff, Knife, Greatsword }

/// <summary>
/// すべての武器に共通する基本処理を管理する抽象クラス。
/// 武器の使用者、ステータス、表示用Renderer、
/// 当たり判定などを管理する。
/// </summary>
public abstract class Weapon : MonoBehaviour
{
    /// <summary>
    /// この武器の種類を取得する。
    /// </summary>
    public abstract WeaponType weaponType { get; }
    /// <summary>
    /// この武器を使用しているActor。
    /// </summary>
    public Actor weaponUser;
    /// <summary>
    /// 武器の表示に使用するMeshRenderer。
    /// </summary>
    public MeshRenderer weaponRenderer;
    /// <summary>
    /// 攻撃または防御判定に使用するCollider。
    /// </summary>
    public BoxCollider weaponCollider;
    /// <summary>
    /// この武器に設定されているステータス。
    /// </summary>
    public WeaponStatus weaponStatus;
    /// <summary>
    /// 武器の使用者とステータスを設定し、
    /// 武器に対応するマテリアルを適用する。
    /// </summary>
    /// <param name="master">この武器を使用するActor。</param>
    /// <param name="status">この武器に設定するWeaponStatus。</param>
    public void WeaponInit(Actor master, WeaponStatus status)
    {
        // 武器の使用者を設定する。
        weaponUser = master;
        // 武器のステータスを設定する。
        weaponStatus = status;
        // WeaponStatusに設定されているマテリアルを武器へ適用する。
        weaponRenderer.material = weaponStatus.weaponMaterial;
    }
    /// <summary>
    /// Colliderを有効にしてから、
    /// 最初のリアクションが発生したかを管理する。
    /// </summary>
    protected bool isFirstReaction = true;
    /// <summary>
    /// 武器のColliderを有効にし、
    /// 攻撃または防御判定を開始する。
    /// </summary>
    public virtual void OpenTheBox()
    {
        // 新しい判定として扱うため、リアクション状態を初期化する。
        isFirstReaction = true;
        // 武器の当たり判定を有効にする。
        weaponCollider.enabled = true;
    }
    /// <summary>
    /// 武器のColliderを無効にし、
    /// 攻撃または防御判定を終了する。
    /// </summary>
    public virtual void CloseTheBox()
    {
        // 次回の判定に備えてリアクション状態を初期化する。
        isFirstReaction = true;
        // 武器の当たり判定を無効にする。
        weaponCollider.enabled = false;
    }
    /// <summary>
    /// 武器のColliderが他のColliderと接触した際の処理を行う。
    /// 派生クラスで武器ごとの攻撃・防御処理を実装する。
    /// </summary>
    /// <param name="other">接触したCollider。</param>
    public abstract void WeaponReaction(Collider other);
    /// <summary>
    /// 武器のTriggerに他のColliderが侵入した際、
    /// 武器固有のリアクション処理を実行する。
    /// </summary>
    /// <param name="other">接触したCollider。</param>
    private void OnTriggerEnter(Collider other) => WeaponReaction(other);

}
