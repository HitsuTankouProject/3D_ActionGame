using Cysharp.Threading.Tasks;
using Fusion;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;
using static Fusion.Sockets.NetBitBuffer;

public class InGame : MonoBehaviour
{
    public static InGame Instance {  get; private set; }
    private const GameScene sceneName = GameScene.InGame;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(Instance);
    }

    private GameManager _gameManager => GameManager.Instance;
    private Player _player => _gameManager.gamePlayer;
    private Character _controlling_Character => _player.controlling_Character;

    private NetworkManager _networkManager => NetworkManager.Instance;
    private NetworkRunner _networkRunner => _networkManager.runner;

    private enum InGameStage { Init, Playing, End}
    [Header("InGame Stage")]
    [SerializeField] private InGameStage gameStage = InGameStage.Init;
    public bool IsGamePlaying() => gameStage == InGameStage.Playing;


    #region Actor

    [Header("Player UseCanvas Part")]
    public HealthBar canvasHealthBar;
    public ColorBar canvasColorBar;
    [Header("Player Follow Camera")]
    public PlayerCameraFollow playerCameraFollow;

    [SerializeField] private Camera miniMapCamera;
    private const float miniMapCameraHeight = 20;
    private void MiniMapCameraUpdate()
    {
        Vector3 targetPosition = _controlling_Character.transform.position;
        miniMapCamera.transform.position = new Vector3(targetPosition.x, targetPosition.y + miniMapCameraHeight, targetPosition.z);
    }

    public List<Character> allPlayerCharacters { get; private set; } = new();

    public void RegisterCharacter(Character character)
    {
        if (character == null || allPlayerCharacters.Contains(character)) return;

        allPlayerCharacters.Add(character);
    }
    public void UnregisterCharacter(Character character)
    {
        if (character == null || !allPlayerCharacters.Remove(character)) return;
    }

    [Header("Character Prefab")]
    [SerializeField] private NetworkPrefabRef adventurerPrefab;
    [SerializeField] private NetworkPrefabRef magicianPrefab;
    [SerializeField] private NetworkPrefabRef thiefPrefab;
    [SerializeField] private NetworkPrefabRef warriorPrefab;

    private NetworkPrefabRef CharacterPrefab(CharacterType targetType)
    {
        switch (targetType)
        {
            case CharacterType.Adventurer: return adventurerPrefab;
            case CharacterType.Magician: return magicianPrefab;
            case CharacterType.Thief: return thiefPrefab;
            case CharacterType.Warrior: return warriorPrefab;

            default: Debug.LogError("How???????"); return default;
        }
    }

    [Header("Character Spawn")]
    [SerializeField] private Transform[] characterSpawnPoints;
    //private Status CharacterSpawnStatus() => _player.TargetCharacterStatus();

    const float spawnInterval = 2.0f;
    private void GetSpawnTransform(PlayerRef playerRef, out Vector3 spawnPosition, out Quaternion spawnRotation)
    {
        int playerIndex = Mathf.Max(0, playerRef.RawEncoded - 1);

        if (characterSpawnPoints == null || characterSpawnPoints.Length == 0)
        {

            spawnPosition = new Vector3(playerIndex * spawnInterval, 0.0f, 0.0f);
            spawnRotation = Quaternion.identity;
            return;
        }

        int spawnPointIndex = playerIndex % characterSpawnPoints.Length;

        Transform spawnPoint = characterSpawnPoints[spawnPointIndex];

        if (spawnPoint == null)
        {
            Debug.LogError($"Spawn Point {spawnPointIndex} is null.", this);

            spawnPosition = new Vector3(playerIndex * 2.0f, 0.0f, 0.0f);

            spawnRotation = Quaternion.identity;
            return;
        }

        spawnPosition = spawnPoint.position;
        spawnRotation = spawnPoint.rotation;

        Debug.Log(
            $"Spawn position selected.\n" +
            $"Player: {playerRef}\n" +
            $"RawEncoded: {playerRef.RawEncoded}\n" +
            $"Spawn Point Index: {spawnPointIndex}\n" +
            $"Position: {spawnPosition}",
            this);
    }

    private void SpawnLocalPlayer()
    {
        PlayerRef localPlayer = _networkRunner.LocalPlayer;
        CharacterType characterType = _player.choseCharacter;
        if (_networkRunner.TryGetPlayerObject(localPlayer, out NetworkObject existingPlayerObject))
        {
            if (_controlling_Character == null)
            {
                Debug.LogError(" Player Controlling Character is null");
                return;
            }

            Debug.LogWarning($"Player Objectはすでに存在します：{localPlayer}", this);

            return;
        }

        if (!CharacterPrefab(characterType).IsValid)
        {
            Debug.LogError("Player Prefab is not assigned.", this);
            return;
        }

        GetSpawnTransform(localPlayer, out Vector3 spawnPosition, out Quaternion spawnRotation);

        Character character = null;
        NetworkObject playerObject =
            _networkRunner.Spawn(
                CharacterPrefab(characterType),
                spawnPosition,
                spawnRotation,
                localPlayer,
                onBeforeSpawned:
                    (runner, networkObject) =>
                    {
                        character = networkObject.GetComponent<Character>();
                    });

        if (playerObject == null)
        {
            Debug.LogError("Failed to spawn Player Object.", this);
            return;
        }

        _networkRunner.SetPlayerObject(localPlayer, playerObject);

        _player.SetUpCharacter(character, canvasHealthBar, canvasColorBar);



        Debug.Log(
            $"Playerを生成しました。\n" +
            $"Player：{localPlayer}\n" +
            $"Position：{spawnPosition}",
            playerObject);
    }


    public List<Monster> allMonsters { get; private set; } = new();
    public void RegisterMonster(Monster monster)
    {
        if (monster == null || allMonsters.Contains(monster)) return;
        allMonsters.Add(monster);

    }
    public void UnregisterMonster(Monster monster)
    {
        if (monster == null || !allMonsters.Remove(monster)) return;
    }

    public void UpdateCharacterAndMonsterList()
    {
        allMonsters.RemoveAll(character => character == null);

        allPlayerCharacters.RemoveAll(character => character == null);

        if (allPlayerCharacters.Count <= 0)
        {
            Debug.LogError("[InGame] There have no any Characters here");
        }
    }

    #endregion

    public float commandTime => GameManager.commandFps * Time.deltaTime;




    private async UniTask InitializeGameScene()
    {
        gameStage = InGameStage.Init;
        if (_gameManager == null)
        {
            Debug.LogError("_gameManager == null");
            return;
        }
        _gameManager.UpdateGameScene(sceneName);

        Debug.Log("Test2 Scene has been loaded.", this);

        await UniTask.WaitUntil(() => NetworkManager.Instance != null);

        if (_networkRunner == null)
        {
            Debug.LogError("NetworkRunner is not assigned.", this);

            return;
        }

        await UniTask.WaitUntil(() => _networkRunner.IsRunning);

        if (!_networkRunner.SessionInfo.IsValid)
        {
            Debug.LogError("Network Session is invalid.", this);

            return;
        }

        SpawnLocalPlayer();
        gameStage = InGameStage.Playing;

    }

    private async UniTask EndGameProcess()
    {
        UpdateCharacterAndMonsterList();
        foreach (Character character in allPlayerCharacters) character.ReturnIdle();
    }





    private void Start()
    {
        InitializeGameScene().Forget();
    }

    private void LateUpdate()
    {
        if (gameStage != InGameStage.Playing) return;
        if (_gameManager == null || _controlling_Character == null) return;
        MiniMapCameraUpdate();
        UpdateCharacterAndMonsterList();
    }



}
