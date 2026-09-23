using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

public class Adventurer : Character
{
    protected override CharacterType characterType => CharacterType.Adventurer;

    public override int initHp => 1000;
    public override int initDef => 100;
    public override int initAtk => 500;
    protected override float minDefScale => 0.3f;
    protected override float trackDistance => 5.0f;

    public override List<LevelScalePair> allLevelScalePairs =>
        new List<LevelScalePair>()
        {
            { new( 20,1.1f) },
            { new( 50,1.7f) },
            { new( 75,2.0f) },
            { new( 99,2.3f) },

        };

    protected override float moveSpeed => 4.0f;

    private const string isDefend = "isDefending";

    public override uint activeSkillCost => 30;

    public override void ReturnIdle()
    {
        base.ReturnIdle();
        actorAnimator.SetBool(isDefend, false);
        leftHand.EndDefend();
    }

    [Header("Adventurer Special")]
    public Weapon leftHand;

    public override void PassiveSkill()
    {
        if(stage == PlayerStage.PassiveSkill)
        {
            WaitTheNextAction();
            return;
        }
        base.PassiveSkill();
        actorAnimator.SetBool(isDefend, true);
        leftHand.DoDefend();

    }


}