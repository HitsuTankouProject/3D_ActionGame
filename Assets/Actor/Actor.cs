using Cysharp.Threading.Tasks;
using Fusion;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


/// <summary>
/// ネットワーク上で同期されるアクターの基本的なステータスを管理するクラス。
/// </summary>
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
/// <summary>
/// アクター共通の処理を提供する基底クラス。
/// </summary>
public abstract class Actor : NetworkBehaviour, IDamageable
{
    protected GameManager _gameManager => GameManager.Instance;
    protected Player _player => _gameManager.gamePlayer;
    protected InGame _inGame => InGame.Instance;
    protected NetworkManager _networkManager => NetworkManager.Instance;


    [Header("Actor Basic")]
    [SerializeField] protected SkinnedMeshRenderer actorBodyMesh;
    /// <summary>
    /// アクターのアニメーションを制御するAnimator。
    /// </summary>
    [SerializeField] protected Animator actorAnimator;
    /// <summary>
    /// アクターのステータス情報。
    /// </summary>
    public ActorStatus actorStatus;

    #region Status

    [Header("Actor Status")]

    #region Hp
    /// <summary>
    /// アクターの最大HP。
    /// </summary>
    public int maxHp;
    /// <summary>
    /// 現在のHP。
    /// HPが変更された場合、HealthBarの表示を更新する。
    /// </summary>
    [Networked, OnChangedRender(nameof(OnHpChanged))]
    public int nowHp { get; protected set; }
    /// <summary>
    /// ネットワーク上で同期される最大HP。
    /// </summary>
    [Networked] protected int networkMaxHp { get; set; }

    /// <summary>
    /// アクターのHPを表示するHealthBar。
    /// </summary>
    public HealthBar healthBar;

    /// <summary>
    /// 現在のHPを基にHealthBarの表示を更新する。
    /// </summary>
    /// <param name="useAnimation">
    /// trueの場合はアニメーション付きで更新し、
    /// falseの場合は即座に更新する。
    /// </param>
    protected void UpdateHealthBar(bool useAnimation)
    {
        if (healthBar == null)
        {
            Debug.LogError("Health Bar is not assigned.", this);
            return;
        }

        if (networkMaxHp <= 0) return;
        // 現在HPを0～1の割合へ変換する。
        float normalizedHp = Mathf.Clamp01((float)nowHp / networkMaxHp);

        if (useAnimation) healthBar.ChangeValueTo(normalizedHp).Forget();
        else healthBar.SetFrontBarValueImmediately(normalizedHp);
    }
    /// <summary>
    /// HPが変更された際にHealthBarを更新する。
    /// </summary>
    protected void OnHpChanged() => UpdateHealthBar(true);

    #endregion

    #region Atk

    /// <summary> 攻撃力レベルの補正を適用した現在の攻撃力を取得します。 </summary>
    public int atkIndex {  get; protected set; }

    #endregion

    #region Def

    /// <summary> 防御力レベルの補正を適用した現在の防御力を取得します。 </summary>
    private int defValue;

    /// <summary>
    /// 現在の防御力倍率。
    /// </summary>
    [Networked]  public float nowDefScale { get; private set; }
    /// <summary>
    /// 防御力倍率を指定された値へ変更します。
    /// </summary>
    /// <param name="targetScale">変更後の防御力倍率。</param>
    public void ChangeDefScale(float target) => nowDefScale = target;
    /// <summary>
    /// 防御力倍率を最小倍率へ戻します。
    /// </summary>
    public void ReturnToMinDefScale() => nowDefScale = actorStatus.actorBasicStatus.actorMinDefScale;

    #endregion

    #region GotHit
    /// <summary>
    /// 受けたダメージと現在の防御力・防御倍率から、
    /// 実際に減少するHP量を計算する。
    /// </summary>
    /// <param name="damage">受ける元のダメージ量。</param>
    /// <returns>防御計算後の最終ダメージ量。</returns>
    protected virtual int GotHitHpLost(int damage)
    {
        // 0以下のダメージは無効とする。
        if (damage <= 0) return 0;

        // 防御力が0以下の場合は、ダメージをそのまま適用する。
        // 通常は発生しない想定だが、0除算を防ぐために確認する。
        if (defValue <= 0) return damage;

        // ダメージが防御力以下の場合は、現在の防御倍率を直接適用する。
        if (damage <= defValue) return Mathf.RoundToInt(damage * (1.0f - nowDefScale));

        // ダメージが防御力の何回分に相当するかを計算する。
        float damage_per_defValue = damage / (float)defValue;
        int totalDamage = 0;
        int maxCalculate = Mathf.CeilToInt(damage_per_defValue);

        for (int calculateTurn = 1; calculateTurn <= maxCalculate; calculateTurn++)
        {
            float index;
            // 計算回数が増えるほど、適用する防御倍率を低下させる。
            float defScale = nowDefScale / calculateTurn;

            // 防御倍率が最低値を下回る場合は最低値に制限し、
            // 残りのダメージをまとめて計算する。
            if (defScale < actorStatus.actorBasicStatus.actorMinDefScale)
            {
                defScale = actorStatus.actorBasicStatus.actorMinDefScale;
                index = damage_per_defValue;
            }
            // 1回につき、防御力1回分までのダメージを計算する。
            else index = Mathf.Min(damage_per_defValue, 1.0f);

            int damageValue = Mathf.RoundToInt(defValue * index * (1 - defScale));
            totalDamage += damageValue;
            damage_per_defValue -= index;

            // すべてのダメージを計算し終えた場合は終了する。
            if (damage_per_defValue <= 0) break;
        }
        return totalDamage;
    }

