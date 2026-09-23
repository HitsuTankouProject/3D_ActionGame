using Cysharp.Threading.Tasks;
using Fusion;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Unity.Collections.Unicode;
public enum GameScene { None = -1, GameTitle = 0, Lobby = 1, InGame = 2, Release = 3, Error}
public enum GameDevice { Pc, Mobile }
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }


    private NetworkManager _networkManager => NetworkManager.Instance;
    public Player gamePlayer;

    public static int player_uid { get; private set; } = -1;
    public const int gameFps = 60;
    public const int commandFps = gameFps / 10;
    public const float commandTime = (float)commandFps / gameFps;

    #region GameScene
    public GameScene nowGameScene { get; private set; } = GameScene.None;
    public void UpdateGameScene(GameScene gameScene) => nowGameScene = gameScene;

    #endregion

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(this);
            return;
        }

        DontDestroyOnLoad(this);
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = gameFps;
    }


}
