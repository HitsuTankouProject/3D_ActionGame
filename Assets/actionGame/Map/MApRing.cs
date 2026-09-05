using UnityEngine;

public class MApRing : MonoBehaviour
{
    public Transform startPos;

    [SerializeField]
    private Vector3 startScale = new Vector3(88f, 5f, 80f);

    [SerializeField]
    private Vector3 endScale = new Vector3(11f, 5f, 10f);

    [SerializeField]
    private float speed = 1f;

    [SerializeField]
    private Transform mapRing;

    private void Start()
    {
        if (mapRing == null)
        {
            Debug.LogError("Map Ring has not been assigned.");
            enabled = false;
            return;
        }
        
        mapRing.localScale = startScale;
        Vector3 position = startPos.position;
        position.y = mapRing.position.y;
        mapRing.position = position;
    }

    private void Update()
    {
        mapRing.localScale = Vector3.MoveTowards(
            mapRing.localScale,
            endScale,
            speed * Time.deltaTime
        );

        if (mapRing.localScale == endScale)
        {
            enabled = false;
        }
    }


}
