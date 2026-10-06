using Cysharp.Threading.Tasks;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

/// <summary>
/// モンスターの種類。
/// </summary>
public enum MonsterType
{
    Dullahan, Golem, Wolf
}
/// <summary>
/// モンスターの現在の状態。
/// </summary>
public enum MonsterStage { Idle, Patrol, Tracking, GetHit, Death }
/// <summary>
/// モンスターが現在実行している行動。
/// </summary>
public enum MonsterAction { None, Run, Thinking, Attack, Skill }
/// <summary>
/// モンスター共通移動、巡回、追跡クラス
/// </summary>
[RequireComponent(typeof(PatrolPath))]
[RequireComponent(typeof(NavMeshAgent))]
/// <summary>
/// モンスター共通の管理の基底クラス。
/// </summary>
public abstract class Monster : Actor
{

    [Header("Monster Basic")]
    /// <summary>
    /// モンスターの種類。
    /// </summary>
    public abstract MonsterType monsterType { get; }
    /// <summary>
    /// プレイヤーを追跡可能な最大距離。
    /// </summary>
    protected override float trackDistance { get; }

    [Header("Monster Status")]
    /// <summary>
    /// 現在のモンスターレベル。
    /// </summary>
    public int monsterLevel { get; private set; } = 10;
    /// <summary>
    /// ゲームに参加しているプレイヤーの平均レベルから、
    /// モンスターのレベルを設定する。
    /// </summary>
    public void SetMonsterLevel()
    {
        int totalLevel = 0;
        // 全プレイヤーのレベルを合計する。
        foreach (Character character in _inGame.allPlayerCharacters)
        {
            totalLevel += character.actorStatus.status.Lv;
        }

        // プレイヤーの平均レベルをモンスターレベルとして使用する。
        monsterLevel = Mathf.RoundToInt(totalLevel/ _inGame.allPlayerCharacters.Count);
        // モンスターレベルを10～99の範囲に制限する。
        monsterLevel = Mathf.Clamp(monsterLevel, 10, 99);
    }
    /// <summary>
    /// 指定された合計値を、指定された個数の正の整数へランダムに分配する。
    /// </summary>
    /// <param name="targetNumber">分配する合計値。</param>
    /// <param name="numberCount">生成する数値の個数。</param>
    /// <returns>
    /// 合計がtargetNumberになるランダムな整数リスト。
    /// 条件を満たさない場合は空のリストを返す。
    /// </returns>
    private List<int> CreateRandomNumbers(int targetNumber, int numberCount)
    {
        List<int> randomNumbers = new();

        if (targetNumber <= 0 || numberCount <= 0)
        {
            Debug.LogError("Target number or Number count must be more than 0.");
            return randomNumbers;
        }
        // 各要素へ最低1を割り当てるため、
        // 合計値は要素数以上である必要がある。
        if (targetNumber < numberCount)
        {
            Debug.LogError($"Target number must be at least {numberCount}.");
            return randomNumbers;
        }
        // 最後の1要素を除いてランダムに値を決定する。
        int remainingNumber = targetNumber;

        for (int i = 0; i < numberCount - 1; i++)
        {
            int remainingSlots = numberCount - i - 1;

            // 残りの各要素へ最低1を残した状態で、
            // 現在の要素に設定可能な最大値を計算する。
            int maximumNumber = remainingNumber - remainingSlots;

            int randomNumber = Random.Range(1, maximumNumber + 1);

            randomNumbers.Add(randomNumber);

            remainingNumber -= randomNumber;
        }
        // 残った値を最後の要素として追加する。
        randomNumbers.Add(remainingNumber);
        // 生成された値の順番をランダムに並び替える。
        for (int i = randomNumbers.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            (randomNumbers[i], randomNumbers[randomIndex]) = (randomNumbers[randomIndex], randomNumbers[i]);
        }

        return randomNumbers;
    }

