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
        GlobalEvents.Raised += OnEvent;
    }

    void OnDisable()
    {
        GlobalEvents.Raised -= OnEvent;
    }

    void OnEvent(GlobalEvents.Id id, GameObject sender)
    {
        // CAMERA
        if (CameraEffectsManager.I != null && cameraEffects != null)
        {
            // optional: cancel first, then trigger (prevents race if both match)
            foreach (var spec in cameraEffects)
            {
                if (spec == null) continue;
                if (spec.MatchesCancel(id, sender))
                    CameraEffectsManager.I.CancelTag(spec.tag);
            }

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
            // same cancel-then-trigger order
            foreach (var pspec in particleEffects)
            {
                if (pspec == null) continue;
                if (pspec.MatchesCancel(id, sender))
                {
                    pspec.Stop(id);
                    pspec.particleObject.SetActive(false);
                }
            }

            foreach (var pspec in particleEffects)
            {
                if (pspec == null) continue;
                if (pspec.MatchesTrigger(id, sender))
                {
                    pspec.particleObject.SetActive(true);
                    pspec.Play();
                }
            }
        }
    }
}
