using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.UIElements;


//[System.Serializable]
//public struct Item
//{
//    public DroppedItem itemScript;
//    public GameObject itemPrefab;

//    public override bool Equals(object obj)
//    {
//        if (obj is Item other)
//        {
//            return Equals(itemScript, other.itemScript) && Equals(itemPrefab, other.itemPrefab);
//        }
//        return false;
//    }

//    public static bool operator ==(Item a, Item b)
//    => a.itemScript.Equals(b.itemScript) && a.itemPrefab.Equals(b.itemPrefab);
//    public static bool operator !=(Item a, Item b)
//        => !a.itemScript.Equals(b.itemScript) || !a.itemPrefab.Equals(b.itemPrefab);

//    public override int GetHashCode() => HashCode.Combine(itemScript, itemPrefab);

//}

[System.Serializable]
public class ItemPool
{
    [SerializeField] private GameObject itemPrefab;
    private int size = 1;
    private Queue<GameObject> queue;

    public int poolSize => size;
    public int runtimeSize => queue.Count;

    public ItemPool(GameObject gameObject) => itemPrefab = gameObject;
    public GameObject prefab => itemPrefab;
    private Transform parent;

    public void Initialize(Transform parent)
    {
        queue = new Queue<GameObject>();    
        this.parent = parent;               

        for (var i = 0; i < size; i++) queue.Enqueue(Copy());
    }
    private GameObject Copy()
    {
        var copy = GameObject.Instantiate(prefab, parent);
        copy.SetActive(false); 
        return copy;

    }
    private GameObject AvailableObject()
    {
        GameObject availableObject = null;

        if (queue.Count > 0 && !queue.Peek().activeSelf)
            availableObject = queue.Dequeue();
        else
            availableObject = Copy();

        queue.Enqueue(availableObject); 

        return availableObject;
    }

    public GameObject PreparedObject()
    {
        GameObject preparedObject = AvailableObject();
        preparedObject.SetActive(true);

        return preparedObject;
    }

    public GameObject PreparedObject(Vector3 position)
    {
        GameObject preparedObject = AvailableObject();
        preparedObject.SetActive(true);
        preparedObject.transform.position = position;

        return preparedObject;
    }


    public void Return(GameObject gameObject)
    {
        gameObject.SetActive(false);
        gameObject.transform.position = Vector3.zero;
        gameObject.transform.rotation = Quaternion.identity;

        queue.Enqueue(gameObject);
    }

}
