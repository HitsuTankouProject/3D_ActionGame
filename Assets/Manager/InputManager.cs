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

    //private CancellationTokenSource waitCommandToken;
    //private bool canAllowCommand => waitCommandToken != null && !waitCommandToken.IsCancellationRequested;

    //private void StartCountingCommandTime()
    //{

    //}

}