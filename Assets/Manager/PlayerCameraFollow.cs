using UnityEngine;
using UnityEngine.TextCore.Text;

public class PlayerCameraFollow : MonoBehaviour
{
    private Character _character => GameManager.Instance.gamePlayer.controlling_Character;

    private float smoothTime = 0.15f;
    private Vector3 offset;

    private Vector3 currentOffset;
    private Vector3 followVelocity;
    private float lookTargetHeight = 1.0f;

    private void Start()
    {
        offset = transform.position;
    }


    public bool keepFollowBehind = false;
    public void TurnCameraToCharacterBack()
    {

        if (_character == null) return;

        currentOffset = _character.transform.rotation * offset;

        Vector3 cameraPosition = _character.transform.position + currentOffset;

        Vector3 lookTarget = _character.transform.position + Vector3.up * lookTargetHeight;

        transform.position = cameraPosition;

        transform.rotation = Quaternion.LookRotation(lookTarget - cameraPosition, Vector3.up);

        followVelocity = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (_character == null) return;

        Vector3 targetPosition = _character.transform.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref followVelocity, smoothTime);
    }

}
