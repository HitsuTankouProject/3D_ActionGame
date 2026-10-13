using System.Collections.Generic;
using UnityEngine;

public class ItemPoolManager : MonoBehaviour
{
    public static ItemPoolManager Instance { get; private set; }
    [Header("All Item Prefab")]
    [SerializeField] private DroppedItem[] allItemPrefabs;
    private Dictionary<ItemType, ItemPool> dictionary = new Dictionary<ItemType, ItemPool>();
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        AllPoolInit();
    }

    public void AllPoolInit()
    {
        foreach(DroppedItem droppedItem in allItemPrefabs)
        {
            if (droppedItem == null)
            {
                Debug.LogError($"{droppedItem} Model Missing");
                continue;
            }
            if(droppedItem.type == ItemType.None)
            {
                Debug.LogWarning($"{droppedItem} type == ItemType.None");
                continue;
            }

            ItemPool itemPool = new ItemPool(droppedItem.gameObject);
            Transform itemPoolParent = new GameObject($"Pool : {droppedItem.type.ToString()}").transform;
            itemPool.Initialize(itemPoolParent);
            dictionary[droppedItem.type] = itemPool;

            itemPoolParent.parent = this.transform;
        }
    }

    public GameObject Release(ItemType itemType) 
        => dictionary[itemType].PreparedObject();
    public GameObject Release(ItemType itemType, Vector3 position)
    {
        return dictionary[itemType].PreparedObject(position);
    }

    public void ReturnToPool(DroppedItem droppedItem)
    {
        if(droppedItem == null || droppedItem.type == ItemType.None)
        {
            Debug.LogWarning("[ItemPoolManager] Cant return non typed item");
            return;
        }
        dictionary[droppedItem.type].Return(droppedItem.gameObject);
    }

}