    /// <summary>
    /// State Authority側でダメージ計算を行い、
    /// 最終ダメージをアクターへ適用する。
    /// </summary>
    /// <param name="damage">受ける元のダメージ量。</param>
    protected virtual void ApplyDamage(int damage)
    {
        if (!Object.HasStateAuthority) return;
        int finalDamage = GotHitHpLost(damage);
        if (finalDamage <= 0) return;
        GotHit(finalDamage);
    }
    /// <summary>
    /// アクターへダメージを与える。
    /// State Authorityを持っていない場合は、
    /// RPCを使用してState Authorityへダメージ処理を要求する。
    /// </summary>
    /// <param name="damage">与えるダメージ量。</param>
    public void TakeDamage(int damage)
    {
        if (damage <= 0) return;

        if (Object.HasStateAuthority)
        {
            ApplyDamage(damage);
            return;
        }

        RPC_RequestDamage(damage);
    }

    protected virtual void GotHit(int damage)
    {
        if (!Object.HasStateAuthority || nowHp == 0) return;

        nowHp = Mathf.Max((nowHp - damage), 0);
        Debug.Log(nowHp, this);
        if (nowHp == 0)
        {
            Death();
            return;
        }

    }
    /// <summary>
    /// State Authorityへダメージ処理を要求するRPC。
    /// </summary>
    /// <param name="damage">与えるダメージ量。</param>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestDamage(int damage) => ApplyDamage(damage);

    #endregion

    #region Death

    /// <summary>
    /// アクターの死亡処理を行う。
    /// 追跡対象として選択できない状態にし、
    /// 現在の追跡情報を解除する。
    /// </summary>
    protected virtual void Death()
    {
        if (!Object.HasStateAuthority) return;

        canBeTrack = false;
        StopTracking();
    }

    #endregion

    /// <summary>
    /// ActorStatusからHP・攻撃力・防御力を取得し、
    /// 戦闘で使用するステータスを初期化する。
    /// </summary>
    protected virtual void AllStatusInit()
    {

        maxHp = actorStatus.FinalHpIndex();
        atkIndex = actorStatus.FinalAtkIndex();
        defValue = actorStatus.FinalDefIndex();
        Debug.Log($"Actor Status Init : MaxHp = {maxHp}, Atk = {atkIndex}, Def = {defValue}", this);

        // 最大HPをネットワーク同期用の値へ設定する。
        networkMaxHp = maxHp;
        // 初期HPを最大HPにする。
        nowHp = networkMaxHp;
        // 防御倍率を初期状態へ戻す。
        ReturnToMinDefScale();
    }

    #endregion

    #region Weapon
    [Header("Weapon")]
    /// <summary>
    /// アクターが現在装備しているメイン武器。
    /// </summary>
    public Weapon mainWeapon;
    /// <summary>
    /// メイン武器を配置するTransform。
    /// </summary>
    [SerializeField] protected Transform mainWeaponTransform;

    /// <summary>
    /// メイン武器の当たり判定を有効にする。
    /// </summary>
    public virtual void UseMainWeapon()
    {
        if (mainWeapon != null) mainWeapon.OpenTheBox();
    }
    /// <summary>
    /// メイン武器の当たり判定を無効にする。
    /// </summary>
    public virtual void NoneUseMainWeapon()
    {
        if (mainWeapon != null) mainWeapon.CloseTheBox();
    }

    #endregion

