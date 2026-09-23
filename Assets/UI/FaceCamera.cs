using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Camera targetCamera => Camera.main;

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        transform.forward = targetCamera.transform.forward;
    }
}
