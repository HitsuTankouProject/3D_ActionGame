using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
public class ColorBall : NetworkBehaviour
{

    private Vector3 rotationAxis = Vector3.up;
    private float rotationSpeed = 180f;



    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        transform.Rotate(rotationAxis, rotationSpeed * Runner.DeltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IColorDamageable>(out IColorDamageable iColorDamageable))
        {
            iColorDamageable.TryIncreaseColor(10);
        }
        Runner.Despawn(Object);
    }


}
