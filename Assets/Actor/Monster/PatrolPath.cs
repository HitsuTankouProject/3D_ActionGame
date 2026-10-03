using UnityEngine;

public class PatrolPath : MonoBehaviour
{
    public Transform[] patrolPoints;
    public int pointCount => patrolPoints.Length;
    public Vector3 GetPoint(int index) => patrolPoints[index].position;

    private void OnDrawGizmos()
    {
        if (patrolPoints == null || patrolPoints.Length < 2)
            return;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null)
                continue;

            int nextIndex = (i + 1) % patrolPoints.Length;

            if (patrolPoints[nextIndex] == null)
                continue;

            Gizmos.DrawLine(
                patrolPoints[i].position,
                patrolPoints[nextIndex].position);
        }
    }
}
