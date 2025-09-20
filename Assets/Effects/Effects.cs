// Effects.cs  (camera + particles, per-spec trigger/cancel masks)

using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class Effects : MonoBehaviour
{
    [Header("Camera Effects")]
    [SerializeReference] 
    private List<CameraEffectSpec> cameraEffects = new();
    
    [SerializeField]
    private List<ParticleEffectSpec> particleEffects = new();

    void OnEnable()
    {
        GlobalEvents.Triggered += OnTriggered;
        GlobalEvents.Cancelled += OnCancelled;
    }

    void OnDisable()
    {
        GlobalEvents.Triggered -= OnTriggered;
        GlobalEvents.Cancelled -= OnCancelled;
    }

    void OnTriggered(GlobalEvents.Id id, GameObject sender)
    {
        // CAMERA
        if (CameraEffectsManager.I != null && cameraEffects != null)
        {
            var call = new EffectCall { source = transform, target = null, position = transform.position, magnitude = 1f };
            foreach (var spec in cameraEffects)
            {
                if (spec == null) continue;
                if (spec.MatchesTrigger(id, sender))
                {
                    var eff = spec.Build(call);
                    if (eff != null)
                        CameraEffectsManager.I.Play(eff, spec.replaceExisting);
                }
            }
        }

        // PARTICLES
        if (particleEffects != null)
        {
            foreach (var pspec in particleEffects)
            {
                if (pspec == null) continue;
                if (pspec.MatchesTrigger(id, sender))
                    pspec.Play();
            }
        }
    }

    void OnCancelled(GlobalEvents.Id id, GameObject sender)
    {
        // CAMERA
        if (CameraEffectsManager.I != null && cameraEffects != null)
        {
            foreach (var spec in cameraEffects)
            {
                if (spec == null) continue;
                if (spec.MatchesCancel(id, sender))
                    CameraEffectsManager.I.CancelTag(spec.tag);
            }
        }

        // PARTICLES
        if (particleEffects != null)
        {
            foreach (var pspec in particleEffects)
            {
                if (pspec == null) continue;
                if (pspec.MatchesCancel(id, sender))
                    pspec.Stop();
            }
        }
    }

}
