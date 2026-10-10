using Fusion;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.Collections.Unicode;

public class ColorBall : DroppedItem
{
    private const uint maxColorAmount = 30;
    private const uint minColorAmount = 10;

    protected override void ItemReaction(Collider other)
    {
        if (other.TryGetComponent<IColorDamageable>(out IColorDamageable iColorDamageable))
        {
            uint colorAmount = (uint)Random.Range( (int)minColorAmount,(int)maxColorAmount + 1); 
            iColorDamageable.TryIncreaseColor(colorAmount);
            Destroy(gameObject);

        }
    }

}
