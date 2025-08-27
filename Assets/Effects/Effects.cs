// Effects.cs  (camera-only for now)

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(-10)]
public class Effects : MonoBehaviour
{
    [SerializeField] private GameObject eventObject;
    [SerializeField] private string triggerName;
    [SerializeField] private string cancelName;
    
    [Header("Camera Effects")]
    [SerializeReference] 
    private List<CameraEffectSpec> cameraEffects;


    private void Update()
    {
        foreach (var eventObj in GlobalEventManager.I.TriggerEvents)
        {
            if(eventObj.Key == triggerName && eventObj.Value == eventObject)
                Play();
        }

        foreach (var eventObj in GlobalEventManager.I.CancelEvents)
        {
            if(eventObj.Key == cancelName && eventObj.Value == eventObject)
                Cancel();
        }
    }

    /// Call this from gameplay when you want to play effects on this object.
    public void Play(Transform target = null, float magnitude = 1f, Vector3? positionOverride = null)
    {
        var call = new EffectCall {
            source   = transform,
            target   = target,
            position = positionOverride ?? transform.position,
            magnitude= magnitude
        };

        if (CameraEffectsManager.I != null && cameraEffects != null)
        {
            for (int i = 0; i < cameraEffects.Count; i++)
            {
                var spec = cameraEffects[i];
                if (spec == null) continue;
                var eff = spec.Build(call);
                if (eff != null)
                {
                    bool policy = spec.replaceExisting;
                    CameraEffectsManager.I.Play(eff, policy);
                }
            }
        }
    }

    public void Cancel(Transform target = null, float magnitude = 1f, Vector3? positionOverride = null)
    {
        var call = new EffectCall {
            source   = transform,
            target   = target,
            position = positionOverride ?? transform.position,
            magnitude= magnitude
        };
        
        if (CameraEffectsManager.I != null && cameraEffects != null)
        {
            for (int i = 0; i < cameraEffects.Count; i++)
            {
                var spec = cameraEffects[i];
                if (spec == null) continue;
                var eff = spec.Build(call);
                if (eff != null)
                {
                    CameraEffectsManager.I.CancelTag(cameraEffects[i].tag);
                }
            }
        }
    }
}