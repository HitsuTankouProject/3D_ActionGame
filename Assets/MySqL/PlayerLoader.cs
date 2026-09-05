using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

//[Serializable]
//public class PlayerStats
//{
//    public float health;
//    public float speed;
//    public int level;
//}
//[Serializable]
//public class PlayerData
//{
//    public int id;
//    public string player_name;
//    public PlayerStats stats;
//}

//[Serializable]
//public class PlayersResponse
//{
//    public bool success;
//    public PlayerData[] players;
//}
//[System.Serializable]
//public class UpdatePlayerRequest
//{
//    public int id;
//    public PlayerStats stats;
//}
//[System.Serializable]
//public class AddPlayerRequest
//{
//    public string player_name;
//    public PlayerStats stats;
//}

//[System.Serializable]
//public class DeletePlayerRequest
//{
//    public int id;
//}





public class PlayerLoader : MonoBehaviour
{
    //private PlayersResponse response;

    //private IEnumerator Start()
    //{
    //    string url = "http://localhost/unity-api/get-players.php";

    //    using UnityWebRequest request = UnityWebRequest.Get(url);

    //    yield return request.SendWebRequest();

    //    if (request.result != UnityWebRequest.Result.Success)
    //    {
    //        Debug.LogError("API error: " + request.error);
    //        yield break;
    //    }

    //    response =
    //        JsonUtility.FromJson<PlayersResponse>(
    //            request.downloadHandler.text
    //        );
    //    playerData = response.players[0];
    //    Debug.Log(
    //                    $"Player: {playerData.player_name}, " +
    //                    $"Health: {playerData.stats.health}, " +
    //                    $"Speed: {playerData.stats.speed}, " +
    //                    $"Level: {playerData.stats.level}"
    //                );
    //}

    //public bool test = false;
    //public PlayerStats status;
    //private PlayerData playerData;

    //private IEnumerator UpdatePlayerData()
    //{
    //    UpdatePlayerRequest updateData = new UpdatePlayerRequest
    //    {
    //        id = playerData.id,
    //        stats = playerData.stats
    //    };

    //    string json = JsonUtility.ToJson(updateData);

    //    string url =
    //        "http://localhost/unity-api/update-player.php";

    //    using UnityWebRequest request =
    //        new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);

    //    byte[] body = Encoding.UTF8.GetBytes(json);

    //    request.uploadHandler = new UploadHandlerRaw(body);
    //    request.downloadHandler = new DownloadHandlerBuffer();
    //    request.SetRequestHeader(
    //        "Content-Type",
    //        "application/json"
    //    );

    //    yield return request.SendWebRequest();

    //    if (request.result != UnityWebRequest.Result.Success)
    //    {
    //        Debug.LogError(
    //            "Save failed: " + request.error +
    //            "\n" + request.downloadHandler.text
    //        );

    //        yield break;
    //    }

    //    Debug.Log(
    //        "Save response: " +
    //        request.downloadHandler.text
    //    );
    //}
    //private IEnumerator AddPlayer(string playerName, PlayerStats stats)
    //{
    //    AddPlayerRequest player = new AddPlayerRequest
    //    {
    //        player_name = playerName,
    //        stats = stats
    //    };

    //    string json = JsonUtility.ToJson(player);
    //    byte[] body = Encoding.UTF8.GetBytes(json);

    //    string url =
    //        "http://localhost/unity-api/add-player.php";

    //    using UnityWebRequest request =
    //        new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);

    //    request.uploadHandler = new UploadHandlerRaw(body);
    //    request.downloadHandler = new DownloadHandlerBuffer();

    //    request.SetRequestHeader(
    //        "Content-Type",
    //        "application/json"
    //    );

    //    yield return request.SendWebRequest();

    //    if (request.result != UnityWebRequest.Result.Success)
    //    {
    //        Debug.LogError(
    //            "Add player failed: " +
    //            request.downloadHandler.text
    //        );

    //        yield break;
    //    }

    //    Debug.Log(
    //        "Add player response: " +
    //        request.downloadHandler.text
    //    );
    //}

    //private IEnumerator DeletePlayer(int playerId)
    //{
    //    DeletePlayerRequest data = new DeletePlayerRequest
    //    {
    //        id = playerId
    //    };

    //    string json = JsonUtility.ToJson(data);
    //    byte[] body = Encoding.UTF8.GetBytes(json);

    //    string url =
    //        "http://localhost/unity-api/delete-player.php";

    //    using UnityWebRequest request =
    //        new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);

    //    request.uploadHandler = new UploadHandlerRaw(body);
    //    request.downloadHandler = new DownloadHandlerBuffer();

    //    request.SetRequestHeader(
    //        "Content-Type",
    //        "application/json"
    //    );

    //    yield return request.SendWebRequest();

    //    if (request.result != UnityWebRequest.Result.Success)
    //    {
    //        Debug.LogError(
    //            "Delete failed: " +
    //            request.downloadHandler.text
    //        );

    //        yield break;
    //    }

    //    Debug.Log(
    //        "Delete response: " +
    //        request.downloadHandler.text
    //    );
    //}



    //private void Update()
    //{
    //    if (test)
    //    {
    //        test = false;
    //        StartCoroutine(DeletePlayer(1));

    //    }
    //}
}
