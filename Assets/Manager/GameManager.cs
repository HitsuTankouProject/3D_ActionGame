using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public NetworkManager networkManager;
    public Player player;
    public static int player_uid { get; private set; } = -1;
    public const int gameFps = 60;
    public const float commandTime = 6 / gameFps;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);

    }


}
