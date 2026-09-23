using Cysharp.Threading.Tasks;
using Fusion;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public enum MonsterType
{
    Dullahan
}

public enum MonsterStage { Idle, Patrol, Tracking, GetHit, Death }

public enum MonsterAction { None, Run, Thinking, Attack, Skill }
public abstract class Monster : Actor
{

    [Header("Components")]
    [SerializeField] protected Animator monsterAnimator;
    public abstract MonsterType monsterType { get; }
    protected override float trackDistance { get; }
    protected abstract float attackDistance { get; }
    protected override float moveSpeed { get; }

    [Header("Actor Status")]
    public int monsterLevel { get; private set; } = 10;
    public void SetMonsterLevel(int level)
    {
        monsterLevel = Mathf.Clamp(level, 10, 99);
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
    public override void ActorInit()
    {
        canBeTrack = true;
        actorStatus.StatusInit(MonsterStatus(), allLevelScalePairs);
    }

    [Header("Patrol")]
    [SerializeField] protected List<Vector3> patrolLocations = new();
    [SerializeField] protected Vector3 targetPatrolLocation = Vector3.zero;
    [SerializeField] private const float patrolWaitSeconds = 1.5f;

    [Header("Action Decision")]
    [SerializeField] private float idleDecisionSeconds = 0.1f;
    [SerializeField] private float actionThinkingSeconds = 0.5f;

    [Header("Tracking")]
    [SerializeField] protected Vector3 closestPatrolLocation = Vector3.zero;


    [Networked] public MonsterStage stage { get; protected set; }
    [Networked] private MonsterAction currentAction { get; set; }
    [Networked] private PlayerRef trackingPlayer { get; set; }



    [Networked] private TickTimer stateTimer { get; set; }
    [Networked] private int animationSequence { get; set; }
    [Networked] private int selectedAttackAnimation { get; set; }
    [Networked] private NetworkBool useFirstAttack { get; set; }


    public bool isTrackingCharacter => trackingPlayer != PlayerRef.None;
    private int lastRenderedAnimationSequence = -1;
    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            trackingPlayer = PlayerRef.None;
            useFirstAttack = true;
            stage = MonsterStage.Idle;
            EnterStage(MonsterStage.Idle);

            Debug.Log($"Monster Spawned: {monsterType}", this);
        }

        List<Vector3> newPatrolLocations = new List<Vector3>(){
            { transform.position + Vector3.back * 5},
            { transform.position + Vector3.forward * 5 },
            { transform.position + Vector3.left * 5 },
            { transform.position + Vector3.right * 5 } };

