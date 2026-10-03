using Fusion;
using UnityEngine;
using UnityEngine.AI;


[RequireComponent(typeof(NetworkRunner))]
[RequireComponent(typeof(NetworkSceneManagerDefault))]
public class RunnerPrefab : MonoBehaviour
{
    public NetworkRunner networkRunner => GetComponent<NetworkRunner>();
    public NetworkSceneManagerDefault networkSceneManagerDefault => GetComponent<NetworkSceneManagerDefault>();

}
