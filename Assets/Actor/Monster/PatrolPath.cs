using UnityEngine;

/// <summary>
/// モンスターが巡回する地点を管理するクラス。
/// 巡回地点の取得と、Scene上での巡回経路の表示を行う。
/// </summary>
public class PatrolPath : MonoBehaviour
{
    /// <summary>
    /// モンスターが巡回する地点の一覧。
    /// </summary>
    public Transform[] patrolPoints;
    /// <summary>
    /// 登録されている巡回地点の数を取得する。
    /// </summary>
    public int pointCount => patrolPoints.Length;

    /// <summary>
    /// 指定されたIndexの巡回地点の位置を取得する。
    /// </summary>
    /// <param name="index">取得する巡回地点のIndex。</param>
    /// <returns>指定された巡回地点のワールド座標。</returns>
    public Vector3 GetPoint(int index) => patrolPoints[index].position;

    /// <summary>
    /// Scene上で各巡回地点を線で接続し、
    /// モンスターの巡回経路を可視化する。
    /// </summary>
    private void OnDrawGizmos()
    {
        // 巡回経路を描画するには最低2つの地点が必要。
        if (patrolPoints == null || patrolPoints.Length < 2) return;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            // 現在の巡回地点が設定されていない場合はスキップする。
            if (patrolPoints[i] == null) continue;

            // 最後の地点の場合は最初の地点へ戻り、
            // 巡回経路が一周するようにする。
            int nextIndex = (i + 1) % patrolPoints.Length;
            // 次の巡回地点が設定されていない場合は描画しない。
            if (patrolPoints[nextIndex] == null) continue;

            // 現在の巡回地点から次の巡回地点まで線を描画する。
            Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
        }
    }
}
