using Cysharp.Threading.Tasks;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class Gate : MonoBehaviour
{
    public enum GateStage { Close, Open }
    [SerializeField] private GateStage stage;
    [SerializeField] private float moveTime = 1.0f;
    [SerializeField] private float openHeight = 2.25f;  
    [SerializeField] private float closeHeight;

    private Vector3 TargetGatePosition()
    {
        Vector3 result = transform.localPosition;
        result.y = stage == GateStage.Open ? openHeight : closeHeight;
        return result;
    }
    private async UniTask GateAction(GateStage gateStage)
    {
        stage = gateStage;
        Vector3 start = transform.localPosition;
        Vector3 final = TargetGatePosition();
        float timer = 0;

        while (moveTime > timer)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / moveTime);
            transform.localPosition = Vector3.Lerp(start, final, progress);

            await UniTask.Yield(PlayerLoopTiming.Update);

        }

        transform.localPosition = final;

    }

    public void OpenGate() => GateAction(GateStage.Open).Forget();
    public void CloseGate() => GateAction(GateStage.Close).Forget();

    private void Start()
    {
        CloseGate();
    }

    public bool open;
    public bool close;
    private void Update()
    {
        
        if (open)
        {
            open = false;
            OpenGate();
        }

        if (close)
        {
            close = false;
            CloseGate();
        }
    }


}
