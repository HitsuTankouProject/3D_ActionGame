using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.VFX;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;


public struct VFXMark
{
    public VisualEffectAsset effect;
    public GameObject effectObject;

    public VFXMark(VisualEffectAsset visualEffectAsset, GameObject gameObject)
    {
        effect = visualEffectAsset;
        effectObject = gameObject;
    }


    public override bool Equals(object obj)
    {
        if (obj is VFXMark other)
        {
            return EqualityComparer<VisualEffectAsset>.Default.Equals(effect, other.effect)
                            && EqualityComparer<GameObject>.Default.Equals(effectObject, other.effectObject);
        }
        return false;

    }
    public override int GetHashCode() => HashCode.Combine(effect, effectObject);
    public static bool operator ==(VFXMark a, VFXMark b)
        => a.effect.Equals(b.effect) && a.effectObject.Equals(b.effectObject) ;
    public static bool operator !=(VFXMark a, VFXMark b)
        => !a.effect.Equals(b.effect) || !a.effectObject.Equals(b.effectObject);

}



public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance;

    #region Constant Effect
    [Header("Constant Effect")]
    [SerializeField] private List<VisualEffect> constantEffectPlayers = new();
    private Dictionary<VFXMark, VisualEffect> allPlayingEffects = new();


    private const string playEffectPropertyName = "IsPlayEffect";
    private static readonly int playEffectPropertyId = Shader.PropertyToID(playEffectPropertyName);
    private bool HasPlayEffectTag(VisualEffectAsset effectAsset)
    {
        if (effectAsset == null) return false;
        var properties = new List<VFXExposedProperty>();
        effectAsset.GetExposedProperties(properties);
        foreach (VFXExposedProperty property in properties)
            if (property.name == playEffectPropertyName && property.type == typeof(bool)) return true;

        return false;

    }
    public void PlayConstantEffectDone(VFXMark vFXMark)
    {

    }

    public void PlayConstantEffect(VFXMark vFXMark)
    {
        if (!HasPlayEffectTag(vFXMark.effect))
        {
            Debug.LogWarning($" Target Effect did not have [IsPlayEffect] switch : {vFXMark.effectObject.name}");
            return;
        }
        if (allPlayingEffects.TryGetValue(vFXMark, out VisualEffect visualEffect))
            visualEffect.SetBool(playEffectPropertyId, false);




    }


    #endregion
}
