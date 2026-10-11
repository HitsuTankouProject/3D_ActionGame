using UnityEngine;
using Cysharp.Threading.Tasks;

public class TreasureBox : MonoBehaviour, IDamageable, IItemDrop
{
    [SerializeField] private Transform boxTop;
    private const float openTime = 0.25f;
    private readonly Vector3 openAngle = new Vector3(120, 0, 0);
    private readonly Vector3 closeAngle = Vector3.zero;

    private bool isOpened = false;
    private async UniTask OpenTheBox()
    {
        if (boxTop == null)
        {
            Debug.LogWarning("boxTop == null");
            return;
        }
        isOpened = true;
        Quaternion startRotation = Quaternion.Euler(closeAngle);
        Quaternion targetRotation = Quaternion.Euler(openAngle);

        float elapsedTime = 0.0f;

        while (elapsedTime < openTime)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsedTime / openTime);
            boxTop.localRotation = Quaternion.Slerp(startRotation, targetRotation, progress);


        }

        boxTop.localRotation = targetRotation;
        this.DropMultiWeaponIn(transform.position , 4);
    }

    public void TakeDamage(int damage)
    {
        if (isOpened) return;
        OpenTheBox().Forget();
    }

}
