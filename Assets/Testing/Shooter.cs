using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;




public class Shooter : MonoBehaviour
{
    public Transform shootingPoint;

    [Range(0.0f, 90.0f)]
    public float shootAngle;
    private float nowShootAngle = -1.0f;

    [Range(1, 15)]
    public uint ballteIndex;
    private uint nowBallteIndex = 0;

    private Quaternion nowForward;

    public GameObject battle;
    List<Vector3> allShootAngle = new();

    private List<Vector3> AllTheShootAngle()
    {
        List<Vector3> result = new((int)ballteIndex);
        Vector3 forward = shootingPoint.forward * 0.1f;

        if (ballteIndex == 1)
        {
            result.Add(forward);
            return result;
        }
        float angleBetween = (shootAngle * 2) / (ballteIndex - 1);
        float startAngle = -(ballteIndex - 1) * angleBetween * 0.5f;

        for (int i = 0; i < ballteIndex; i++)
        {
            float angle = startAngle + angleBetween * i;

            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * forward;

            result.Add(direction);
        }
        return result;

    }

    private bool IsNeedNewAngle()
    {
        bool result =
            (nowShootAngle != shootAngle)
            || (nowBallteIndex != ballteIndex)
            || (nowForward != shootingPoint.rotation);

        return result;
    }

    private void Shoot()
    {
        if (IsNeedNewAngle())
        {
            Debug.Log("Finding new Shoot angle");
            allShootAngle = AllTheShootAngle();

            nowShootAngle = shootAngle;
            nowBallteIndex = ballteIndex;
            nowForward = shootingPoint.rotation;

        }

        foreach (Vector3 angle in allShootAngle)
        {
            Instantiate(battle, angle, Quaternion.LookRotation(angle));
        }
    }


    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Shoot();
        }

        if (Keyboard.current.wKey.isPressed)
        {
            transform.Rotate(-50.0f * Time.deltaTime, 0, 0f);
        }
        else if (Keyboard.current.sKey.isPressed)
        {
            transform.Rotate(50.0f * Time.deltaTime, 0, 0f);
        }
        else if(Keyboard.current.aKey.isPressed)
        {
            transform.Rotate(0f, -50.0f * Time.deltaTime, 0f);
        }
        else if (Keyboard.current.dKey.isPressed)
        {
            transform.Rotate(0f, 50.0f * Time.deltaTime, 0f);
        }

    }

}
