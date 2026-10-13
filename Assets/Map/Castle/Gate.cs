using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;

public class Gate : MonoBehaviour, ISwitcher
{
    private InGame _inGame => InGame.Instance;

    public Transform switchPart => this.transform;
    [SerializeField] private float moveTime = 1.0f;
    public float switchTime => moveTime;

    public Vector3 openLocalPosition => new Vector3(transform.localPosition.x, 2.25f, transform.localPosition.z);
    public Vector3 openLocalRotationAngle => default;
    public Vector3 openLocalScale => default;


    public Vector3 closeLocalPosition => new Vector3(transform.localPosition.x, -0.2f, transform.localPosition.z);
    public Vector3 closeLocalRotationAngle => default;
    public Vector3 closeLocalScale => default;

    [SerializeField] private List<Monster> monsters;

    public void OpenGate() => Switcher.SwitchInPositionOpen(this);
    public void CloseGate() => Switcher.SwitchInPositionClose(this);

    private void Start() => OpenGate();
    private bool isOpen = false;
    public void LateUpdateGateStage()
    {
        if (isOpen) return;
        if (!_inGame.IsGamePlaying()) return;
        monsters.RemoveAll(monster => monster == null);
        if (monsters.Count <= 0)
        {
            isOpen = true;
            OpenGate();
        }
    }

}