    /// <summary>
    /// モンスターレベルをHP・防御力・攻撃力・
    /// アクティブスキルレベルへランダムに分配し、
    /// モンスター用のステータスを生成する。
    /// </summary>
    /// <returns>生成されたモンスターのステータス。</returns>
    protected virtual Status MonsterStatus()
    {
        Status status = new();
        List<int> list = CreateRandomNumbers(monsterLevel, 4);
        status.Lv = monsterLevel;

        status.HpLv = list[0];
        status.DefLv = list[1];
        status.AtkLv = list[2];
        status.ActiveLv = list[3];

        return status;
    }

    [Header("Monster Action Decision")]
    /// <summary>
    /// モンスターの行動時間、攻撃距離、
    /// スキルクールダウンなどの設定。
    /// </summary>
    [SerializeField] private MonsterActionDuration monsterActionDuration;
    /// <summary>
    /// 現在のモンスターの状態。
    /// </summary>
    [Networked] public MonsterStage stage { get; protected set; }
    /// <summary>
    /// 現在実行している行動。
    /// </summary>
    [Networked] public MonsterAction currentAction { get; protected set; }
    /// <summary>
    /// 状態や行動の待機時間を管理するタイマー。
    /// </summary>
    [Networked] private TickTimer stateTimer { get; set; }
    /// <summary>
    /// StateTimerが現在動作中か確認する。
    /// </summary>
    private bool IsStateTimerRunning() => stateTimer.IsRunning && !stateTimer.Expired(Runner);
    /// <summary>
    /// StateTimerが使用されていない場合、
    /// 指定された時間でタイマーを開始する。
    /// </summary>
    /// <param name="limitTime">タイマーの時間。</param>
    /// <returns>タイマーを開始できた場合はtrue。</returns>
    private bool TrySetStateTimer(float limitTime)
    {
        if (IsStateTimerRunning())
        {
            Debug.LogWarning("stateTimer is using");
            return false;
        }
        stateTimer = TickTimer.CreateFromSeconds(Runner, limitTime);
        return stateTimer.IsRunning;
    }
    /// <summary>
    /// 指定された位置の方向へモンスターを回転させる。
    /// Y軸方向の差は回転計算から除外する。
    /// </summary>
    /// <param name="targetPosition">向く対象の位置。</param>
    protected override void Turn(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0.0f;

        if (direction.sqrMagnitude <= 0.0f)
            return;

        transform.rotation = Quaternion.LookRotation(direction);
    }
    /// <summary>
    /// NavMeshAgentを使用して指定された位置へ移動する。
    /// </summary>
    /// <param name="targetPosition">移動先の位置。</param>
    public override void Move(Vector3 targetPosition)
    {
        if (!Object.HasStateAuthority || !navMeshAgent.isOnNavMesh) return;
        navMeshAgent.speed = actorStatus.actorBasicStatus.actorMoveSpeed;
        Turn(targetPosition);
        navMeshAgent.SetDestination(targetPosition);
        SetNetworkActionIfChanged(MonsterAction.Run);
    }
    /// <summary>
    /// 指定された位置から一定距離を保つように移動する。
    /// </summary>
    /// <param name="targetPosition">移動対象の位置。</param>
    /// <param name="stoppingDistance">対象から停止する距離。</param>
    protected void MoveAndStopInDistance(Vector3 targetPosition, float stoppingDistance = 0.1f)
    {
        if (!Object.HasStateAuthority || !navMeshAgent.isOnNavMesh) return;
        navMeshAgent.stoppingDistance = stoppingDistance;
        Move(targetPosition);
    }



    #region Stage Process
    [Header(" Stage Process")]
    /// <summary>
    /// モンスターが巡回する経路。
    /// </summary>
    [SerializeField] protected PatrolPath patrolPath;
    /// <summary>
    /// モンスターのNavMesh移動を制御するAgent。
    /// </summary>
    [SerializeField] protected NavMeshAgent navMeshAgent;

