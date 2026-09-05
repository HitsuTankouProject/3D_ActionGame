using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.InputSystem;


struct LightData
{
    public Vector3 startPoint;
    public Vector3 endPoint;

    public LightData(Vector3 start, Vector3 end)
    {
        startPoint = start; endPoint = end;
    }
}

[RequireComponent(typeof(LineRenderer))]
public class Light : MonoBehaviour
{
    public Transform shootingPoint;

    [Header("Lighr lenght")]
    [Range(1.0f, 10.0f)]
    public float lenght;

    [Header("z-x map")]
    [Range(0.0f, 360.0f)]
    public float horizonAngle;

    [Header("y-z map")]
    [Range(0.0f, 360.0f)]
    public float verticalAngle;


    private List<LightData> lightDataList = new();

    private Vector3 Direction()
    {
        float h = horizonAngle * Mathf.Deg2Rad;
        float v = verticalAngle * Mathf.Deg2Rad;

        float x = Mathf.Sin(h) * Mathf.Cos(v);
        float y = Mathf.Sin(v);
        float z = Mathf.Cos(h) * Mathf.Cos(v);

        return new Vector3(x, y, z);
    }


    [Header("Test")]
    private LineRenderer lineRenderer;
    public bool test = false;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;

        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;

        Reflect();


    }

    private Vector3 CalculateReflection(Vector3 incident, Vector3 normal)
    {
        incident = incident.normalized;
        normal = normal.normalized;

        float dot = incident.x * normal.x + incident.y * normal.y + incident.z * normal.z;

        Vector3 reflect = incident - 2f * dot * normal;

        return reflect.normalized;
    }

    private void Reflect()
    {
        Vector3 start = shootingPoint.position;
        //Vector3 dir = Direction();
        Vector3 dir = shootingPoint.forward;
        Vector3 newDir = Vector3.zero;
        lightDataList.Clear();

        float canUseLenght = lenght;
        int lightPoint = 2;
        bool haveReflect = false;

        while (true)
        {
            RaycastHit hit;
            if (!Physics.Raycast(start, dir, out hit, canUseLenght)) break;

            haveReflect = true;
            lightPoint++;

            Vector3 reflectPoint = hit.point;
            canUseLenght -= Mathf.Abs(Vector3.Distance(reflectPoint, start));

            newDir = CalculateReflection(dir, hit.normal);
            dir = newDir;

            lightDataList.Add(new LightData(start, reflectPoint));

            start = reflectPoint;
        }

        if (!haveReflect)
            lightDataList.Add(new LightData(shootingPoint.position, (shootingPoint.position + dir * canUseLenght)));
        else
            lightDataList.Add(new LightData(start, (start + dir * canUseLenght)));

        lineRenderer.positionCount = lightPoint;
        int checkPoint = lightPoint - lightDataList.Count;
        if (checkPoint != 1) Debug.LogError("Light Point doesnt Fair");

        for (int i = 0; i < lightDataList.Count; i++)
        {
            lineRenderer.SetPosition(i, lightDataList[i].startPoint);
            lineRenderer.SetPosition(i+1, lightDataList[i].endPoint);
        }


    }

    private void Update()
    {
        Reflect();



        if (Keyboard.current.wKey.isPressed)
        {
            transform.Rotate(-50.0f * Time.deltaTime, 0, 0f);
        }
        else if (Keyboard.current.sKey.isPressed)
        {
            transform.Rotate(50.0f * Time.deltaTime, 0, 0f);
        }
        else if (Keyboard.current.aKey.isPressed)
        {
            transform.Rotate(0f, -50.0f * Time.deltaTime, 0f);
        }
        else if (Keyboard.current.dKey.isPressed)
        {
            transform.Rotate(0f, 50.0f * Time.deltaTime, 0f);
        }

        //if (Keyboard.current.spaceKey.wasPressedThisFrame)
        //{
            
        //}
        

    }


}
