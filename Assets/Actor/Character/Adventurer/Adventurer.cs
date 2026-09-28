using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

public class Adventurer : Character
{
    public override CharacterType characterType => CharacterType.Adventurer;
    protected override float trackDistance => 5.0f;

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
    public Transform leftHandTransform;

    public override void PassiveSkill()
    {
        if (stage == PlayerStage.Death) return;
        if (stage == PlayerStage.PassiveSkill) 
        {
            WaitTheNextAction();
            return;
        }
        base.PassiveSkill();
        actorAnimator.SetBool(isDefend, true);
        leftHand.DoDefend();

    }


}