    /// <summary>
    /// モンスター付近のNavMeshを検索し、
    /// 見つかった位置へNavMeshAgentを配置する。
    /// </summary>
    /// <returns>NavMeshを取得できた場合はtrue。</returns>
    private bool TryGetNavMesh()
    {
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas)) return false;

        navMeshAgent.Warp(hit.position);
        return true;
    }

    /// <summary>
    /// NavMeshAgentが現在移動中か確認する。
    /// </summary>
    private bool IsNavMeshMoving()
    {
        if (navMeshAgent == null || !navMeshAgent.isActiveAndEnabled || !navMeshAgent.isOnNavMesh) return false;

        return navMeshAgent.velocity.sqrMagnitude > 0.01f;
    }
    /// <summary>
    /// NavMeshAgentが現在の目的地へ到着したか確認する。
    /// </summary>
    private bool HasReachedSpot()
    {
        if (navMeshAgent == null 
            ||!navMeshAgent.isActiveAndEnabled 
            ||!navMeshAgent.isOnNavMesh 
            ||navMeshAgent.pathPending ||!navMeshAgent.hasPath)
            return false;

        return navMeshAgent.pathStatus == NavMeshPathStatus.PathComplete &&
               navMeshAgent.remainingDistance <= Mathf.Max(navMeshAgent.stoppingDistance, 0.2f);
    }
    /// <summary>
    /// NavMeshAgentの現在の移動経路を解除する。
    /// </summary>
    private void StopNavMeshMovement()
    {
        if (navMeshAgent == null || 
            !navMeshAgent.isActiveAndEnabled || !navMeshAgent.isOnNavMesh) return;
        navMeshAgent.ResetPath();
    }
    /// <summary>
    /// 追跡可能範囲内にいる最も近いプレイヤーを検索し、
    /// 追跡対象として設定する。
    /// </summary>
    /// <returns>追跡対象を取得できた場合はtrue。</returns>
    protected override bool TryTrackActor()
    {
        if (_inGame == null || _inGame.allPlayerCharacters == null) return false;

        // すでに追跡対象が存在する場合は、
        // 現在も追跡可能範囲内か確認する。
        if (trackingActor != null) return !IsTrackingActorOutOfTrackDistance(); 

        trackingActor = null;
        float closestDistance = float.MaxValue;

        foreach (Character character in _inGame.allPlayerCharacters)
        {
            if (character == null || character.Object == null) continue;
            if (!character.canBeTrack) continue;

            float distance = Vector3.Distance(transform.position, character.transform.position);

            // 追跡距離外、または現在の候補より遠い場合は除外する。
            if (distance > trackDistance || distance >= closestDistance) continue;

            closestDistance = distance;
            trackingActor = character;
        }

        return trackingActor != null;
    }

    #region Idle

    /// <summary>
    /// モンスターをIdle状態へ戻す。
    /// 移動・行動・タイマーをリセットする。
    /// </summary>
    public virtual void ReturnIdle()
    {
        if (!Object.HasStateAuthority) return;

        StopNavMeshMovement();
        SetNetworkAction(MonsterAction.None);
        stateTimer = TickTimer.None;
        stage = MonsterStage.Idle;
    }

    /// <summary>
    /// Idle状態の更新処理。
    /// 追跡対象が存在する場合はTrackingへ、
    /// 存在しない場合はPatrolへ移行する。
    /// </summary>
    private void UpdateIdle()
    {
        if (!Object.HasStateAuthority) return;
        // if Can track character -> stage = tracking;
        // false -> stage = Patrol
        if (stateTimer.IsRunning && !stateTimer.Expired(Runner)) return;
        
        if (TryTrackActor()) stage = MonsterStage.Tracking;
        else stage = MonsterStage.Patrol;

    }

    #endregion

    #region Patrol
    /// <summary>
    /// 現在選択されている巡回地点のIndex。
    /// </summary>
    private int patrolIndex = 0;
    /// <summary>
    /// 次に移動する巡回地点をランダムに選択する。
    /// 可能な場合は現在の地点と同じ地点を避ける。
    /// </summary>
    /// <returns>
    /// 選択された巡回地点のIndex。
    /// 取得できない場合は-1。
    /// </returns>
    private int GetPatrolPointIndex()
    {
        if (patrolPath == null || patrolPath.pointCount <= 0)
        {
            Debug.LogWarning("Monster Patrol Point = null", this);
            return -1;
        }
        if (patrolPath.pointCount == 1) return 0;

        int index = patrolIndex;

        do index = Random.Range(0, patrolPath.pointCount);
        while (index == patrolIndex);

        return index;

    }
    /// <summary>
    /// 次の巡回地点を選択し、その地点へ移動する。
    /// </summary>
    private void GoToNextPatrolPoint()
    {
        patrolIndex = GetPatrolPointIndex();
        if (patrolIndex == -1) return;

        Vector3 targetPosition = patrolPath.GetPoint(patrolIndex);
        Move(targetPosition);
    }

    /// <summary>
    /// Patrol状態の更新処理。
    /// プレイヤーの検索、巡回地点への移動、
    /// 到着後の待機処理を行う。
    /// </summary>
    private void UpdatePatrol()
    {
        if (!Object.HasStateAuthority || IsStateTimerRunning()) return;

        // 追跡対象を発見した場合はTrackingへ移行する。
        if (TryTrackActor())
        {
            stage = MonsterStage.Tracking;
            return;
        }
        // 移動中の場合は現在の移動を継続する。
        if (IsNavMeshMoving()) return;
        // 巡回地点へ到着した場合は一定時間待機する。
        if (HasReachedSpot()) 
        {
            if (!TrySetStateTimer(monsterActionDuration.patrolSpotWaitSeconds)) return;
            StopNavMeshMovement();
            SetNetworkAction(MonsterAction.Thinking);
        }
        else GoToNextPatrolPoint();

    }

    #endregion

    #region Attack
    /// <summary>
    /// 現在の追跡対象が通常攻撃可能距離内にいるか確認する。
    /// </summary>
    protected bool CanNormalAttack() => trackingActorDistance < monsterActionDuration.normalAttackDistance;
    /// <summary>
    /// 現在の追跡対象がスキル攻撃可能距離内にいるか確認する。
    /// </summary>
    protected bool CanSkillAttack() => trackingActorDistance < monsterActionDuration.skillAttackDistance;
    /// <summary>
    /// 現在の追跡対象に対して、
    /// 通常攻撃またはスキルを使用可能か確認する。
    /// </summary>
    protected bool CanAttack() => CanNormalAttack() || CanSkillAttack();
    /// <summary>
    /// スキルのクールダウンを管理するタイマー。
    /// </summary>
    [Networked] private TickTimer skillTimer { get; set; }
    /// <summary>
    /// 現在のスキルクールダウン残り時間。
    /// </summary>
    public float skillCooldownRemaining
    {
        get
        {
            if (IsSkillOnCooldown()) return skillTimer.RemainingTime(Runner) ?? 0.0f;
            else return 0.0f;
        }
    }
    /// <summary>
    /// スキルが現在クールダウン中か確認する。
    /// </summary>
    private bool IsSkillOnCooldown()=> skillTimer.IsRunning && !skillTimer.Expired(Runner);
    /// <summary>
    /// スキルがクールダウン中でない場合、
    /// 新しいクールダウンを開始する。
    /// </summary>
    /// <returns>開始できた場合はtrue。</returns>
    private bool TryStartSkillCooldown()
    {
        if(IsSkillOnCooldown()) return false;
        skillTimer = TickTimer.CreateFromSeconds(Runner, monsterActionDuration.skillCooldownSeconds);

        return skillTimer.IsRunning;

    }
    /// <summary>
    /// スキルの使用を試みる。
    /// クールダウンを開始できた場合はSkill状態へ変更する。
    /// </summary>
    /// <returns>スキルを使用可能な場合はtrue。</returns>
    protected virtual bool TryUseSkill()
    {
        if(!TryStartSkillCooldown())return false;
        SetNetworkAction(MonsterAction.Skill);
        return true;
    }

    /// <summary>
    /// すべての通常攻撃の重みの合計値。
    /// </summary>
    private uint allNormalAttackWeights = default;
    /// <summary>
    /// 通常攻撃アニメーションと重み設定が
    /// 正しく設定されているか確認する。
    /// </summary>
    /// <returns>設定に問題がない場合はtrue。</returns>
    private bool IsAllAttackWeightsSetUp()
    {
        if (monsterActionDuration.normalAttackWeights.Length != allNormalAttackHashes.Length || monsterActionDuration.normalAttackWeights.Length <= 0) return false;
        uint totalWeight = 0;
        foreach (uint weight in monsterActionDuration.normalAttackWeights)
        {
            // 重み0の攻撃は許可しない。
            if (weight == 0)
            {
                Debug.LogError("Normal attack weight cannot be 0.");
                return false;
            }
            totalWeight += weight;
        }
        allNormalAttackWeights = totalWeight;
        return true;

    }

    /// <summary>
    /// 設定された攻撃重みに基づいて、
    /// 使用する通常攻撃のIndexをランダムに決定する。
    /// </summary>
    /// <param name="currentIndex">
    /// 選択された通常攻撃のIndex。
    /// </param>
    /// <returns>攻撃を選択できた場合はtrue。</returns>
    private bool TryGetNormalAttackIndex(out int currentIndex)
    {
        currentIndex = -1;
        uint randomWeight = (uint)Random.Range(0, (int)allNormalAttackWeights);

        uint currentWeight = 0;

        for (int i = 0; i < monsterActionDuration.normalAttackWeights.Length; i++)
        {
            currentWeight += monsterActionDuration.normalAttackWeights[i];

            if (randomWeight < currentWeight) 
            { 
                currentIndex = i;
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// 通常攻撃状態へ変更する。
    /// </summary>
    protected virtual void NormalAttack() => SetNetworkAction(MonsterAction.Attack);
    #endregion

    #region Tracking

    /// <summary>
    /// 現在位置から最も近い巡回地点。
    /// </summary>
    private Vector3 theClosePatrolSpot = Vector3.positiveInfinity;
    /// <summary>
    /// 現在位置から最も近い巡回地点を検索する。
    /// </summary>
    /// <returns>最も近い巡回地点の位置。</returns>
    private Vector3 FindTheClosePatrolSpot()
    {
        Vector3 closePatrolSpot = Vector3.positiveInfinity;
        float closestDistance = float.MaxValue;
        foreach (Transform spot in patrolPath.patrolPoints)
        {
            float distance = Vector3.Distance(transform.position, spot.position);
            if ( distance > closestDistance) continue;

            closestDistance = distance;
            closePatrolSpot = spot.position;
        }

        return closePatrolSpot;
    }
    /// <summary>
    /// 最も近い巡回地点が追跡可能範囲外にあるか確認する。
    /// </summary>
    /// <returns>追跡可能範囲外の場合はtrue。</returns>
    private bool IsTheClosePatrolSpotOutOfTrackDistance()
    {
        if (theClosePatrolSpot != Vector3.positiveInfinity 
            && Vector3.Distance(transform.position, theClosePatrolSpot) < trackDistance)
            return false;

        theClosePatrolSpot = FindTheClosePatrolSpot();

        return Vector3.Distance(transform.position, theClosePatrolSpot) > trackDistance;
    }

    /// <summary>
    /// 追跡対象を見失った後の処理中かを表す。
    /// </summary>
    private bool isLostTracking = false;
    /// <summary>
    /// 追跡対象を見失った際に、
    /// 最も近い巡回地点へ戻る処理を行う。
    /// </summary>
    private void LostTracking()
    {
        // 初回は待機タイマーを開始し、
        // 最も近い巡回地点へ移動する。
        if (!isLostTracking)
        {
            if(!TrySetStateTimer(monsterActionDuration.lostTrackingWaitSeconds)) return;
            isLostTracking = true;
            SetNetworkAction(MonsterAction.Run);
            Move(theClosePatrolSpot);
            return;
        }
        // 移動処理終了後に追跡状態をリセットする。
        isLostTracking = false;
        StopNavMeshMovement();
        SetNetworkAction(MonsterAction.Thinking);
        theClosePatrolSpot = Vector3.positiveInfinity;
        ReturnIdle();
    }

    /// <summary>
    /// モンスター固有の攻撃行動を実行する。
    /// </summary>
    protected abstract void AttackAction();
    /// <summary>
    /// 追跡対象との距離に応じて移動または攻撃を行う。
    /// </summary>
    protected virtual void Tracking()
    {
        isLostTracking = false;
        // 攻撃距離外の場合は、
        // 最も近い攻撃可能距離まで移動する。
        if (!CanAttack())
        {
            MoveAndStopInDistance(trackingActor.transform.position, monsterActionDuration.ClosestAttackDistance());
            return;
        }
        // 次の行動が許可されていない、
        // または思考時間を開始できない場合は待機する。
        if (!canDoNextCommand || !TrySetStateTimer(monsterActionDuration.actionThinkingSeconds)) return;
        StopNavMeshMovement();
        canDoNextCommand = false;
        AttackAction();
    }

    /// <summary>
    /// Tracking状態の更新処理。
    /// 追跡可能範囲を確認しながら追跡・攻撃処理を行う。
    /// </summary>
    protected override void UpdateTracking()
    {
        if (!Object.HasStateAuthority || IsStateTimerRunning()) return;

        // 巡回範囲または追跡距離から外れた場合、
        // 追跡終了処理へ移行する。
        if (IsTheClosePatrolSpotOutOfTrackDistance() 
            || IsTrackingActorOutOfTrackDistance()) 
        {
            LostTracking();
            return;
        }

        Tracking();

    }


    #endregion

    #region GotHit
    /// <summary>
    /// 基底クラスのダメージ計算を使用して、
    /// モンスターが実際に受けるダメージを取得する。
    /// </summary>
    protected override int GotHitHpLost(int damage)
    {
        int result =  base.GotHitHpLost(damage);
        Debug.Log($" {nowDefScale}, got hit: {damage}, nowHp: {nowHp}");
        return result;
    }
    /// <summary>
    /// ダメージ適用後のHPに応じて、
    /// GetHitまたはDeath状態へ変更する。
    /// </summary>
    protected override void GotHit(int damage)
    {
        base.GotHit(damage);

        if (nowHp > 0) stage = MonsterStage.GetHit;
        else stage = MonsterStage.Death;
    }
    /// <summary>
    /// GetHit状態の更新処理。
    /// 被ダメージアニメーションを再生してIdleへ戻る。
    /// </summary>
    private void UpdateGetHit()
    {
        if (!canDoNextCommand) return;
        SetGetHitAnimation();
        canDoNextCommand = false;
        ReturnIdle();
    }




    #endregion

    #region Death
    /// <summary>
    /// 死亡処理がすでに開始されているかを表す。
    /// </summary>
    private bool isStartDeathProcessing = false;
    /// <summary>
    /// モンスターの死亡処理を行い、
    /// InGameのモンスター管理から登録を解除する。
    /// </summary>
    protected override void Death()
    {
        if (!Object.HasStateAuthority) return;
        _inGame.UnregisterMonster(this);
        base.Death();

    }
    /// <summary>
    /// Death状態の更新処理。
    /// 死亡処理、撃破数更新、死亡アニメーションの適用を行う。
    /// </summary>
    private void UpdateDeath()
    {
        if (!Object.HasStateAuthority) return;
        // 死亡処理の重複実行を防止する。
        if (isStartDeathProcessing) return;
        isStartDeathProcessing = true;
        Death();
        _gameManager.gamePlayer.AddKillMonsterStatus(actorStatus.status.Lv);


        StopTracking();
        ApplyCurrentAnimation();
        SetNetworkAction(MonsterAction.None);



    }
    /// <summary>
    /// 死亡アニメーション終了後に呼び出され、
    /// モンスターのNetworkObjectをDespawnする。
    /// </summary>
    public void OnDeathAnimationFinished()
    {
        if (Object == null || !Object.HasStateAuthority) return;
        Runner.Despawn(Object);
    }
    /// <summary>
    /// NetworkObjectがDespawnされた際に、
    /// InGameのモンスター管理から登録を解除する。
    /// </summary>
    public override void Despawned(NetworkRunner runner, bool hasState)
        => InGame.Instance?.UnregisterMonster(this);

    #endregion

    #endregion

    #region Animation
    /// <summary>
    /// アニメーション変更を同期するためのシーケンス番号。
    /// </summary>
    [Networked] private int animationSequence { get; set; }
    /// <summary>
    /// 最後にRender側で適用したアニメーションのシーケンス番号。
    /// </summary>
    private int lastRenderedAnimationSequence = -1;
    /// <summary>
    /// NetworkedのanimationSequenceが変更された場合に、
    /// 現在のアニメーション状態をRender側へ反映する。
    /// </summary>
    public override void Render()
    {
        if (lastRenderedAnimationSequence == animationSequence) return;

        lastRenderedAnimationSequence = animationSequence;
        ApplyCurrentAnimation();
    }
    /// <summary>
    /// 追跡中かどうかを表すAnimatorパラメータ名。
    /// </summary>
    private const string trackingBool = "Tracking";
    /// <summary>
    /// ネットワーク上の現在行動を変更し、
    /// アニメーション更新を通知する。
    /// </summary>
    /// <param name="newAction">変更後の行動。</param>
    protected void SetNetworkAction(MonsterAction newAction)
    {
        if (!Object.HasStateAuthority) return;

        currentAction = newAction;
        // 同じActionでもアニメーションを再実行できるよう、
        // シーケンス番号を増加させる。
        animationSequence++;

        ApplyCurrentAnimation();
        lastRenderedAnimationSequence = animationSequence;
    }
    /// <summary>
    /// 現在の行動と異なる場合のみ、
    /// ネットワーク上の行動を変更する。
    /// </summary>
    private void SetNetworkActionIfChanged(MonsterAction newAction)
    {
        if (currentAction == newAction) return;
        SetNetworkAction(newAction);
    }

    /// <summary>
    /// 被ダメージアニメーションを再生するために、
    /// アニメーションシーケンスを更新する。
    /// </summary>
    protected virtual void SetGetHitAnimation()
    {
        currentAction = MonsterAction.None;
        animationSequence++;

        ApplyCurrentAnimation();
        lastRenderedAnimationSequence = animationSequence;
    }

    /// <summary>
    /// 現在のStageとActionに応じたアニメーションを適用する。
    /// </summary>
    private void ApplyCurrentAnimation()
    {
        if (actorAnimator == null) return;

        // 被ダメージ状態の場合は移動と攻撃を解除し、
        // Hitアニメーションを再生する。
        if (stage == MonsterStage.GetHit)
        {
            actorAnimator.SetBool(runBool, false);
            ReSetAllTheAttackTrigger();
            actorAnimator.SetTrigger(gotHitTriggerHash);
            return;
        }
        // 死亡状態の場合は移動と攻撃を解除し、
        // Deathアニメーションを再生する。
        else if (stage == MonsterStage.Death)
        {
            actorAnimator.SetBool(runBool, false);
            ReSetAllTheAttackTrigger();
            actorAnimator.SetTrigger(deathTriggerHash);

        }
        PlayAnimation(currentAction);
    }
    /// <summary>
    /// 現在のMonsterActionに対応するアニメーションを再生する。
    /// </summary>
    /// <param name="monsterAction">再生対象の行動。</param>
    private void PlayAnimation(MonsterAction monsterAction)
    {
        if (actorAnimator == null) return;

        switch (monsterAction)
        {
            case MonsterAction.None:
                actorAnimator.SetBool(trackingBool, stage == MonsterStage.Tracking);
                actorAnimator.SetBool(runBool, false);
                actorAnimator.SetTrigger(cancelTriggerHash);
                break;
            case MonsterAction.Run:
                actorAnimator.SetBool(trackingBool, stage == MonsterStage.Tracking);
                actorAnimator.SetBool(runBool, true);
                break;
            case MonsterAction.Thinking:
                actorAnimator.SetBool(runBool, false);
                actorAnimator.SetTrigger(cancelTriggerHash);
                break;
            case MonsterAction.Attack:
                actorAnimator.SetBool(runBool, false);
                AnimationAttack();
                break;
            case MonsterAction.Skill:
                actorAnimator.SetBool(runBool, false);
                AnimationSkill();
                break;
        }
    }
    /// <summary>
    /// 通常攻撃とアクティブスキルのTriggerをリセットする。
    /// </summary>
    protected override void ReSetAllTheAttackTrigger()
    {
        if (actorAnimator == null) return;

        base.ReSetAllTheAttackTrigger();
        actorAnimator.ResetTrigger(activeSkillTriggerHash);
    }
    /// <summary>
    /// 攻撃重みに基づいて通常攻撃を選択し、
    /// 対応する攻撃アニメーションを再生する。
    /// </summary>
    protected virtual void AnimationAttack()
    {
        if (actorAnimator == null) return;

        ReSetAllTheAttackTrigger();
        if (!TryGetNormalAttackIndex(out int currentIndex))
        {
            Debug.LogError("Cant Get The Normal Attack Index");
            return;
        }
        actorAnimator.SetTrigger(allNormalAttackHashes[currentIndex]);
    }
    /// <summary>
    /// アクティブスキルのアニメーションを再生する。
    /// </summary>
    protected virtual void AnimationSkill()
    {
        if (actorAnimator == null) return;

        ReSetAllTheAttackTrigger();
        actorAnimator.SetTrigger(activeSkillTriggerHash);
    }

    /// <summary>
    /// アニメーションイベントなどから呼び出され、
    /// 次の行動を実行可能な状態へ変更する。
    /// </summary>
    public override void CanDoNextCommand()
    {
        if (Object == null || !Object.HasStateAuthority) return;

        canDoNextCommand = true;
    }

    #endregion
    /// <summary>
    /// モンスターのレベル・ステータス・NavMesh・
    /// 攻撃設定の初期化が完了しているかを表す。
    /// </summary>
    private bool levelInitDone = false;
    /// <summary>
    /// モンスターのレベル、ステータス、NavMesh、
    /// 攻撃重みなどの初期設定を行う。
    /// </summary>

    public override void ActorInit()
    {
        // 全プレイヤーのCharacter生成が完了するまで待機する。
        if (Runner.ActivePlayers.Count() < _inGame.allPlayerCharacters.Count) return;
        SetMonsterLevel();
        actorStatus.StatusInit(MonsterStatus());
        AllStatusInit();
        // 現在位置付近のNavMeshへ配置する。
        if (!TryGetNavMesh())
        {
            Debug.LogError($"No NavMesh near: {transform.position}");
            return;
        }
        // 通常攻撃のHash数と重み設定を確認する。
        if (!IsAllAttackWeightsSetUp())
        {
            Debug.LogError("Attack hashes and weights count do not match.\r\n");
            return;
        }
        canBeTrack = true;
        levelInitDone = true;
    }

    /// <summary>
    /// State Authority側でモンスターの現在Stageに応じた
    /// AI処理を毎Network Tick更新する。
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || !_inGame.IsGamePlaying()) return;
        // 初期化が完了していない場合は、
        // AI処理より先に初期化を行う。
        if (!levelInitDone)
        {
            ActorInit();
            return;
        }

        switch (stage)
        {
            case MonsterStage.Idle: UpdateIdle();break;
            case MonsterStage.Patrol:UpdatePatrol();break;
            case MonsterStage.Tracking:UpdateTracking();break;
            case MonsterStage.GetHit:UpdateGetHit();break;
            case MonsterStage.Death: UpdateDeath(); break;
        }


    }
}