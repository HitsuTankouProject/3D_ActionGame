using Cysharp.Threading.Tasks;
using UnityEngine;

public class Test : MonoBehaviour
{
    private NetworkManager _networkManager => NetworkManager.Instance;

    private void Start()
    {
        _networkManager.TestNetworkConnection().Forget();
    }


    /// <summary>
    /// UIのSingle Player Buttonから呼び出す。
    /// </summary>
    public void StartSinglePlayer()
    {
        StartPlayerAsync(RoomMode.Single).Forget();
    }

    /// <summary>
    /// UIのMultiplayer Buttonから呼び出す。
    /// </summary>
    public void StartMultiplayer()
    {
        StartPlayerAsync(RoomMode.Multiplayer).Forget();
    }

    private async UniTask StartPlayerAsync(RoomMode roomMode)
    {
        if (_networkManager.networkMode != NetworkMode. Online)
        {
            Debug.LogError("Photon Lobbyに接続していません。", this);
            return;
        }

        bool result = await _networkManager.StartMatchmaking(roomMode);
        Debug.Log(result ?
            $"{roomMode.ToString()} Roomへの参加に成功しました。" :
            $"{roomMode.ToString()} Roomへの参加に失敗しました。", this);
    }


}