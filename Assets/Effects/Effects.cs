using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// NOTE:
// - Requires: GlobalEvents.cs (with GlobalEvents.Raised event),
//             ParticleEffectSpec.cs,
//             CameraEffectSpec + CameraEffectsManager (your existing camera system),
//             SfxClip.cs, SoundEffectSpec.cs,
//             EffectsAudioHelpers.cs (extension helpers for audio).
//
// Flow per event: CANCEL first → TRIGGER after (for camera, particles, sounds).

[DefaultExecutionOrder(-10)]
public class Effects : MonoBehaviour
{
    // ===== Camera Effects (unchanged pattern) =====
    [Header("Camera Effects")]
    [SerializeReference] private List<CameraEffectSpec> cameraEffects = new();

    // ===== Particle Effects (existing) =====
    [SerializeField] private List<ParticleEffectSpec> particleEffects = new();

    // ===== Sound Effects (new) =====
    [SerializeField] private List<SoundEffectSpec> soundEffects = new();

    // Runtime holders for looped SFX
    private readonly Dictionary<string, AudioSource> _loopSources = new();
    private readonly Dictionary<string, Coroutine> _autoStops = new();

    private void OnEnable()
    {
        GlobalEvents.Raised += OnEvent;
    }

    private void OnDisable()
    {
        GlobalEvents.Raised -= OnEvent;
    }

    private void OnEvent(GlobalEvents.Id id, GameObject sender)
    {
        // =======================
        // CAMERA: cancel → trigger
        // =======================
        if (CameraEffectsManager.I != null && cameraEffects != null && cameraEffects.Count > 0)
        {
            // cancel
            foreach (var spec in cameraEffects)
            {
                if (spec == null) continue;
                if (spec.MatchesCancel(id, sender))
                    CameraEffectsManager.I.CancelTag(spec.tag);
            }

            // trigger
            var call = new EffectCall
            {
                source    = transform,
                target    = null,
                position  = transform.position,
                magnitude = 1f
            };

            foreach (var spec in cameraEffects)
            {
                if (spec == null) continue;
                if (!spec.MatchesTrigger(id, sender)) continue;

                var eff = spec.Build(call); // your existing API
                if (eff != null)
                    CameraEffectsManager.I.Play(eff, spec.replaceExisting);
            }
        }

        // =========================
        // PARTICLES: cancel → trigger
        // =========================
        if (particleEffects != null && particleEffects.Count > 0)
        {
            // cancel
            foreach (var pspec in particleEffects)
            {
                if (pspec == null) continue;
                if (!pspec.MatchesCancel(id, sender)) continue;

                pspec.Stop(id);
                if (pspec.particleObject) pspec.particleObject.SetActive(false);
            }

            // trigger
            foreach (var pspec in particleEffects)
            {
                if (pspec == null) continue;
                if (!pspec.MatchesTrigger(id, sender)) continue;

                if (pspec.particleObject) pspec.particleObject.SetActive(true);
                pspec.Play();
            }
        }

        // ======================
        // SOUNDS: cancel → trigger
        // ======================
        if (soundEffects != null && soundEffects.Count > 0)
        {
            // ---- cancel loops first
            foreach (var sspec in soundEffects)
            {
                if (sspec == null || sspec.sfx == null) continue;
                if (!sspec.MatchesCancel(id, sender)) continue;

                if (_loopSources.TryGetValue(sspec.tag, out var src) && src)
                {
                    // extension method from EffectsAudioHelpers
                    this.StopSource(src, sspec.fadeOutOnStopSeconds);
                }
                // clear any pending auto-stop
                if (_autoStops.TryGetValue(sspec.tag, out var routine) && routine != null)
                {
                    StopCoroutine(routine);
                    _autoStops[sspec.tag] = null;
                }
            }

            // ---- then triggers
            var call = new EffectCall
            {
                source    = transform,
                target    = null,
                position  = transform.position,
                magnitude = 1f
            };

            foreach (var sspec in soundEffects)
            {
                if (sspec == null || sspec.sfx == null) continue;
                if (!sspec.MatchesTrigger(id, sender)) continue;

                // create/reuse a source (loops get a dedicated child, one-shots reuse root)
                var src = this.GetOrCreateSource(sspec, _loopSources);
                this.ConfigureSource(src, sspec); // mixer + 2D/3D + distances

                // resolve final volume/pitch (optionally scale by call.magnitude)
                sspec.ResolveVolumePitch(out var vol, out var pitch, call.magnitude);
                src.volume = vol;
                src.pitch = pitch;

                if (sspec.mode == SoundMode.OneShot)
                {
                    src.loop = false;
                    src.PlayOneShot(sspec.sfx.clip);
                }
                else // Looping
                {
                    src.loop = true;
                    if (src.clip != sspec.sfx.clip)
                        src.clip = sspec.sfx.clip;

                    if (!src.isPlaying) src.Play();

                    if (sspec.loopDurationSeconds > 0f)
                    {
                        this.StartAutoStop(
                            sspec.tag,
                            sspec.loopDurationSeconds,
                            sspec.fadeOutOnStopSeconds,
                            _loopSources,
                            _autoStops
                        );
                    }
                }
            }
        }
    }
}
