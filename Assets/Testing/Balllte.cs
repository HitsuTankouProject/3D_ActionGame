using UnityEngine;

public class Balllte : MonoBehaviour
{
    [Range(1.0f, 10.0f)]
    public float lenght;
    [Range(1.0f, 10.0f)]
    public float speed;

    private float totalRunTime => lenght / speed;
    private float timer = 0; 

    private void Reflect(Vector3 normal)
    {

    }


    private void Update()
    {
        if (timer >= totalRunTime) Destroy(gameObject);
        timer += Time.deltaTime;
        transform.position = transform.forward * Time.deltaTime * speed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 normal = collision.contacts[0].normal;
        Reflect(normal);
    }
}
