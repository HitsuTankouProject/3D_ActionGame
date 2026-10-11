using Cysharp.Threading.Tasks;
using Fusion;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;


public class Test : MonoBehaviour, ICharacterInPut
{
    public Character _character;
    public Character character => _character;

    [Header("Follow")]
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private float lookTargetHeight = 1.0f;
    [SerializeField] private float cameraDistance = 4.0f;

    [Header("Rotation")]
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float minimumPitch = -20.0f;
    [SerializeField] private float maximumPitch = 70.0f;
    [SerializeField] private float initialPitch = 20.0f;

    [Header("Camera Collision")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float cameraRadius = 0.3f;
    [SerializeField] private float wallOffset = 0.1f;
    [SerializeField] private float distanceSmoothTime = 0.1f;

    private float yaw;
    private float pitch;
    private float currentDistance;
    private float distanceVelocity;

    private Vector3 currentLookTarget;
    private Vector3 followVelocity;
    private Character followedCharacter;

    private void Start()
    {
        yaw = transform.eulerAngles.y;
        pitch = Mathf.Clamp(initialPitch, minimumPitch, maximumPitch);
        currentDistance = cameraDistance;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// マウス入力からカメラの水平角と上下角を更新する。
    /// </summary>
    private void ReadCameraRotation()
    {
        if (Mouse.current == null ||
            Cursor.lockState != CursorLockMode.Locked)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        // マウスの移動量なので、Time.deltaTimeは掛けない。
        yaw = Mathf.Repeat(
            yaw + mouseDelta.x * mouseSensitivity, 360.0f);

        pitch -= mouseDelta.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);
    }

    /// <summary>
    /// 注視位置を追従させ、カメラをキャラクターの周囲に配置する。
    /// </summary>
    private void CameraFollow()
    {
        Vector3 lookTarget =
            _character.transform.position + Vector3.up * lookTargetHeight;

        // 操作キャラクターが変わった場合は追従位置を初期化する。
        if (followedCharacter != _character)
        {
            followedCharacter = _character;
            currentLookTarget = lookTarget;
            followVelocity = Vector3.zero;
        }

        currentLookTarget = Vector3.SmoothDamp(
            currentLookTarget,
            lookTarget,
            ref followVelocity,
            smoothTime);

        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0.0f);
        Vector3 cameraDirection = cameraRotation * Vector3.back;

        float allowedDistance = cameraDistance;

        // 注視位置からカメラ方向へ障害物を確認する。
        if (Physics.SphereCast(
            currentLookTarget,
            cameraRadius,
            cameraDirection,
            out RaycastHit hit,
            cameraDistance,
            obstacleLayer,
            QueryTriggerInteraction.Ignore))
        {
            allowedDistance = Mathf.Max(hit.distance - wallOffset, 0.0f);
        }

        if (currentDistance > allowedDistance)
        {
            // 壁に近づいた場合は即座に距離を縮める。
            currentDistance = allowedDistance;
            distanceVelocity = 0.0f;
        }
        else
        {
            // 障害物がなくなった場合は徐々に元の距離へ戻す。
            currentDistance = Mathf.SmoothDamp(
                currentDistance,
                allowedDistance,
                ref distanceVelocity,
                distanceSmoothTime);
        }

        transform.SetPositionAndRotation(
            currentLookTarget + cameraDirection * currentDistance,
            cameraRotation);
    }




    private void LateUpdate()
    {
        ReadCameraRotation();
        CameraFollow();
    }

}