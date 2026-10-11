using UnityEngine;
using UnityEngine.TextCore.Text;


public interface ICharacterInPut 
{
    public Character character { get;}
}

public static class CharacterInPut
{
    public static bool TryNormalAttack(this ICharacterInPut iCharacterInPut)
    {
        if (!InputManager.IsNormalAttackKeyDown()) return false;
        iCharacterInPut.character.Attack();
        return true;
    }
    public static bool TryPassiveSkill(this ICharacterInPut iCharacterInPut)
    {
        if (!InputManager.IsPassiveSkillKeyDown()) return false;
        iCharacterInPut.character.PassiveSkill();
        return true;

    }

    public static bool TryActiveSkill(this ICharacterInPut iCharacterInPut)
    {
        if (!InputManager.IsActiveSkillKeyDown()) return false;
        iCharacterInPut.character.ActiveSkill();
        return true;

    }

    public static void CharacterMove (this ICharacterInPut iCharacterInPut)
    {
        iCharacterInPut.character.SetMoveInput(InputManager.ReadMoveValue());

    }




    //public static void NormalAttack(this ICharacterInPut iCharacterInPut)
    //{
    //    if (!InputManager.IsNormalAttackKeyDown()) return;
    //    iCharacterInPut.character.Attack();
    //}








}
