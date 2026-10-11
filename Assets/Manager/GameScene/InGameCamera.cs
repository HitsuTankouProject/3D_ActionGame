using Photon.Realtime;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;

public class InGameCamera : MonoBehaviour
{
    private GameManager _gameManager => GameManager.Instance;
    private InGame _inGame => InGame.Instance;
    private Character _character => GameManager.Instance.gamePlayer.controlling_Character;

    [Header("Follow")]
    private float smoothTime = 0.15f;
    private float lookTargetHeight = 1.0f;
    private float cameraDistance = 6.0f;
    [Header("Rotation")]
    private float mouseSensitivity = 0.15f;
    private float minimumPitch = -20.0f;
    private float maximumPitch = 70.0f;
    private float initialPitch = 20.0f;
    [Header("Camera Collision")]
    private float cameraRadius = 0.3f;
    private float wallOffset = 0.1f;
    private float distanceSmoothTime = 0.1f;

    private LayerMask obstacleLayer;
    private float yaw;
    private float pitch;
    private float currentDistance;
    private float distanceVelocity;

    private Vector3 currentLookTarget;
    private Vector3 followVelocity;
    private Character followedCharacter;

    public void CameraSetUp()
    {
        yaw = transform.eulerAngles.y;
        pitch = Mathf.Clamp(initialPitch, minimumPitch, maximumPitch);
        currentDistance = cameraDistance;
        obstacleLayer = LayerMask.GetMask("Map");
    }

    /// <summary>
    /// マウス入力からカメラの水平角と上下角を更新する。
    /// </summary>
    public void ReadCameraRotation()
    {
        if (InputManager.canMoveMousePad) return;
        Vector2 mouseDelta = InputManager.ReadMouseMoveValue();

        // マウスの移動量なので、Time.deltaTimeは掛けない。
        yaw = Mathf.Repeat(yaw + mouseDelta.x * mouseSensitivity, 360.0f);

        pitch -= mouseDelta.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);
    }
    /// <summary>
    /// 注視位置を追従させ、カメラをキャラクターの周囲に配置する。
    /// </summary>
    public void CameraFollow()
    {
        if (InputManager.canMoveMousePad) return;

        Vector3 lookTarget = _character.transform.position + Vector3.up * lookTargetHeight;

        // 操作キャラクターが変わった場合は追従位置を初期化する。
        if (followedCharacter != _character)
        {
            followedCharacter = _character;
            currentLookTarget = lookTarget;
            followVelocity = Vector3.zero;
        }

        currentLookTarget = Vector3.SmoothDamp(currentLookTarget, lookTarget, ref followVelocity, smoothTime);

        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0.0f);
        Vector3 cameraDirection = cameraRotation * Vector3.back;

        float allowedDistance = cameraDistance;

        // 注視位置からカメラ方向へ障害物を確認する。
        if (Physics.SphereCast( currentLookTarget, cameraRadius, cameraDirection,
            out RaycastHit hit, cameraDistance, obstacleLayer, QueryTriggerInteraction.Ignore))
            allowedDistance = Mathf.Max(hit.distance - wallOffset, 0.0f);

        // 壁に近づいた場合は即座に距離を縮める。

        if (currentDistance > allowedDistance)
        {
            currentDistance = allowedDistance;
            distanceVelocity = 0.0f;
        }
        // 障害物がなくなった場合は徐々に元の距離へ戻す。
        else currentDistance = Mathf.SmoothDamp(currentDistance, allowedDistance, ref distanceVelocity, distanceSmoothTime);

        transform.SetPositionAndRotation(currentLookTarget + cameraDirection * currentDistance, cameraRotation);
    }





}
