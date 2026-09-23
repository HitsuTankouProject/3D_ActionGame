using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using static Unity.Collections.Unicode;



public class InputManager : MonoBehaviour
{
    private GameManager _gameManager => GameManager.Instance;
    public Character _character => _gameManager.gamePlayer.controlling_Character;
    private Keyboard _keyboard => Keyboard.current;
    private Mouse _mouse => Mouse.current;

    private Vector3 ReadMoveInput()
    {
        if (_character == null) return Vector3.zero;

        Vector3 characterForward = _character.transform.forward;

        Vector3 characterRight = _character.transform.right;

        characterForward.y = 0.0f;
        characterRight.y = 0.0f;

        characterForward.Normalize();
        characterRight.Normalize();

        Vector3 inputDirection = Vector3.zero;

        if (_keyboard.wKey.isPressed) inputDirection += characterForward;
        if (_keyboard.sKey.isPressed) inputDirection -= characterForward;
        if (_keyboard.aKey.isPressed) inputDirection -= characterRight;
        if (_keyboard.dKey.isPressed) inputDirection += characterRight;

        return inputDirection.normalized;

    }
    private void CharacterMove()=> _character.SetMoveInput(ReadMoveInput());

    private float mouseTurnSensitivity = 0.01f;
    private Vector3 ReadTurnInput()
    {
        if (_character == null || _mouse == null) return Vector3.zero;

        Vector3 characterRight = _character.transform.right;
        characterRight.y = 0.0f;
        characterRight.Normalize();

        float mouseDeltaX = _mouse.delta.ReadValue().x;
        return characterRight * mouseDeltaX * mouseTurnSensitivity;
    }

    private void CharacterTurn()
    {
        Vector3 result = _character.transform.forward + ReadTurnInput();
        _character.SetTurnInput(result);
    }

    private bool CharacterAttack()
    {
        if (_character == null || _mouse == null) return false;
        if (!_mouse.leftButton.wasPressedThisFrame) return false;
        else
        {
            _character.Attack();
            return true;
        }

    }
    private bool CharacterPassiveSkill()
    {
        if (_character == null || _keyboard == null) return false;
        if (!_keyboard.fKey.isPressed) return false;
        else 
        {
            _character.PassiveSkill();
            return true;
        }
        

    }
    private bool CharacterActiveSkill()
    {
        if (_character == null || _keyboard == null) return false;
        if (!_keyboard.eKey.wasPressedThisFrame) return false;
        else
        {
            _character.ActiveSkill();
            return true;
        }


    }

    private void Update()
    {
        if (_keyboard == null || _character == null || _mouse == null) return;
        CharacterTurn();

        if (CharacterPassiveSkill() || CharacterAttack() || CharacterActiveSkill())
        {
            _character.SetMoveInput(Vector3.zero);
            return;
        }

        CharacterMove();
        
    }


}