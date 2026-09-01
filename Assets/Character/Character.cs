using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System;
using System.Security.Cryptography;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

[System.Serializable] public enum PlayerDataType{ Character, Bag }
[System.Serializable] public enum CharacterType { Adventurer, Magician, Thief, Warrior }

[System.Serializable]
public struct CharacterStatus
{
    public int Lv;
    public int LevelPoint;
    public int HpLv;
    public int DefLv;
    public int AtkLv;
    public int PassiveLv;
    public int ActiveLv;
    public int UltLv;

    public CharacterStatus(int level, int levelPoint, int hpLv, int defLv, int atkLv, int passiveLv, int activeLv, int ultLv)
    {

        this.Lv = Mathf.Clamp(level, 1, 99);
        this.LevelPoint = Mathf.Clamp(levelPoint, 0, 99); ;
        this.HpLv = Mathf.Clamp(hpLv, 1, 99); ;
        this.DefLv = Mathf.Clamp(defLv, 1, 99); ;
        this.AtkLv = Mathf.Clamp(atkLv, 1, 99); ;
        this.PassiveLv = Mathf.Clamp(passiveLv, 1, 99); ;
        this.ActiveLv = Mathf.Clamp(activeLv, 1, 99); ;
        this.UltLv = Mathf.Clamp(ultLv, 1, 99); ;
    }

    public string AllStatus() =>
    $"Lv: {Lv}, LevelPoint: {LevelPoint}, HpLv: {HpLv}, DefLv: {DefLv}, " +
    $"AtkLv: {AtkLv}, PassiveLv: {PassiveLv}, ActiveLv: {ActiveLv}, UltLv: {UltLv}";
}

public enum PlayerStage { Idle, Run, Attack, PassiveSkill, ActiveSkill, UltSkill, Hit, Death }

public abstract class Character : MonoBehaviour 
{
    [Header("Character Status")]
    public CharacterStatus status;
    public SkinnedMeshRenderer characterMesh;
    public Animator characterAnimator;
    private AnimatorStateInfo stateInfo => characterAnimator.GetCurrentAnimatorStateInfo(0);


    [Header("Character Stage")]
    public PlayerStage stage;

    private const string attack01Trigger        = "Attack_01";
    private const string attack02Trigger        = "Attack_02";
    private const string runTrigger             = "Run";
    private const string passiveSkillTrigger    = "PassiveSkill";
    private const string activeSkillTrigger     = "ActiveSkill";
    private const string ultSkillTrigger        = "UltSkill";

    private bool useFirstAttack = true;
    protected void Animation_Attack()
    {
        string trigger = useFirstAttack ? attack01Trigger : attack02Trigger;
        characterAnimator.ResetTrigger(attack01Trigger);
        characterAnimator.ResetTrigger(attack02Trigger);

        characterAnimator.SetTrigger(trigger);

        useFirstAttack = !useFirstAttack;
    }

    private bool IsAllowCommand() => stage != PlayerStage.Hit && stage != PlayerStage.Death;
    private bool canDoNextCommand = true;
    public void CanDoNextCommand() => canDoNextCommand = true;
    public void CancelCommand()
    {
        characterAnimator.Play(stateInfo.fullPathHash, 0, 1f);
        characterAnimator.Update(0f);
    }
    private void PlayAnimation(PlayerStage playerStage, Vector3 faceTo)
    {
        switch (playerStage)
        {
            case PlayerStage.Run: characterAnimator.SetBool(runTrigger, true); break;
            case PlayerStage.Attack: Animation_Attack(); break;
            case PlayerStage.PassiveSkill: characterAnimator.SetBool(passiveSkillTrigger, true); break;
            case PlayerStage.ActiveSkill: characterAnimator.SetTrigger(activeSkillTrigger); break;
            case PlayerStage.UltSkill: characterAnimator.SetTrigger(ultSkillTrigger); break;
            default: break;
        }
    }

    public void RequestChangeStage(PlayerStage playerStage, Vector3 faceTo)
    {
        if (!IsAllowCommand()|| !canDoNextCommand) return;
        canDoNextCommand = false;

        Vector3 direction = faceTo - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(direction);

        stage = playerStage;
        PlayAnimation(playerStage, faceTo);
    }


















    public abstract int maxHp { get; }

    private struct StatusPair
    {
        public int level;
        public float scale;

        public StatusPair(int level, float scale)
        {
            this.level = level;
            this.scale = scale;
        }
    }

    private readonly StatusPair initStage = new StatusPair(1, 1.0f);
    private readonly StatusPair firstStage = new StatusPair(40, 1.7f);
    private readonly StatusPair secondStage = new StatusPair(80, 2.0f);
    private readonly StatusPair thirdStage = new StatusPair(99, 2.2f);

    public int FinallyStatus(int status_init, int status_lv)
    {
        StatusPair nowStage;
        StatusPair nextStage;

        if (status_lv< firstStage.level)
        {
            nowStage = initStage;
            nextStage = firstStage;
        }
        else if(status_lv< secondStage.level)
        {
            nowStage = firstStage;
            nextStage = secondStage;
        }
        else if(status_lv< thirdStage.level)
        {
            nowStage = secondStage;
            nextStage = thirdStage;
        }
        else return Mathf.RoundToInt(status_init * thirdStage.scale);

        int nowScale = status_lv- nowStage.level;
        float levelScale = (nextStage.scale - nowStage.scale) / (nextStage.level - nowStage.level);
        levelScale *= nowScale;
        int finalStatus = Mathf.RoundToInt(status_init * (nowStage.scale + levelScale));
        return finalStatus;
    }
    public abstract int GotHitHpLost(int damage);





}
