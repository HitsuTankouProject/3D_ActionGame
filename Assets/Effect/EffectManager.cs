using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;



public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        ConstantEffectInit();
    }

    #region Actor Constant Effect
    [Header("Actor Constant Effect")]
    private List<VisualEffect> constantVisualEffectRunner = new();
    private Queue<VisualEffect> useableVisualEffectRunner = new();

    private void AddNewVisualEffectRunner()
    {
        GameObject effectObject = new GameObject("ConstantVisualEffect");
        effectObject.transform.parent = this.transform;
        effectObject.SetActive(false);

        VisualEffect newVisualEffect = effectObject.AddComponent<VisualEffect>();            
        constantVisualEffectRunner.Add(newVisualEffect);
        useableVisualEffectRunner.Enqueue(newVisualEffect);
    }

    private void ConstantEffectInit()
    {
        foreach(VisualEffect effect in constantVisualEffectRunner)
        {
            Destroy(effect.gameObject);
        }

        constantVisualEffectRunner.Clear();
        useableVisualEffectRunner.Clear();
        AddNewVisualEffectRunner();
    }
    private void ReturnToQueue(VisualEffect visualEffect)
    {
        if (visualEffect == null) return;
        else if (!constantVisualEffectRunner.Contains(visualEffect))
        {
            Debug.LogError("[EffectManager] Why returning a Unregistered VisualEffect ?");
            return;
        }
        else if (useableVisualEffectRunner.Contains(visualEffect))
        {
            Debug.LogWarning("[EffectManager] VisualEffect has already been returned.");
            return;
        }
        visualEffect.Stop();
        visualEffect.gameObject.SetActive(false);
        useableVisualEffectRunner.Enqueue(visualEffect);
    }
    public VisualEffect GetUseableVisualEffect()
    {
        if (useableVisualEffectRunner.Count == 0) AddNewVisualEffectRunner();
        VisualEffect visualEffect = useableVisualEffectRunner.Dequeue();
        visualEffect.gameObject.SetActive(true);

        return visualEffect;
    }


    #endregion




}
