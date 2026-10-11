using Fusion;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.Collections.Unicode;

public class ColorBall : DroppedItem
{
    public override ItemType type => ItemType.ColorBall;


    private const uint maxColorAmount = 30;
    private const uint minColorAmount = 10;

    protected override void ItemReaction(Collider other)
    {
        if (!setUpDone) return;
        if (other.TryGetComponent<IColorDamageable>(out IColorDamageable iColorDamageable))
        {
            uint colorAmount = (uint)Random.Range( (int)minColorAmount,(int)maxColorAmount + 1); 
            iColorDamageable.TryIncreaseColor(colorAmount);
            ReturnToPool();

        }
    }

}
