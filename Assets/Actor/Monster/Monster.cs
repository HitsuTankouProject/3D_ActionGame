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

public enum MonsterType
{
    Dullahan, Golem, Wolf
}

public enum MonsterStage { Idle, Patrol, Tracking, GetHit, Death }

public enum MonsterAction { None, Run, Thinking, Attack, Skill }
[RequireComponent(typeof(PatrolPath))]
[RequireComponent(typeof(NavMeshAgent))]
public abstract class Monster : Actor
{

    [Header("Monster Basic")]
    public abstract MonsterType monsterType { get; }
    protected override float trackDistance { get; }

    [Header("Monster Status")]
    public int monsterLevel { get; private set; } = 10;
    public void SetMonsterLevel()
    {
        int totalLevel = 0;
        foreach(Character character in _inGame.allPlayerCharacters)
        {
            totalLevel += character.actorStatus.status.Lv;
        }
        monsterLevel = Mathf.RoundToInt(totalLevel/ _inGame.allPlayerCharacters.Count);

        monsterLevel = Mathf.Clamp(monsterLevel, 10, 99);
    }

    private List<int> CreateRandomNumbers(int targetNumber, int numberCount)
    {
        List<int> randomNumbers = new();

        if (targetNumber <= 0 || numberCount <= 0)
        {
            Debug.LogError("Target number or Number count must be more than 0.");
            return randomNumbers;
        }

        if (targetNumber < numberCount)
        {
            Debug.LogError($"Target number must be at least {numberCount}.");
            return randomNumbers;
        }

        int remainingNumber = targetNumber;

        for (int i = 0; i < numberCount - 1; i++)
        {
            int remainingSlots = numberCount - i - 1;

            int maximumNumber = remainingNumber - remainingSlots;

            int randomNumber = Random.Range(1, maximumNumber + 1);

            randomNumbers.Add(randomNumber);

            remainingNumber -= randomNumber;
        }

        randomNumbers.Add(remainingNumber);

        for (int i = randomNumbers.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            (randomNumbers[i], randomNumbers[randomIndex]) = (randomNumbers[randomIndex], randomNumbers[i]);
        }

        return randomNumbers;
    }

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
    [SerializeField] private MonsterActionDuration monsterActionDuration;

    [Networked] public MonsterStage stage { get; protected set; }
    [Networked] public MonsterAction currentAction { get; protected set; }

    [Networked] private TickTimer stateTimer { get; set; }

    private bool IsStateTimerRunning() => stateTimer.IsRunning && !stateTimer.Expired(Runner);
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

    protected override void Turn(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0.0f;

        if (direction.sqrMagnitude <= 0.0f)
            return;

        transform.rotation = Quaternion.LookRotation(direction);
    }
    public override void Move(Vector3 targetPosition)
    {
        if (!Object.HasStateAuthority || !navMeshAgent.isOnNavMesh) return;
        navMeshAgent.speed = actorStatus.actorBasicStatus.actorMoveSpeed;
        Turn(targetPosition);
        navMeshAgent.SetDestination(targetPosition);
        SetNetworkActionIfChanged(MonsterAction.Run);
    }

    protected void MoveAndStopInDistance(Vector3 targetPosition, float stoppingDistance = 0.1f)
    {
        if (!Object.HasStateAuthority || !navMeshAgent.isOnNavMesh) return;
        navMeshAgent.stoppingDistance = stoppingDistance;
        Move(targetPosition);
    }



    #region Stage Process
    [Header(" Stage Process")]
    [SerializeField] protected PatrolPath patrolPath;
    [SerializeField] protected NavMeshAgent navMeshAgent;

