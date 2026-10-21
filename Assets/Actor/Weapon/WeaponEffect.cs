using UnityEngine;
using UnityEngine.VFX;
using System.Collections.Generic;

[System.Serializable]
public abstract class WeaponEffect
{

    [SerializeField] protected List<VisualEffectAsset> allEffects;
    [SerializeField] protected bool isPlayEffect;



    public virtual void OpenEffect(VisualEffectAsset targetEffect)
    {

    }




}