    #region Track
    /// <summary>
    /// このアクターが他のアクターから追跡対象として選択可能かを表す。
    /// </summary>
    [Networked] public NetworkBool canBeTrack { get; protected set; }
    /// <summary>
    /// 現在追跡しているアクター。
    /// </summary>
    public Actor trackingActor { get; protected set; } = null;
    /// <summary>
    /// 現在追跡しているアクターの位置。
    /// 対象が存在しない場合は自身の前方位置を返す。
    /// </summary>
    protected Vector3 trackingActorPosition => trackingActor == null ? transform.position + transform.forward : trackingActor.transform.position;
    /// <summary>
    /// 現在追跡しているアクターまでの距離。
    /// 対象が存在しない場合は最大値を返す。
    /// </summary>
    protected float trackingActorDistance => trackingActor == null ? float.MaxValue : Vector3.Distance(transform.position, trackingActor.transform.position);
    /// <summary>
    /// 現在の追跡対象が追跡可能距離の外にいるか判定する。
    /// </summary>
    /// <returns>
    /// 対象が存在しない、または追跡距離外の場合はtrue。
    /// </returns>
    protected bool IsTrackingActorOutOfTrackDistance()
    {
        if (trackingActor == null) return true;
        return trackingActorDistance > trackDistance;
    }
    /// <summary>
    /// アクターが追跡可能な最大距離。
    /// </summary>
    protected abstract float trackDistance { get; }
    /// <summary>
    /// 追跡可能なアクターを検索し、
    /// 追跡対象として設定する。
    /// </summary>
    /// <returns>
    /// 追跡対象を取得できた場合はtrue。
    /// </returns>
    protected abstract bool TryTrackActor();
    /// <summary>
    /// 現在の追跡対象を解除する。
    /// </summary>
    protected virtual void StopTracking() => trackingActor = null;
    /// <summary>
    /// 現在の追跡対象が引き続き追跡可能か確認する。
    /// 追跡不可、または追跡距離外になった場合は追跡を解除する。
    /// </summary>
    protected virtual void UpdateTracking()
    {
        if (trackingActor == null) return;
        if (!trackingActor.canBeTrack)
        {
            // 対象が追跡不可状態になった場合は追跡を終了する。
            StopTracking();
            return;
        }
        // 追跡可能距離の外に出た場合も追跡を終了する。
        if (IsTrackingActorOutOfTrackDistance()) StopTracking();
    }

    #endregion

    #region ActionProcess
    /// <summary>
    /// 指定された方向へアクターを移動させる。
    /// </summary>
    /// <param name="moveDirection">移動方向。</param>
    public abstract void Move(Vector3 moveDirection);
    /// <summary>
    /// アクターの回転速度。
    /// </summary>
    protected float turnSpeed = 180.0f;
    /// <summary>
    /// 指定された方向へアクターを向かせる。
    /// </summary>
    /// <param name="direction">向く方向。</param>
    protected virtual void Turn(Vector3 direction) => transform.rotation = Quaternion.LookRotation(direction);
    /// <summary>
    /// 次の行動を実行可能かを表す。
    /// </summary>
    protected bool canDoNextCommand = true;
    /// <summary>
    /// 次の行動を実行可能な状態へ変更する。
    /// </summary>
    public virtual void CanDoNextCommand() => canDoNextCommand = true;

    #endregion

    #region Animation

    /// <summary>
    /// 現在のアニメーションをキャンセルするTrigger Hash。
    /// </summary>
    protected readonly int cancelTriggerHash = Animator.StringToHash("Cancel");

    protected const string runBool = "Run";

    /// <summary>
    /// 通常攻撃アニメーションのTrigger Hash一覧。
    /// </summary>
    protected abstract int[] allNormalAttackHashes { get; }
    /// <summary>
    /// アクティブスキルのTrigger Hash。
    /// </summary>
    protected readonly int activeSkillTriggerHash = Animator.StringToHash("ActiveSkill");
    /// <summary>
    /// 被ダメージアニメーションのTrigger Hash。
    /// </summary>
    protected readonly int gotHitTriggerHash = Animator.StringToHash("Hit");
    /// <summary>
    /// 死亡アニメーションのTrigger Hash。
    /// </summary>
    protected readonly int deathTriggerHash = Animator.StringToHash("Death");


    /// <summary>
    /// すべての通常攻撃Triggerをリセットする。
    /// </summary>
    protected virtual void ReSetAllTheAttackTrigger()
    {
        if (actorAnimator == null) return;
        foreach (var trigger in allNormalAttackHashes)
            actorAnimator.ResetTrigger(trigger);
    }

    #endregion


    /// <summary>
    /// アクター固有の初期化処理を行う。
    /// </summary>
    public abstract void ActorInit();
    /// <summary>
    /// NetworkObjectがDespawnされた際の処理。
    /// </summary>
    /// <param name="runner">使用中のNetworkRunner。</param>
    /// <param name="hasState">
    /// Despawn時にオブジェクトの状態を保持しているか。
    /// </param>
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        

    }


}
