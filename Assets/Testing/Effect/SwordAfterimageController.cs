using UnityEngine;
using UnityEngine.VFX;

public class SwordAfterimageController : MonoBehaviour
{
    [SerializeField] private VisualEffect swordEffect;
    [SerializeField] private Transform swordMeshTransform;
    [SerializeField] private Transform swordTop;
    [SerializeField] private Transform swordBottom;

    private Vector3 lastTopPosition;
    private Vector3 lastBottomPosition;


    private static readonly int positionId = Shader.PropertyToID("SwordPosition");

    private static readonly int anglesId = Shader.PropertyToID("SwordAngles");

    private static readonly int SwordTopPosition = Shader.PropertyToID("SwordTopPosition");

    private static readonly int SwordBottomPosition = Shader.PropertyToID("SwordBottomPosition");

    //private static readonly int scaleId = Shader.PropertyToID("SwordScale");

    private void Start()
    {
        lastTopPosition = swordTop.position;
        lastBottomPosition = swordBottom.position;
    }
    private void Update()
    {
        Vector3 topPosition = swordTop.position;
        Vector3 bottomPosition = swordBottom.position;

        swordEffect.SetVector3(SwordTopPosition, topPosition);
        swordEffect.SetVector3(SwordBottomPosition, bottomPosition);


        lastTopPosition = topPosition;
        lastBottomPosition = bottomPosition;
    }

    private void LateUpdate()
    {
        if (swordEffect == null || swordMeshTransform == null)
            return;

        swordEffect.SetVector3(positionId, swordMeshTransform.position);
        swordEffect.SetVector3(anglesId, swordMeshTransform.eulerAngles);
        //swordEffect.SetVector3(scaleId, swordMeshTransform.localScale);
    }
}