    private bool TryGetNavMesh()
    {
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas)) return false;

        //Debug.Log($"Found NavMesh: {hit.position}");
        navMeshAgent.Warp(hit.position);
        return true;
    }
    private bool IsNavMeshMoving()
    {
        if (navMeshAgent == null || !navMeshAgent.isActiveAndEnabled || !navMeshAgent.isOnNavMesh) return false;

        return navMeshAgent.velocity.sqrMagnitude > 0.01f;
    }
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
    private void StopNavMeshMovement()
    {
        if (navMeshAgent == null || 
            !navMeshAgent.isActiveAndEnabled || !navMeshAgent.isOnNavMesh) return;
        navMeshAgent.ResetPath();
    }

    protected override bool TryTrackActor()
    {
        if (_inGame == null || _inGame.allPlayerCharacters == null) return false;

        if (trackingActor != null) return !IsTrackingActorOutOfTrackDistance(); 

        trackingActor = null;
        float closestDistance = float.MaxValue;

        foreach (Character character in _inGame.allPlayerCharacters)
        {
            if (character == null || character.Object == null) continue;
            if (!character.canBeTrack) continue;

            float distance = Vector3.Distance(transform.position, character.transform.position);

            if (distance > trackDistance || distance >= closestDistance) continue;

            closestDistance = distance;
            trackingActor = character;
        }

        return trackingActor != null;
    }

    #region Idle

    public virtual void ReturnIdle()
    {
        if (!Object.HasStateAuthority) return;

        StopNavMeshMovement();
        SetNetworkAction(MonsterAction.None);
        stateTimer = TickTimer.None;
        stage = MonsterStage.Idle;
    }

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

    private int patrolIndex = 0;
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
    private void GoToNextPatrolPoint()
    {
        patrolIndex = GetPatrolPointIndex();
        if (patrolIndex == -1) return;

        Vector3 targetPosition = patrolPath.GetPoint(patrolIndex);
        Move(targetPosition);
    }

    private void UpdatePatrol()
    {
        if (!Object.HasStateAuthority || IsStateTimerRunning()) return;

        if (TryTrackActor())
        {
            stage = MonsterStage.Tracking;
            return;
        }
        if (IsNavMeshMoving()) return;
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
    protected bool CanNormalAttack() => trackingActorDistance < monsterActionDuration.normalAttackDistance;

    protected bool CanSkillAttack() => trackingActorDistance < monsterActionDuration.skillAttackDistance;

    protected bool CanAttack() => CanNormalAttack() || CanSkillAttack();

    [Networked] private TickTimer skillTimer { get; set; }
    public float skillCooldownRemaining
    {
        get
        {
            if (IsSkillOnCooldown()) return skillTimer.RemainingTime(Runner) ?? 0.0f;
            else return 0.0f;
        }
    }
    private bool IsSkillOnCooldown()=> skillTimer.IsRunning && !skillTimer.Expired(Runner);
    private bool TryStartSkillCooldown()
    {
        if(IsSkillOnCooldown()) return false;
        skillTimer = TickTimer.CreateFromSeconds(Runner, monsterActionDuration.skillCooldownSeconds);

        return skillTimer.IsRunning;

    }

    protected virtual bool TryUseSkill()
    {
        if(!TryStartSkillCooldown())return false;
        SetNetworkAction(MonsterAction.Skill);
        return true;
    }

    private uint allNormalAttackWeights = default;
    private bool IsAllAttackWeightsSetUp()
    {
        if (monsterActionDuration.normalAttackWeights.Length != allNormalAttackHashes.Length || monsterActionDuration.normalAttackWeights.Length <= 0) return false;
        uint totalWeight = 0;
        foreach (uint weight in monsterActionDuration.normalAttackWeights)
        {
            if (weight == 0)
            {
                Debug.LogError("Normal attack weight cannot be 0.");
                return false;
            }
        }
        allNormalAttackWeights = totalWeight;
        return true;

    }

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
    protected virtual void NormalAttack()
    {
        SetNetworkAction(MonsterAction.Attack);
    }

    #endregion

    #region Tracking

    private Vector3 theClosePatrolSpot = Vector3.positiveInfinity;
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
    private bool IsTheClosePatrolSpotOutOfTrackDistance()
    {
        if (theClosePatrolSpot != Vector3.positiveInfinity 
            && Vector3.Distance(transform.position, theClosePatrolSpot) < trackDistance)
            return false;

        theClosePatrolSpot = FindTheClosePatrolSpot();

        return Vector3.Distance(transform.position, theClosePatrolSpot) > trackDistance;
    }

    private bool isLostTracking = false;
    private void LostTracking()
    {
        if(!isLostTracking)
        {
            if(!TrySetStateTimer(monsterActionDuration.lostTrackingWaitSeconds)) return;
            isLostTracking = true;
            SetNetworkAction(MonsterAction.Run);
            Move(theClosePatrolSpot);
            return;
        }
        
        isLostTracking = false;
        StopNavMeshMovement();
        SetNetworkAction(MonsterAction.Thinking);
        theClosePatrolSpot = Vector3.positiveInfinity;
        ReturnIdle();
    }

    protected abstract void AttackAction();

    protected virtual void Tracking()
    {
        isLostTracking = false;
        if (!CanAttack())
        {
            MoveAndStopInDistance(trackingActor.transform.position, monsterActionDuration.ClosestAttackDistance());
            return;
        }
        if (!canDoNextCommand || !TrySetStateTimer(monsterActionDuration.actionThinkingSeconds)) return;
        StopNavMeshMovement();
        canDoNextCommand = false;
        AttackAction();
    }


    protected override void UpdateTracking()
    {
        if (!Object.HasStateAuthority || IsStateTimerRunning()) return;

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

    protected override int GotHitHpLost(int damage)
    {
        int result =  base.GotHitHpLost(damage);
        Debug.Log($" {nowDefScale}, got hit: {damage}, nowHp: {nowHp}");
        return result;
    }

    protected override void GotHit(int damage)
    {
        base.GotHit(damage);

        if (nowHp > 0) stage = MonsterStage.GetHit;
        else stage = MonsterStage.Death;
    }

    private void UpdateGetHit()
    {
        if (!canDoNextCommand) return;
        SetGetHitAnimation();
        canDoNextCommand = false;
        ReturnIdle();
    }

    #endregion

    #region Death
    private bool isStartDeathProcessing = false;

    private void UpdateDeath()
    {
        if (!Object.HasStateAuthority) return;

        if (isStartDeathProcessing) return;
        isStartDeathProcessing = true;
        Death();
        _gameManager.gamePlayer.AddKillMonsterStatus(actorStatus.status.Lv);


        StopTracking();
        ApplyCurrentAnimation();
        SetNetworkAction(MonsterAction.None);



    }

    public void OnDeathAnimationFinished()
    {
        if (Object == null || !Object.HasStateAuthority) return;
        Runner.Despawn(Object);
    }
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        InGame.Instance?.UnregisterMonster(this);
    }


    #endregion

    #endregion

    #region Animation
    [Networked] private int animationSequence { get; set; }
    private int lastRenderedAnimationSequence = -1;

    public override void Render()
    {
        if (lastRenderedAnimationSequence == animationSequence) return;

        lastRenderedAnimationSequence = animationSequence;
        ApplyCurrentAnimation();
    }
    private const string trackingBool = "Tracking";

    protected void SetNetworkAction(MonsterAction newAction)
    {
        if (!Object.HasStateAuthority) return;

        currentAction = newAction;
        animationSequence++;

        ApplyCurrentAnimation();
        lastRenderedAnimationSequence = animationSequence;
    }

    private void SetNetworkActionIfChanged(MonsterAction newAction)
    {
        if (currentAction == newAction) return;
        SetNetworkAction(newAction);
    }


    protected virtual void SetGetHitAnimation()
    {
        currentAction = MonsterAction.None;
        animationSequence++;

        ApplyCurrentAnimation();
        lastRenderedAnimationSequence = animationSequence;
    }

    private void ApplyCurrentAnimation()
    {
        if (actorAnimator == null) return;

        if (stage == MonsterStage.GetHit)
        {
            actorAnimator.SetBool(runBool, false);
            ReSetAllTheAttackTrigger();
            actorAnimator.SetTrigger(gotHitTrigger);
            return;
        }
        else if (stage == MonsterStage.Death)
        {
            actorAnimator.SetBool(runBool, false);
            ReSetAllTheAttackTrigger();
            actorAnimator.SetTrigger(deathTrigger);

        }
        PlayAnimation(currentAction);
    }

    private void PlayAnimation(MonsterAction monsterAction)
    {
        if (actorAnimator == null) return;

        switch (monsterAction)
        {
            case MonsterAction.None:
                actorAnimator.SetBool(trackingBool, stage == MonsterStage.Tracking);
                actorAnimator.SetBool(runBool, false);
                actorAnimator.SetTrigger(cancelTrigger);
                break;
            case MonsterAction.Run:
                actorAnimator.SetBool(trackingBool, stage == MonsterStage.Tracking);
                actorAnimator.SetBool(runBool, true);
                break;
            case MonsterAction.Thinking:
                actorAnimator.SetBool(runBool, false);
                actorAnimator.SetTrigger(cancelTrigger);
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

    protected override void ReSetAllTheAttackTrigger()
    {
        if (actorAnimator == null) return;

        base.ReSetAllTheAttackTrigger();
        actorAnimator.ResetTrigger(activeSkillTrigger);
    }

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

    protected virtual void AnimationSkill()
    {
        if (actorAnimator == null) return;

        ReSetAllTheAttackTrigger();
        actorAnimator.SetTrigger(activeSkillTrigger);
    }


    public override void CanDoNextCommand()
    {
        if (Object == null || !Object.HasStateAuthority) return;

        canDoNextCommand = true;
    }

    #endregion

    private bool levelInitDone = false;

    public override void ActorInit()
    {
        if (Runner.ActivePlayers.Count() < _inGame.allPlayerCharacters.Count) return;
        SetMonsterLevel();
        actorStatus.StatusInit(MonsterStatus());
        AllStatusInit();

        if(!TryGetNavMesh())
        {
            Debug.LogError($"No NavMesh near: {transform.position}");
            return;
        }
        if (!IsAllAttackWeightsSetUp())
        {
            Debug.LogError("Attack hashes and weights count do not match.\r\n");
            return;
        }
        canBeTrack = true;
        levelInitDone = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || !_inGame.IsGamePlaying()) return;
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