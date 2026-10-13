using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

public interface IItemDrop { }

public static class ItemDropSystem
{
    private static ItemPoolManager _itemPoolManager => ItemPoolManager.Instance;
    private static GameManager _gameManager => GameManager.Instance;

    private const float dropLandingY = 0.5f;
    private const float dropFlightSeconds = 0.6f;
    private const float dropArcHeight = 3.0f;
    private const float dropMinSpreadRadius = 2.5f;
    private const float dropMaxSpreadRadius = 5.0f;


    /// <summary>
    /// アイテムを放物線で移動させ、指定された高さへ着地させる。
    /// </summary>
    private static async UniTask MoveDropObject(GameObject dropObject)
    {
        CancellationToken cancellationToken = dropObject.GetCancellationTokenOnDestroy();

        Vector3 startPosition = dropObject.transform.position;
        float angle = UnityEngine.Random.Range(0.0f, Mathf.PI * 2.0f);
        float distance = UnityEngine.Random.Range(dropMinSpreadRadius, dropMaxSpreadRadius);

        Vector2 spread = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

        Vector3 landingPosition = new Vector3( startPosition.x + spread.x, dropLandingY, startPosition.z + spread.y);

        float elapsedTime = 0.0f;

        while (elapsedTime < dropFlightSeconds)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsedTime / dropFlightSeconds);

            Vector3 position = Vector3.Lerp(startPosition, landingPosition, progress);

            // 放物線の高さを加える。
            position.y += 4.0f * dropArcHeight * progress * (1.0f - progress);

            dropObject.transform.position = position;
        }

        dropObject.transform.position = landingPosition;
    }
    public static void ResetItemDropSystem()
    {
        allDropWeaponWeight.Clear();
        totalWeight = 0;
        WeaponType[] usingWeapons = _gameManager.characterCanPickWeapon[_gameManager.gamePlayer.choseCharacter];

        foreach (WeaponType weaponType in Enum.GetValues(typeof(WeaponType)))
        {
            if (weaponType == WeaponType.None || weaponType == WeaponType.Staff || weaponType == WeaponType.Knife || weaponType == WeaponType.Greatsword) continue;

            int weight = usingWeapons.Contains(weaponType) ? usingDropWeaponWeight : normalDropWeaponWeight;

            allDropWeaponWeight[weaponType] = weight;
            totalWeight += weight;
        }
    }

    private static GameObject DropColorBall(this IItemDrop target)
    {
        GameObject result = _itemPoolManager.Release(ItemType.ColorBall);
        if(result.TryGetComponent<ColorBall>(out ColorBall colorBall))
        {
            colorBall.SetUp();
        }

        return result; 
    }
    private static async UniTask<GameObject> DropColorBall(Vector3 position)
    {
        GameObject result = _itemPoolManager.Release(ItemType.ColorBall, position);

        await MoveDropObject(result);

        if (result.TryGetComponent<ColorBall>(out ColorBall colorBall)) colorBall.SetUp();

        return result;
    }

    public static void DropMultiColorBallIn(this IItemDrop target, Vector3 position , int count = 1)
    {
        if (count < 1) return;
        for (int i = 0; i < count; i++) DropColorBall(position).Forget();

    }

    private static readonly int[] dropWeaponRareWeight = new int[5] { 70, 55, 40, 25, 10 };
    private static uint GetDropWeaponRare()
    {
        int totalWeight = 0;
        foreach (int rareWeight in dropWeaponRareWeight) totalWeight += rareWeight;

        int randomWeight = UnityEngine.Random.Range(0, totalWeight);

        for (int rareIndex = 0; rareIndex < dropWeaponRareWeight.Length; rareIndex++)
        {
            if (randomWeight < dropWeaponRareWeight[rareIndex]) return (uint)(rareIndex + 1);
            randomWeight -= dropWeaponRareWeight[rareIndex];
        }

        return 1;
    }

    private const int normalDropWeaponWeight = 40;
    private const int usingDropWeaponWeight = 70;
    private static int totalWeight = 0;
    private static Dictionary<WeaponType, int> allDropWeaponWeight = new();

    private static WeaponType GetDropWeapon()
    {
        int randomWeight = UnityEngine.Random.Range(0, totalWeight);

        foreach (KeyValuePair<WeaponType, int> weaponWeight in allDropWeaponWeight)
        {
            if (randomWeight < weaponWeight.Value) return weaponWeight.Key;

            randomWeight -= weaponWeight.Value;
        }

        return WeaponType.None;
    }

    private static bool TryGetDropWeaponStatus(out WeaponStatus weaponStatus)
    {
        WeaponType weapon = GetDropWeapon();
        uint rare = GetDropWeaponRare();

        if(!_gameManager.TryGetTargetWeaponStatus(weapon, rare, out weaponStatus))
        {
            Debug.LogWarning("[ItemDropSystem] weaponStatus == null");
            return false;
        }

        return weaponStatus != default;
    }

    private static GameObject DropWeaponIn(this IItemDrop target)
    {
        if (!TryGetDropWeaponStatus(out WeaponStatus weaponStatus)) return null;

        GameObject result = _itemPoolManager.Release(ItemType.Weapon);
        if (result.TryGetComponent<WeaponDrop>(out WeaponDrop weaponDrop))
        {
            weaponDrop.SetUpWeapon(weaponStatus);
        }
        return result;
    }
    private static async UniTask<GameObject> DropWeaponIn(Vector3 position)
    {
        if (!TryGetDropWeaponStatus(out WeaponStatus weaponStatus)) return null;

        GameObject result = _itemPoolManager.Release(ItemType.Weapon, position);

        await MoveDropObject(result);

        if (result.TryGetComponent<WeaponDrop>(out WeaponDrop weaponDrop)) weaponDrop.SetUpWeapon(weaponStatus);

        return result;
    }
    public static void DropMultiWeaponIn(this IItemDrop target, Vector3 position, int count = 1)
    {
        if (count < 1) return;
        for (int i = 0; i < count; i++) DropWeaponIn(position).Forget();

    }














}
