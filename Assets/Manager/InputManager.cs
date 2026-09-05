using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;



public class InputManager : MonoBehaviour
{
    private GameManager _gameManager => GameManager.Instance;
    private float _commandTime => GameManager.commandTime;
    public Character _character => _gameManager.player.controlling_Character;

    

    private void Update()
    {
        if (_character == null) return;
        if (Keyboard.current.wKey.isPressed)
        {
            _character.RequestChangeStage(PlayerStage.Run, _character.transform.position + Vector3.forward);
        }
        //if (Keyboard.current.aKey.isPressed)
        //{
        //    _character.RequestChangeStage(PlayerStage.Run, _character.transform.position + Vector3.left);
        //}
        //if (Keyboard.current.sKey.isPressed)
        //{
        //    _character.RequestChangeStage(PlayerStage.Run, _character.transform.position + Vector3.back);
        //}
        //if (Keyboard.current.dKey.isPressed)
        //{
        //    _character.RequestChangeStage(PlayerStage.Run, _character.transform.position + Vector3.right);
        //}

        //if (Keyboard.current.wKey.wasReleasedThisFrame || Keyboard.current.aKey.wasReleasedThisFrame || Keyboard.current.sKey.wasReleasedThisFrame || Keyboard.current.dKey.wasReleasedThisFrame)
        //{

        //    _character.ReturnIdle();
        //}
        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            _character.RequestChangeStage(PlayerStage.Attack, _character.transform.position + Vector3.forward);
        }
    }
}