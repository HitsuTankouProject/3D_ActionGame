using UnityEngine;

public class testingAction : MonoBehaviour
{

    public Animator animator;
    private enum ActionState
    {
        NonBattle,
        Battling,
        Dead
    }
    [SerializeField] private ActionState currentState = ActionState.NonBattle;
    private enum Action
    {
        Idle,
        NormalAttack,
        PassiveSkill,
        Ult
    }
    [SerializeField] private Action currentAction = Action.Idle;

    private readonly int changeStateTriggerHash = Animator.StringToHash("ChangeStateTrigger");
    private readonly string isBattleHash = "IsBattle";
    private readonly string isRunHash = "IsRun";
    private readonly int gotHitAndFlyTriggerHash = Animator.StringToHash("GotHitAndFlyTrigger");
    private readonly int gotHitTriggerHash = Animator.StringToHash("GotHitTrigger");
    private readonly int deadTriggerHash = Animator.StringToHash("DeadTrigger");
    private readonly int passiveSkillTriggerHash = Animator.StringToHash("PassiveSkillTrigger");
    private readonly string isBlockingHash = "IsBlocking";
    private readonly int normalAttackTriggerHash = Animator.StringToHash("NormalAttackTrigger");
    private readonly string normalAttackIndexHash = "NormalAttackIndex";
    private readonly int ultHash = Animator.StringToHash("Ult");

    private void ResetAllHash()
    {
        animator.ResetTrigger(gotHitAndFlyTriggerHash);
        animator.ResetTrigger(gotHitTriggerHash);
        animator.ResetTrigger(deadTriggerHash);
        animator.ResetTrigger(passiveSkillTriggerHash);
        animator.ResetTrigger(normalAttackTriggerHash);
        animator.ResetTrigger(ultHash);

        animator.SetBool(isRunHash, false);
        animator.SetBool(isBlockingHash, false);
    }


    public bool changeState = false;
    public GameObject takeWeapon;
    public GameObject wearWeapon;

    private void ChangeState()
    {
        if (currentState == ActionState.Dead) return;
        ResetAllHash();
        bool changeStateToBattle = currentState == ActionState.NonBattle;
        currentState = changeStateToBattle? ActionState.Battling : ActionState.NonBattle;
        animator.SetBool(isBattleHash, changeStateToBattle);
        animator.SetTrigger(changeStateTriggerHash);

        takeWeapon.SetActive(changeStateToBattle);
        wearWeapon.SetActive(!changeStateToBattle);
    }



    public bool normalAttack = false;
    private int normalAttackIndex = 0;
    private void NormalAttack()
    {
        if (currentState != ActionState.Battling) return;
        ResetAllHash();
        currentAction = Action.NormalAttack;
        animator.SetInteger(normalAttackIndexHash, normalAttackIndex);
        animator.SetTrigger(normalAttackTriggerHash);
        normalAttackIndex = (normalAttackIndex + 1) % 3;

    }


    public bool passiveSkill = false;
    private bool isBlocking = false;

    public void PassiveSkill()
    {
        if (currentState != ActionState.Battling) return;
        currentAction = Action.PassiveSkill;
        ResetAllHash();
        isBlocking = !isBlocking;
        if (isBlocking) animator.SetTrigger(passiveSkillTriggerHash);
        animator.SetBool(isBlockingHash, isBlocking);

    }


    public bool ult = false;
    public void Ult()
    {
        if (currentState != ActionState.Battling) return;
        ResetAllHash();
        currentAction = Action.Ult;

        animator.SetTrigger(ultHash);
    }



    private void ChangeToBattle()
    {
        currentState = ActionState.Battling;


        takeWeapon.SetActive(true);
        wearWeapon.SetActive(false);
    }


    public bool gotHit = false;
    public void GotHit()
    {
        if (currentState == ActionState.Dead) return;
        ResetAllHash();
        animator.SetTrigger(gotHitTriggerHash);
        animator.SetBool(isBattleHash, true);
        animator.SetTrigger(changeStateTriggerHash);
        ChangeToBattle();

    }

    public bool gotHitAndFly = false;
    public void GotHitAndFly()
    {
        if (currentState == ActionState.Dead) return;
        ResetAllHash();
        animator.SetTrigger(gotHitAndFlyTriggerHash);
        animator.SetBool(isBattleHash, true);
        animator.SetTrigger(changeStateTriggerHash);
        ChangeToBattle();

    }


    private void Start()
    {
        ChangeToBattle();
    }


    private void Update()
    {
        if (currentState == ActionState.Dead) return;

        if (gotHitAndFly)
        {
            gotHitAndFly = false;
            GotHitAndFly();
            return;
        }

        if (gotHit)
        {
            gotHit = false;
            GotHit();
            return;
        }

        if (changeState)
        {
            changeState = false;
            ChangeState();

            return;
        }

        if (ult)
        {
            ult = false;
            Ult();
            return;
        }

        if (passiveSkill)
        {
            passiveSkill = false;
            PassiveSkill();
            return;
        }

        if (normalAttack)
        {
            normalAttack = false;
            NormalAttack();
            return;
        }
    }




}
