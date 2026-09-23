using Cysharp.Threading.Tasks;
using Fusion;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum ActorType { Character, Monster }

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]

public abstract class Actor : NetworkBehaviour, IDamageable
{
    protected GameManager _gameManager => GameManager.Instance;
    protected InGame _inGame => InGame.Instance;
    protected NetworkManager _networkManager => NetworkManager.Instance;


    [Header("Actor Basic")]
    [SerializeField] protected SkinnedMeshRenderer actorBodyMesh;
    public ActorStatus actorStatus = new();

    [SerializeField] protected Animator actorAnimator;
    [SerializeField] protected CharacterController characterController;


    #region Status
    [Header("Actor Status")]
    public abstract List<LevelScalePair> allLevelScalePairs { get; }

    #region Hp

    public abstract int initHp { get; }
    public int maxHp;

    [Networked, OnChangedRender(nameof(OnHpChanged))]
    public int nowHp { get; protected set; }
    [Networked] protected int networkMaxHp { get; set; }


    public HealthBar healthBar;
    protected void UpdateHealthBar(bool useAnimation)
    {
        if (healthBar == null)
        {
            Debug.LogError("Health Bar is not assigned.", this);
            return;
        }

        if (networkMaxHp <= 0) return;

        float normalizedHp = Mathf.Clamp01((float)nowHp / networkMaxHp);

        if (useAnimation) healthBar.ChangeValueTo(normalizedHp).Forget();
        else healthBar.SetFrontBarValueImmediately(normalizedHp);
    }

    protected void OnHpChanged() => UpdateHealthBar(true);

    #endregion

    #region Atk
    /// <summary>
    /// レベル1時点の基礎攻撃力を取得します。
    /// 派生クラスで職業ごとの値を定義します。
    /// </summary>
    public abstract int initAtk { get; }
    /// <summary> 攻撃力レベルの補正を適用した現在の攻撃力を取得します。 </summary>
    public int atkIndex {  get; protected set; }

    #endregion

    #region Def
    /// <summary>
    /// レベル1時点の基礎防御力を取得します。
    /// 派生クラスでキャラクターごとの値を定義します。
    /// </summary>
    public abstract int initDef { get; }
    /// <summary> 防御力レベルの補正を適用した現在の防御力を取得します。 </summary>
    private int defValue;

    /// <summary>
    /// 防御力に適用する最小倍率を取得します。
    /// 派生クラスで職業ごとの値を定義します。
    /// </summary>
    protected abstract float minDefScale { get; }
    /// <summary>
    /// 現在の防御力倍率。
    /// </summary>
    public float nowDefScale { get; private set; }
    /// <summary>
    /// 防御力倍率を指定された値へ変更します。
    /// </summary>
    /// <param name="targetScale">変更後の防御力倍率。</param>
    public void ChangeDefScale(float target) => nowDefScale = target;
    /// <summary>
    /// 防御力倍率を最小倍率へ戻します。
    /// </summary>
    public void ReturnToMinDefScale() => nowDefScale = minDefScale;

    #endregion

    #region GotHit
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
            if (defScale < minDefScale)
            {
                defScale = minDefScale;
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

    protected virtual void ApplyDamage(int damage)
    {
        if (!Object.HasStateAuthority) return;
        int finalDamage = GotHitHpLost(damage);
        if (finalDamage <= 0) return;
        GotHit(finalDamage);
    }

    public void TakeDamage(int damage)
    {
        Debug.Log(damage);
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

        if (nowHp == 0)
        {
            Death();
            return;
        }

    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestDamage(int damage) => ApplyDamage(damage);

    #endregion

    #region Death

    protected virtual void Death()
    {
        if (!Object.HasStateAuthority) return;

        canBeTrack = false;
        StopTracking();
    }

    #endregion


    protected virtual void AllStatusInit()
    {
        maxHp = actorStatus.FinalHpIndex(initHp);
        atkIndex = actorStatus.FinalAtkIndex(initAtk);
        defValue = actorStatus.FinalDefIndex(initDef);
        networkMaxHp = maxHp;
        nowHp = networkMaxHp;
    }

    #endregion

    #region Weapon
    [Header("Weapon")]
    [SerializeField] protected Weapon mainWeapon;
    public virtual void UseMainWeapon()
    {
        if (mainWeapon != null) mainWeapon.OpenTheBox();
    }
    public virtual void NoneUseMainWeapon()
    {
        if (mainWeapon != null) mainWeapon.CloseTheBox();
    }

    #endregion

    #region Track

    [Networked] public NetworkBool canBeTrack { get; protected set; }
    public Actor trackingActor { get; protected set; } = null;
    protected Vector3 trackingMonsterPosition => trackingActor == null ? transform.position + transform.forward : trackingActor.transform.position;

    protected abstract float trackDistance {  get; }
    protected abstract bool TryTrackActor();
    protected virtual void StopTracking() => trackingActor = null;

    protected virtual void UpdateTracking( float distanceError = 1.0f )
    {
        if (trackingActor == null) return;
        if (!trackingActor.canBeTrack)
        {
            StopTracking();
            return;
        }

        float distance = Vector3.Distance(transform.position, trackingActor.transform.position);

        if(distance > (trackDistance * distanceError)) StopTracking();
    }


    //protected virtual void OnBecameVisible()
    //{
    //    canBeTrack = true;
    //}
    //protected virtual void OnBecameInvisible()
    //{
    //    canBeTrack = false;
    //}

    #endregion

    #region ActionProcess
    protected abstract float moveSpeed { get; }

    public virtual void Move(Vector3 moveDirection)
    {
        if (!Object.HasStateAuthority)return;

        moveDirection.y = 0.0f;

        if (moveDirection.sqrMagnitude < 1.0f) moveDirection.Normalize();

        characterController.Move(moveDirection * moveSpeed * Runner.DeltaTime);

    }
    protected float turnSpeed = 180.0f;
    protected virtual void Turn(Vector3 faceTo, bool isLookAt)
    {
        if (faceTo == Vector3.zero) return;

        Vector3 direction = faceTo - transform.position;
        // 上下方向には回転させない。
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f) return;
        if (isLookAt) transform.rotation = Quaternion.LookRotation(direction);
        else
        {
            Quaternion targetRotation = Quaternion.LookRotation(faceTo.normalized);

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Runner.DeltaTime);
        }
    }

    protected bool canDoNextCommand = true;
    public virtual void CanDoNextCommand()
    {
        canDoNextCommand = true;
    }

    #endregion

    #region Animation

    protected const string cancelTrigger = "Cancel";

    protected const string runBool = "Run";
    protected const string attack01Trigger = "Attack_01";
    protected const string attack02Trigger = "Attack_02";
    protected const string activeSkillTrigger = "ActiveSkill";

    protected const string gotHitTrigger = "Hit";
    protected const string deathTrigger = "Death";

    #endregion



    public abstract void ActorInit();

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        

    }


}
