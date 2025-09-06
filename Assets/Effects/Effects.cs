// Effects.cs  (camera-only for now)

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DefaultExecutionOrder(-10)]
public class Effects : MonoBehaviour
{
    [SerializeField] private GameObject eventObject;
    [SerializeField] private GlobalEvents.Id triggerId;
    [SerializeField] private GlobalEvents.Id cancelId;
    
    [Header("Camera Effects")]
    [SerializeReference] 
    private List<CameraEffectSpec> cameraEffects;

    [Header("Particle Effects")]
    [SerializeField] private List<ParticleEffectSpec> particleEffects = new();

    private void OnEnable()
    {
        GlobalEvents.Triggered += OnTriggered;
        GlobalEvents.Cancelled += OnCancelled;
    }

    private void OnDisable()
    {
        GlobalEvents.Triggered -= OnTriggered;
        GlobalEvents.Cancelled -= OnCancelled;
    }

    private void OnTriggered(GlobalEvents.Id id, GameObject sender)
    {
        if (id == triggerId && (eventObject == null || sender == eventObject))
            Play();
    }

    private void OnCancelled(GlobalEvents.Id id, GameObject sender)
    {
        if (id == cancelId && (eventObject == null || sender == eventObject))
            Cancel();
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
        
        if (particleEffects != null && particleEffects.Count > 0 && ParticleEffectsManager.I != null)
        {
            var call1 = new EffectCall  // however you build it today
            {
                source   = transform,
                target   = target,
                position = positionOverride ?? transform.position,
                magnitude= magnitude
            };

            foreach (var ps in particleEffects)
                ParticleEffectsManager.I.Play(ps, call1);
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
        
        if (particleEffects != null && particleEffects.Count > 0 && ParticleEffectsManager.I != null)
        {
            var call1 = new EffectCall { 
                source   = transform,
                target   = target,
                position = positionOverride ?? transform.position,
                magnitude= magnitude 
            };
            foreach (var ps in particleEffects)
                ParticleEffectsManager.I.Cancel(ps, call1);
        }
    }
}