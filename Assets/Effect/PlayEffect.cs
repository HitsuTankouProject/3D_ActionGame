using UnityEngine;
using System.Collections.Generic;
using UnityEngine.VFX;


public abstract class PlayEffect : MonoBehaviour
{
    protected EffectManager _effectManager => EffectManager.Instance;
    protected static readonly int effectPlayBool = Shader.PropertyToID("IsPlayEffect");
    protected VisualEffect usingVisualEffect;

    protected virtual void OpenEffect(VisualEffectAsset targetAsset)
    {
        if (usingVisualEffect == null) usingVisualEffect = _effectManager.GetUseableVisualEffect();
        usingVisualEffect.visualEffectAsset = targetAsset;
        usingVisualEffect.SetBool(effectPlayBool, true);
    }

}