        SetPatrolLocation(newPatrolLocations);
        ApplyCurrentAnimation();
        lastRenderedAnimationSequence = animationSequence;
        InGame.Instance?.RegisterMonster(this);
        ActorInit();
        AllStatusInit();
    }

    public override void Render()
    {
        if (lastRenderedAnimationSequence == animationSequence) return;

        lastRenderedAnimationSequence = animationSequence;
        ApplyCurrentAnimation();
    }

    #region Stage Process

    public void ChangeStage(MonsterStage newStage)
    {
        if (!Object.HasStateAuthority || stage == newStage) return;

        MonsterStage oldStage = stage;

        ExitStage(oldStage);
        stage = newStage;
        EnterStage(newStage);

        //Debug.Log($"{monsterType}: {oldStage} -> {newStage}", this);
    }
    private void EnterStage(MonsterStage newStage)
    {
        canDoNextCommand = false;
        stateTimer = TickTimer.None;

        switch (newStage)
        {
            case MonsterStage.Idle:
                StopTracking();
                stateTimer = TickTimer.CreateFromSeconds(Runner, idleDecisionSeconds);
                SetNetworkAction(MonsterAction.None);
                break;
            case MonsterStage.Patrol:
                StopTracking();
                targetPatrolLocation = FindNextPatrolLocation();
                SetNetworkAction(MonsterAction.Run);
                break;
            case MonsterStage.Tracking:
                SetNetworkAction(MonsterAction.None);
                break;
            case MonsterStage.GetHit:
                SetGetHitAnimation();
                break;
            case MonsterStage.Death:
                StopTracking();
                ApplyCurrentAnimation();
                SetNetworkAction(MonsterAction.None);
                break;
        }
    }

    private void ExitStage(MonsterStage oldStage)
    {
        stateTimer = TickTimer.None;
        canDoNextCommand = false;
    }

    public virtual void ReturnIdle()
    {
        if (!Object.HasStateAuthority) return;

        if (stage == MonsterStage.Idle)
        {
            EnterStage(MonsterStage.Idle);
            return;
        }

        ChangeStage(MonsterStage.Idle);
    }


    #endregion

    #region Idle

    private void UpdateIdle()
    {
        if (stateTimer.IsRunning && !stateTimer.Expired(Runner)) return;

        stateTimer = TickTimer.None;

        if (TryTrackActor())
        {
            StartTracking();
            ChangeStage(MonsterStage.Tracking);
            return;
        }


        ChangeStage(MonsterStage.Patrol);
    }

    #endregion

    #region Patrol

    private void UpdatePatrol()
    {
        if (TryTrackActor())
        {
            StartTracking();
            ChangeStage(MonsterStage.Tracking);
            return;
        }

        if (currentAction == MonsterAction.Thinking)
        {
            if (!stateTimer.Expired(Runner)) return;

            stateTimer = TickTimer.None;
            targetPatrolLocation = FindNextPatrolLocation();
            SetNetworkAction(MonsterAction.Run);
            return;
        }
        Vector3 directionToTarget = targetPatrolLocation - transform.position;
        directionToTarget.y = 0.0f;
        float remainingDistance = directionToTarget.magnitude;
        if (remainingDistance <= 0.1f)
        {
            //transform.position = targetPatrolLocation;
            SetNetworkAction(MonsterAction.Thinking);

            stateTimer = TickTimer.CreateFromSeconds(Runner, patrolWaitSeconds);
            return;
        }

        Move(targetPatrolLocation);
    }

    public void SetPatrolLocation(List<Vector3> newPatrolLocation)
    {
        if (newPatrolLocation == null)
        {
            Debug.LogError(
                "Patrol Location is null.",
                this);

            return;
        }
        patrolLocations.Add(this.transform.position);
        patrolLocations.AddRange(newPatrolLocation);
    }
    private Vector3 FindNextPatrolLocation()
    {
        if (patrolLocations == null || patrolLocations.Count == 0)
            return transform.position;

        if (!patrolLocations.Contains(targetPatrolLocation))
            return patrolLocations[Random.Range(0, patrolLocations.Count)];

        int currentIndex = patrolLocations.IndexOf(targetPatrolLocation);
        int nextIndex = Random.Range(0, patrolLocations.Count);

        if (nextIndex == currentIndex)
            nextIndex = (nextIndex + 1) % patrolLocations.Count;

        return patrolLocations[nextIndex];
    }

    #endregion

    #region Tracking
    protected override void UpdateTracking(float distanceError = 1.0f)
    {
        if (!TryGetTrackingCharacter(out Character character))
        {
            ReturnIdle();
            return;
        }

        if (!TryFindClosestPatrolLocation() || IsOutOfTrackingRange(character))
        {
            ReturnIdle();
            return;
        }

        if (currentAction == MonsterAction.Attack ||
            currentAction == MonsterAction.Skill)
        {
            UpdateAttackOrSkill();
            return;
        }

        float distance = Vector3.Distance( transform.position, character.transform.position);

        if (distance > attackDistance * 1.1f)
        {
            stateTimer = TickTimer.None;
            MoveToTrackingCharacter(character);
            return;
        }

        Turn(character.transform.position, true);

        if (currentAction != MonsterAction.Thinking)
        {
            StartThinking();
            return;
        }

        UpdateThinking();
    }

    private void StartThinking()
    {
        stateTimer = TickTimer.CreateFromSeconds(Runner, actionThinkingSeconds);
        SetNetworkAction(MonsterAction.Thinking);
    }

    private void UpdateThinking()
    {
        if (!stateTimer.Expired(Runner)) return;

        stateTimer = TickTimer.None;

        MonsterAction selectedAction = Random.Range(0, 2) == 0
            ? MonsterAction.Attack
            : MonsterAction.Skill;

        canDoNextCommand = false;
        SetNetworkAction(selectedAction);
    }

    private void UpdateAttackOrSkill()
    {
        if (!canDoNextCommand) return;

        canDoNextCommand = false;
        SetNetworkAction(MonsterAction.None);
    }

    private void MoveToTrackingCharacter(Character character)
    {
        Vector3 targetPosition = character.transform.position;
        Vector3 directionFromTarget = transform.position - targetPosition;
        directionFromTarget.y = 0.0f;

        if (directionFromTarget.sqrMagnitude > 0.0001f)
        {
            directionFromTarget.Normalize();
            targetPosition += directionFromTarget * attackDistance;
        }

        Move(targetPosition);
    }

    public virtual bool IsStartOfTracking()
    {
        if (!Object.HasStateAuthority) return false;
        if (!TryTrackActor()) return false;

        StartTracking();
        return true;
    }

    protected override bool TryTrackActor()
    {
        trackingActor = null;

        if (_inGame == null || _inGame.allPlayerCharacters == null || !TryFindClosestPatrolLocation()) return false;

        float closestDistance = float.MaxValue;

        foreach (Character character in _inGame.allPlayerCharacters)
        {
            if (character == null || character.Object == null) continue;
            if (!character.canBeTrack) continue;



            float distanceFromPatrolArea = Vector3.Distance( closestPatrolLocation, character.transform.position );

            if (distanceFromPatrolArea > trackDistance || distanceFromPatrolArea >= closestDistance) continue;

            closestDistance = distanceFromPatrolArea;
            trackingActor = character;
        }

        return trackingActor != null;
    }
    private void StartTracking()
    {
        if (trackingActor == null || trackingActor.Object == null) return;

        trackingPlayer = trackingActor.Object.InputAuthority;
    }

    private bool TryGetTrackingCharacter(out Character trackingCharacter)
    {
        trackingCharacter = null;

        if (trackingPlayer == PlayerRef.None ||
            _inGame == null ||
           _inGame.allPlayerCharacters == null)
        {
            return false;
        }

        foreach (Character character in _inGame.allPlayerCharacters)
        {
            if (character == null || character.Object == null) continue;
            if (character.Object.InputAuthority != trackingPlayer) continue;
            if (!character.canBeTrack) continue;

            trackingCharacter = character;
            return true;
        }

        return false;
    }

    private bool TryFindClosestPatrolLocation()
    {
        if (patrolLocations == null || patrolLocations.Count == 0) return false;

        Vector3 result = patrolLocations[0];
        float closestDistanceSqr = (result - transform.position).sqrMagnitude;

        for (int index = 1; index < patrolLocations.Count; index++)
        {
            Vector3 patrolLocation = patrolLocations[index];
            float currentDistanceSqr =
                (patrolLocation - transform.position).sqrMagnitude;

            if (currentDistanceSqr >= closestDistanceSqr) continue;

            closestDistanceSqr = currentDistanceSqr;
            result = patrolLocation;
        }

        closestPatrolLocation = result;
        return true;
    }

    private bool IsOutOfTrackingRange(Character character)
    {
        if (character == null) return true;

        float distance = Vector3.Distance(
            closestPatrolLocation,
            character.transform.position);

        return distance > trackDistance * 1.1f;
    }

    protected override void StopTracking()
    {
        trackingPlayer = PlayerRef.None;
        base.StopTracking();

    }

    #endregion

    #region Movement
    public override void Move(Vector3 targetPosition)
    {
        if (!Object.HasStateAuthority) return;
        Vector3 moveDirection = targetPosition - transform.position;

        moveDirection.y = 0.0f;

        float remainingDistance = moveDirection.magnitude;

        if (remainingDistance <= 0.0001f) return;

        Turn(targetPosition, true);

        SetNetworkActionIfChanged(MonsterAction.Run);

        float movementDistance = Mathf.Min( moveSpeed * Runner.DeltaTime, remainingDistance);

        Vector3 movement = moveDirection.normalized * movementDistance;

        characterController.Move(movement);
    }

    #endregion

    #region Get Hit And Death

    private void UpdateGetHit()
    {
        if (!canDoNextCommand) return;

        canDoNextCommand = false;

        if (TryGetTrackingCharacter(out Character character) &&
            TryFindClosestPatrolLocation() &&
            !IsOutOfTrackingRange(character))
        {
            ChangeStage(MonsterStage.Tracking);
            return;
        }

        ReturnIdle();
    }
    protected override void GotHit(int damage)
    {
        base.GotHit(damage);
        if (nowHp > 0)
        {
            ChangeStage(MonsterStage.GetHit);
        }
    }

    protected override void Death()
    {
        if (!Object.HasStateAuthority) return;

        base.Death();

        ChangeStage(MonsterStage.Death);

    }

    public void OnDeathAnimationFinished()
    {
        if (Object == null ||!Object.HasStateAuthority) return;

        Runner.Despawn(Object);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        InGame.Instance?.UnregisterMonster(this);
    }

    #endregion

    #region Animation

    //private const string CancelTrigger = "Cancel";
    private const string trackingBool = "Tracking";
    //private const string RunBool = "Run";
    //private const string Attack01Trigger = "Attack_01";
    //private const string Attack02Trigger = "Attack_02";
    //private const string ActiveSkillTrigger = "ActiveSkill";
    //protected const string GotHitTrigger = "Hit";

    private void SetNetworkAction(MonsterAction newAction)
    {
        if (!Object.HasStateAuthority) return;

        if (newAction == MonsterAction.Attack)
        {
            selectedAttackAnimation = useFirstAttack ? 0 : 1;
            useFirstAttack = !useFirstAttack;
        }

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

    private void SetGetHitAnimation()
    {
        currentAction = MonsterAction.None;
        animationSequence++;

        ApplyCurrentAnimation();
        lastRenderedAnimationSequence = animationSequence;
    }

    private void ApplyCurrentAnimation()
    {
        if (monsterAnimator == null) return;

        if (stage == MonsterStage.GetHit)
        {
            monsterAnimator.SetBool(runBool, false);
            ResetActionTriggers();
            monsterAnimator.SetTrigger(gotHitTrigger);
            return;
        }
        else if (stage == MonsterStage.Death)
        {
            monsterAnimator.SetBool(runBool, false);
            ResetActionTriggers();
            monsterAnimator.SetTrigger(deathTrigger);

        }
        PlayAnimation(currentAction);
    }

    private void PlayAnimation(MonsterAction monsterAction)
    {
        if (monsterAnimator == null) return;

        switch (monsterAction)
        {
            case MonsterAction.None:
                monsterAnimator.SetBool(trackingBool, stage == MonsterStage.Tracking);
                monsterAnimator.SetBool(runBool, false);
                monsterAnimator.SetTrigger(cancelTrigger);
                break;
            case MonsterAction.Run:
                monsterAnimator.SetBool(trackingBool, stage == MonsterStage.Tracking);
                monsterAnimator.SetBool(runBool, true);
                break;
            case MonsterAction.Thinking:
                monsterAnimator.SetBool(runBool, false);
                monsterAnimator.SetTrigger(cancelTrigger);
                break;
            case MonsterAction.Attack:
                monsterAnimator.SetBool(runBool, false);
                AnimationAttack();
                break;
            case MonsterAction.Skill:
                monsterAnimator.SetBool(runBool, false);
                AnimationSkill();
                break;
        }
    }

    private void ResetActionTriggers()
    {
        if (monsterAnimator == null) return;

        monsterAnimator.ResetTrigger(attack01Trigger);
        monsterAnimator.ResetTrigger(attack02Trigger);
        monsterAnimator.ResetTrigger(activeSkillTrigger);
    }

    protected virtual void AnimationAttack()
    {
        if (monsterAnimator == null) return;

        ResetActionTriggers();

        string selectedTrigger = selectedAttackAnimation == 0 ? attack01Trigger : attack02Trigger;

        monsterAnimator.SetTrigger(selectedTrigger);
    }

    protected virtual void AnimationSkill()
    {
        if (monsterAnimator == null) return;

        ResetActionTriggers();
        monsterAnimator.SetTrigger(activeSkillTrigger);
    }

    /// <summary>
    /// Attack、Skill、Hit 動畫の Animation Event から呼び出す。
    /// </summary>
    public override void CanDoNextCommand()
    {
        if (Object == null || !Object.HasStateAuthority) return;

        canDoNextCommand = true;
    }

    #endregion




    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        switch (stage)
        {
            case MonsterStage.Idle:
                UpdateIdle();
                break;
            case MonsterStage.Patrol:
                UpdatePatrol();
                break;
            case MonsterStage.Tracking:
                UpdateTracking();
                break;
            case MonsterStage.GetHit:
                UpdateGetHit();
                break;
        }


    }
}