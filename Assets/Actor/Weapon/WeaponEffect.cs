using UnityEngine;
using UnityEngine.VFX;

[System.Serializable]
public abstract class WeaponEffect
{

    [SerializeField] protected VisualEffectAsset[] allEffects;
    [SerializeField] protected bool isPlayEffect;




    public virtual void OpenEffect(VisualEffectAsset targetEffect)
    {

    }




}
