using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class Effects : MonoBehaviour
{
    [Header("Camera Effects")]
    [SerializeReference] private List<CameraEffectSpec> cameraEffects = new();

    [Header("Particle Effects")]
    [SerializeField] private List<ParticleEffectSpec> particleEffects = new();

    [Header("Sound Effects")]
    [SerializeField] private List<SoundEffectSpec> soundEffects = new();

    private void OnEnable()  => GlobalEvents.Raised += OnEvent;
    private void OnDisable() => GlobalEvents.Raised -= OnEvent;

    private void OnEvent(GlobalEvents.Id id, GameObject sender)
    {
        // CAMERA (unchanged: cancel → trigger)
        if (CameraEffectsManager.I != null && cameraEffects != null)
        {
            foreach (var spec in cameraEffects)
                if (spec != null && spec.MatchesCancel(id, sender))
                    CameraEffectsManager.I.CancelTag(spec.tag);

            var call = new EffectCall { source = transform, target = null, position = transform.position, magnitude = 1f };

            foreach (var spec in cameraEffects)
            {
                if (spec == null || !spec.MatchesTrigger(id, sender)) continue;
                var eff = spec.Build(call);
                if (eff != null) CameraEffectsManager.I.Play(eff, spec.replaceExisting);
            }
        }

        // PARTICLES (unchanged: cancel → trigger)
        if (particleEffects != null)
        {
            foreach (var pspec in particleEffects)
                if (pspec != null && pspec.MatchesCancel(id, sender))
                {
                    pspec.Stop(id);
                    if (pspec.particleObject) pspec.particleObject.SetActive(false);
                }

            foreach (var pspec in particleEffects)
                if (pspec != null && pspec.MatchesTrigger(id, sender))
                {
                    if (pspec.particleObject) pspec.particleObject.SetActive(true);
                    pspec.Play();
                }
        }

        // SOUNDS (now via AudioManager: cancel → trigger)
        if (soundEffects != null)
        {
            foreach (var sspec in soundEffects)
                if (sspec != null && sspec.MatchesCancel(id, sender))
                    sspec.Cancel();

            foreach (var sspec in soundEffects)
                if (sspec != null && sspec.MatchesTrigger(id, sender))
                    sspec.Trigger(transform /* follow this root for 3D */);
        }
    }
}
