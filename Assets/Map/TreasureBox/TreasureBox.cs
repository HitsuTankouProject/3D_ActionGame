using UnityEngine;
using Cysharp.Threading.Tasks;

public class TreasureBox : MonoBehaviour, IDamageable, IItemDrop, ISwitcher
{
    [SerializeField] private Transform boxTop;
    [SerializeField] private float openTime = 0.25f;
    public Transform switchPart => boxTop;
    public float switchTime => openTime;

    public Vector3 openLocalRotationAngle => new Vector3(120, 0, 0);
    public Vector3 closeLocalRotationAngle => Vector3.zero;

    public Vector3 openLocalPosition => default;
    public Vector3 openLocalScale => default;
    public Vector3 closeLocalPosition => default;
    public Vector3 closeLocalScale => default;

    private bool isOpened = false;

    private void Start() => Switcher.SwitchInRotationClose(this);
    private async UniTask OpenTheBox()
    {
        if (boxTop == null)
        {
            Debug.LogWarning("boxTop == null");
            return;
        }
        isOpened = true;
        await Switcher.SwitchInRotation(this, Switcher.SwitchStage.Open);
        this.DropMultiWeaponIn(transform.position , 10);
    }

    public void TakeDamage(int damage)
    {
        if (isOpened) return;
        OpenTheBox().Forget();
    }

}
