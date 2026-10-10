using UnityEngine;
using UnityEngine.VFX;

public class SwordEffect : MonoBehaviour
{
    [SerializeField] private VisualEffect swordVFX;
    [SerializeField] private Transform swordTop;
    [SerializeField] private Transform swordBottom;

    [SerializeField] private bool playEffet;

    private Vector3 lastTopPosition;
    private Vector3 lastBottomPosition;

    private static readonly int SwordTopPosition =
        Shader.PropertyToID("SwordTopPosition");

    private static readonly int SwordBottomPosition =
        Shader.PropertyToID("SwordBottomPosition");

    private static readonly int IsMoving =
        Shader.PropertyToID("IsMoving");

    private void Start()
    {
        lastTopPosition = swordTop.position;
        lastBottomPosition = swordBottom.position;
    }

    private void Update()
    {
        if (!playEffet)
        {
            swordVFX.SetBool(IsMoving, playEffet);

            return;
        }
        Vector3 topPosition = swordTop.position;
        Vector3 bottomPosition = swordBottom.position;

        swordVFX.SetVector3(SwordTopPosition, topPosition);
        swordVFX.SetVector3(SwordBottomPosition, bottomPosition);

        swordVFX.SetBool(IsMoving, playEffet);

        lastTopPosition = topPosition;
        lastBottomPosition = bottomPosition;
    }
}