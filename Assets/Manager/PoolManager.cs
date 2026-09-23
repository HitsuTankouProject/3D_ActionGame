using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Pool
{
    public Queue<NetworkObject> objectQueue{ get; } = new();

    public int poolMaxVolume { get; private set; }

    public Pool(int maximumVolume = 100)
    {
        poolMaxVolume = maximumVolume;
    }

}

//参考用URL：https://doc.photonengine.com/fusion/v2/technical-samples/fusion-object-pooling?utm_source=chatgpt.com

public class PoolManager : Fusion.Behaviour, INetworkObjectProvider
{
    public static PoolManager Instance {  get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(this);
            return;
        }


    }

    [InlineHelp] public bool DelayIfSceneManagerIsBusy = true;
    private Dictionary<NetworkPrefabId, Pool> allObjectPools = new Dictionary<NetworkPrefabId, Pool>();
    public void Initialize(NetworkRunner runner) => allObjectPools.Clear();

    protected NetworkObject InstantiatePrefab(NetworkRunner runner, NetworkObject prefab, NetworkPrefabId contextPrefabId)
    {
        var result = default(NetworkObject);

        // Found free queue for prefab AND the queue is not empty. Return free object.
        if (allObjectPools.TryGetValue(contextPrefabId, out Pool objectPool))
        {

            if (objectPool.objectQueue.Count > 0)
            {
                result = objectPool.objectQueue.Dequeue();
                result.gameObject.SetActive(true);
                return result;
            }
        }
        else
        {
            allObjectPools.Add(contextPrefabId, new Pool());
        }

        // -- At this point a free queue was not yet created or was empty. Create new object.
        result = Instantiate(prefab);

        return result;
    }
    protected void DestroyPrefabInstance(NetworkRunner runner, NetworkPrefabId prefabId, NetworkObject instance)
    {
        // No free queue for this prefab OR the pool already have the max amount of object we defined. Should be destroyed.
        if (allObjectPools.TryGetValue(prefabId, out Pool objectPool) == false
            || (objectPool.poolMaxVolume > 0 && objectPool.objectQueue.Count >= objectPool.poolMaxVolume))
        {            Destroy(instance.gameObject);
            return;
        }

        // Free queue found. Should cache.
        objectPool.objectQueue.Enqueue(instance);

        // Make objects inactive.
        instance.gameObject.SetActive(false);
    }

    public NetworkObjectAcquireResult AcquireInstance(NetworkRunner runner, in NetworkObjectAcquireContext context, out NetworkObject instance)
    {

        instance = null;

        if (DelayIfSceneManagerIsBusy && runner.SceneManager.IsBusy) return NetworkObjectAcquireResult.Retry;

        // For scene objects Fusion provides the instance via context.AttachableInstance,
        // the provider just needs to return it. Scene objects are never pooled.
        if (context.TypeId.IsSceneObject)
        {
            if (context.AttachableInstance)
            {
                instance = context.AttachableInstance;
                return NetworkObjectAcquireResult.Success;
            }

            // the scene will be loaded, eventually
            return NetworkObjectAcquireResult.Retry;
        }

        Assert.Check(context.AttachableInstance == null, "AttachableInstance is not supported for prefabs");

        if (!context.TypeId.IsPrefab)
        {
            return NetworkObjectAcquireResult.Failed;
        }

        var prefabId = context.TypeId.AsPrefabId;

        NetworkObject prefab;
        try
        {
            prefab = runner.Prefabs.Load(prefabId, isSynchronous: context.IsSynchronous);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load prefab: {ex}");
            return NetworkObjectAcquireResult.Failed;
        }

        if (!prefab)
        {
            // this is ok, as long as Fusion does not require the prefab to be loaded immediately;
            // if an instance for this prefab is still needed, this method will be called again next update
            return NetworkObjectAcquireResult.Retry;
        }

        instance = InstantiatePrefab(runner, prefab, prefabId);
        Assert.Check(instance);

        if (context.DontDestroyOnLoad)
        {
            runner.MakeDontDestroyOnLoad(instance.gameObject);
        }
        else
        {
            runner.MoveToRunnerScene(instance.gameObject);
        }

        runner.Prefabs.AddInstance(prefabId);
        return NetworkObjectAcquireResult.Success;
    }

    public void ReleaseInstance(NetworkRunner runner, in NetworkObjectReleaseContext context)
    {
        var instance = context.Object;

        // Only pool prefabs.
        if (!context.IsBeingDestroyed)
        {
            switch (context.TypeId.Kind)
            {
                case NetworkTypeIdKind.Prefab:
                    DestroyPrefabInstance(runner, context.TypeId.AsPrefabId, instance);
                    break;
                case NetworkTypeIdKind.PrefabNested:
                    // Do NOT destroy nested prefab objects: their GameObject is part of a pooled root's baked
                    // hierarchy. Deactivate instead - Fusion re-attaches and re-activates them when the pooled
                    // root is reused.
                    instance.gameObject.SetActive(false);
                    break;
                default:
                    Destroy(instance.gameObject);
                    break;
            }
        }

        if (context.TypeId.IsPrefab)
        {
            runner.Prefabs.RemoveInstance(context.TypeId.AsPrefabId);
        }
    }
    // Without this, prefabs loaded through the provider are never unloaded when the runner shuts down.
    public void Shutdown(NetworkRunner runner)
    {
        var prefabs = runner.Prefabs;
        if (prefabs?.Options.UnloadUnusedPrefabsOnShutdown == true)
        {
            prefabs.UnloadUnreferenced(includeIncompleteLoads: true);
        }
    }

    public NetworkPrefabId GetPrefabId(NetworkRunner runner, NetworkObjectGuid prefabGuid)
        => runner.Prefabs.GetId(prefabGuid);

}
