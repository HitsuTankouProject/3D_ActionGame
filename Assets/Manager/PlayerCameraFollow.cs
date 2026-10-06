using System;
using UnityEngine;
using UnityEngine.TextCore.Text;

/// <summary>
/// プレイヤーが操作しているキャラクターを追従するカメラを管理するクラス。
/// キャラクターの追従、背後へのカメラ移動、
/// 障害物との衝突によるカメラ位置の補正を行う。
/// </summary>
public class PlayerCameraFollow : MonoBehaviour
{
    /// <summary>
    /// 現在プレイヤーが操作しているキャラクターを取得する。
    /// </summary>
    private Character _character => GameManager.Instance.gamePlayer.controlling_Character;

    [Header("Follow")]
    /// <summary>
    /// カメラが目標位置へ移動する際の補間時間。
    /// </summary>
    [SerializeField] private float smoothTime = 0.15f;
    /// <summary>
    /// カメラが注視する位置の、キャラクターからの高さ。
    /// </summary>
    [SerializeField] private float lookTargetHeight = 1.0f;

    [Header("Camera Collision")]
    /// <summary>
    /// カメラとの衝突判定を行う障害物のLayer。
    /// </summary>
    [SerializeField] private LayerMask obstacleLayer;
    /// <summary>
    /// SphereCastで使用するカメラの判定半径。
    /// </summary>
    [SerializeField] private float cameraRadius = 0.3f;
    /// <summary>
    /// 障害物との間に確保する距離。
    /// </summary>
    [SerializeField] private float wallOffset = 0.1f;

    /// <summary>
    /// キャラクターからカメラまでの基本Offset。
    /// </summary>
    private Vector3 offset;
    /// <summary>
    /// キャラクターの向きを反映した現在のCamera Offset。
    /// </summary>
    private Vector3 currentOffset;
    /// <summary>
    /// SmoothDampで使用するカメラの現在速度。
    /// </summary>
    private Vector3 followVelocity;

    /// <summary>
    /// カメラの初期Offsetを設定する。
    /// 初期カメラ位置を基準としてOffsetを保存する。
    /// </summary>
    private void Start() => offset = transform.position - Vector3.zero;


    ///// <summary>
    ///// カメラを現在操作しているキャラクターの背後へ移動し、
    ///// キャラクターの注視位置へ向ける。
    ///// </summary>
    //public void TurnCameraToCharacterBack()
    //{
    //    if (_character == null) return;
    //    // キャラクターの現在の向きを基本Offsetへ反映する。
    //    currentOffset = _character.transform.rotation * offset;
    //    // キャラクターの位置から指定された高さを注視位置とする。
    //    Vector3 lookTarget = _character.transform.position + Vector3.up * lookTargetHeight;
    //    // キャラクターの向きを反映したカメラ位置を計算する。
    //    Vector3 targetCameraPosition = _character.transform.position + currentOffset;
    //    // カメラを計算した位置へ移動する。
    //    transform.position = targetCameraPosition;
    //    // カメラをキャラクターの注視位置へ向ける。
    //    transform.rotation = Quaternion.LookRotation(lookTarget - targetCameraPosition, Vector3.up);
    //    // SmoothDampの移動速度を初期化する。
    //    followVelocity = Vector3.zero;
    //}

    /// <summary>
    /// キャラクターの移動後にカメラ位置を更新し、
    /// 障害物が存在する場合はカメラ位置を補正する。
    /// </summary>
    private void CameraFollow()
    {
        if (_character == null) return;

        // キャラクターの注視位置を計算する。
        Vector3 lookTarget = _character.transform.position + Vector3.up * lookTargetHeight;
        // 基本Offsetからカメラの目標位置を計算する。
        Vector3 targetPosition = _character.transform.position + offset;
        // 注視位置からカメラ目標位置への方向を取得する。
        Vector3 direction = targetPosition - lookTarget;
        // SphereCastで確認する最大距離を取得する。
        float targetDistance = direction.magnitude;

        direction.Normalize();
        // 注視位置からカメラ方向へSphereCastを行い、
        // カメラとキャラクターの間に障害物があるか確認する。
        if (Physics.SphereCast(lookTarget, cameraRadius, direction, out RaycastHit hit,
                targetDistance, obstacleLayer, QueryTriggerInteraction.Ignore))
        {
            // 障害物がある場合は、その手前までカメラを移動する。
            targetPosition = lookTarget + direction * Mathf.Max(hit.distance - wallOffset, 0.0f);
        }

        // 急激に位置が変化しないよう、
        // SmoothDampを使用して目標位置へ追従する。
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref followVelocity, smoothTime);
    }



    private void LateUpdate()
    {
        CameraFollow();
    }
}
