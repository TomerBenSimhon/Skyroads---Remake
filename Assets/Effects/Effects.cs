using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// Requires: GlobalEvents.cs, ParticleEffectSpec.cs, CameraEffectSpec/CameraEffectsManager,
//           SfxClip.cs, SoundEffectSpec.cs, EffectsAudioHelpers.cs

[DefaultExecutionOrder(-10)]
public class Effects : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    // ===== Camera Effects =====
    [Header("Camera Effects")]
    [SerializeReference] private List<CameraEffectSpec> cameraEffects = new();

    // ===== Particle Effects =====
    [Header("Particle Effects")]
    [SerializeField] private List<ParticleEffectSpec> particleEffects = new();

    // ===== Sound Effects =====
    [Header("Sound Effects")]
    [SerializeField] private List<SoundEffectSpec> soundEffects = new();

    // Per-tag sources for BOTH one-shots and loops
    private readonly Dictionary<string, AudioSource> _tagSources = new();
    private readonly Dictionary<string, Coroutine>   _autoStops  = new();

    private void OnEnable()  => GlobalEvents.Raised += OnEvent;
    private void OnDisable()
    {
        GlobalEvents.Raised -= OnEvent;

        // Safety: stop & clear anything still playing under this Effects
        if (enableDebugLogs) Debug.Log($"[Effects:{name}] OnDisable: stopping all child audio sources.");
        var all = GetComponentsInChildren<AudioSource>(true);
        foreach (var src in all)
        {
            if (src && src.isPlaying) src.Stop();
        }
        _tagSources.Clear();
        _autoStops.Clear();
    }

    private void OnEvent(GlobalEvents.Id id, GameObject sender)
    {
        // =======================
        // CAMERA: cancel → trigger
        // =======================
        if (CameraEffectsManager.I != null && cameraEffects != null && cameraEffects.Count > 0)
        {
            foreach (var spec in cameraEffects)
            {
                if (spec == null) continue;
                if (spec.MatchesCancel(id, sender))
                {
                    if (enableDebugLogs) Debug.Log($"[Effects:{name}] Camera CANCEL {spec.tag} on {id}");
                    CameraEffectsManager.I.CancelTag(spec.tag);
                }
            }

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

                if (enableDebugLogs) Debug.Log($"[Effects:{name}] Camera TRIGGER {spec.tag} on {id}");
                var eff = spec.Build(call);
                if (eff != null)
                    CameraEffectsManager.I.Play(eff, spec.replaceExisting);
            }
        }

        // =========================
        // PARTICLES: cancel → trigger
        // =========================
        if (particleEffects != null && particleEffects.Count > 0)
        {
            foreach (var pspec in particleEffects)
            {
                if (pspec == null) continue;
                if (!pspec.MatchesCancel(id, sender)) continue;

                if (enableDebugLogs) Debug.Log($"[Effects:{name}] Particles CANCEL {pspec.tag} on {id}");
                pspec.Stop(id);
                if (pspec.particleObject) pspec.particleObject.SetActive(false);
            }

            foreach (var pspec in particleEffects)
            {
                if (pspec == null) continue;
                if (!pspec.MatchesTrigger(id, sender)) continue;

                if (enableDebugLogs) Debug.Log($"[Effects:{name}] Particles TRIGGER {pspec.tag} on {id}");
                if (pspec.particleObject) pspec.particleObject.SetActive(true);
                pspec.Play();
            }
        }

        // ======================
        // SOUNDS: cancel → trigger
        // ======================
        if (soundEffects != null && soundEffects.Count > 0)
        {
            // ---- CANCEL (handles loops AND long one-shots)
            foreach (var sspec in soundEffects)
            {
                if (sspec == null || sspec.sfx == null) continue;
                if (!sspec.MatchesCancel(id, sender)) continue;

                if (enableDebugLogs) Debug.Log($"[Effects:{name}] Sound CANCEL {sspec.tag} on {id}");

                // 1) per-tag source
                if (_tagSources.TryGetValue(sspec.tag, out var src) && src)
                    this.StopSource(src, sspec.fadeOutOnStopSeconds);

                // 2) sweep all children for any matching sources just in case
                this.StopBySpecSweep(sspec, sspec.fadeOutOnStopSeconds, enableDebugLogs);

                // clear any pending auto-stop
                if (_autoStops.TryGetValue(sspec.tag, out var routine) && routine != null)
                {
                    StopCoroutine(routine);
                    _autoStops[sspec.tag] = null;
                }
            }

            // ---- TRIGGER
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

                if (enableDebugLogs) Debug.Log($"[Effects:{name}] Sound TRIGGER {sspec.tag} on {id}");

                // Dedicated per-tag source (child GameObject) so cancel can always reach it
                var src = this.GetOrCreateTaggedSource(sspec, _tagSources);
                this.ConfigureSource(src, sspec); // mixer + 2D/3D + distances

                // resolve volume/pitch (optionally scale by call.magnitude)
                sspec.ResolveVolumePitch(out var vol, out var pitch, call.magnitude);
                src.volume = vol;
                src.pitch  = pitch;

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
                            _tagSources,
                            _autoStops
                        );
                    }
                }
            }
        }
    }
}
