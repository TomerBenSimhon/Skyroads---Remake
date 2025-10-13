using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    [Header("Defaults")]
    [Tooltip("Parent transform for spawned emitters (auto-filled to this object).")]
    [SerializeField] private Transform emittersRoot;

    // Active loop emitters by tag (usually 1 per tag)
    private readonly Dictionary<string, AudioSource> _activeLoops = new();

    // Active fade-in coroutines per loop tag (to avoid races)
    private readonly Dictionary<string, Coroutine> _loopFadeIns = new();

    // One-shot bookkeeping (optional, for sweeping/cancelling by tag)
    private readonly Dictionary<string, List<AudioSource>> _activeOneShots = new();

    private void Awake()
    {
        if (I != null && I != this)
        {
            if (debugLogs) Debug.Log($"[AudioManager] Duplicate, destroying {name}");
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
        if (!emittersRoot) emittersRoot = transform;
    }

    // ---------- Public API ----------

    public AudioSource PlayOneShot(
        string tag,
        SfxClip clipAsset,
        Transform followTarget = null,
        float volumeMul = 1f,
        float pitchOffset = 0f,
        bool randomizePitch = false,
        float randomPitchRange = 0f)
    {
        if (!clipAsset || !clipAsset.clip)
        {
            Debug.LogWarning("[AudioManager] PlayOneShot: missing SfxClip or AudioClip");
            return null;
        }

        var go = new GameObject($"SFX_{tag}_{Guid.NewGuid():N}");
        go.transform.SetParent(emittersRoot, worldPositionStays: false);

        var src = go.AddComponent<AudioSource>();
        ConfigureSourceFromAsset(src, clipAsset);

        if (clipAsset.is3D && followTarget)
        {
            var follower = go.AddComponent<AudioFollower>();
            follower.Init(followTarget);
        }

        ResolveVolumePitch(clipAsset, volumeMul, pitchOffset, randomizePitch, randomPitchRange,
            out float vol, out float pitch);

        src.volume = vol;
        src.pitch  = pitch;
        src.loop   = false;

        src.PlayOneShot(clipAsset.clip);
        var lifetime = clipAsset.clip.length / Mathf.Max(0.01f, Mathf.Abs(src.pitch)) + 0.05f;
        Destroy(go, lifetime);

        if (!_activeOneShots.TryGetValue(tag, out var list))
        {
            list = new List<AudioSource>();
            _activeOneShots[tag] = list;
        }
        list.Add(src);
        StartCoroutine(CleanupWhenStopped(tag, src));

        if (debugLogs) Debug.Log($"[AudioManager] OneShot '{tag}' started");

        return src;
    }

    public AudioSource PlayLoop(
        string tag,
        SfxClip clipAsset,
        Transform followTarget = null,
        float volumeMul = 1f,
        float pitchOffset = 0f,
        bool randomizePitch = false,
        float randomPitchRange = 0f,
        float fadeInSeconds = 0f,
        FadeShape fadeInShape = FadeShape.Linear,
        AnimationCurve customFade = null
    )
    {
        if (!clipAsset || !clipAsset.clip)
        {
            Debug.LogWarning("[AudioManager] PlayLoop: missing SfxClip or AudioClip");
            return null;
        }

        if (!_activeLoops.TryGetValue(tag, out var src) || !src)
        {
            var go = new GameObject($"SFX_{tag}_Loop");
            go.transform.SetParent(emittersRoot, worldPositionStays: false);

            src = go.AddComponent<AudioSource>();
            _activeLoops[tag] = src;

            ConfigureSourceFromAsset(src, clipAsset);

            if (clipAsset.is3D && followTarget)
            {
                var follower = go.AddComponent<AudioFollower>();
                follower.Init(followTarget);
            }

            src.loop = true;
            src.clip = clipAsset.clip;
            src.volume = (fadeInSeconds > 0f) ? 0f : src.volume; // ensure start at 0 if fading
            src.Play();

            if (debugLogs) Debug.Log($"[AudioManager] Loop '{tag}' created & started (fadeIn={fadeInSeconds:0.###}s, shape={fadeInShape})");
        }
        else
        {
            ConfigureSourceFromAsset(src, clipAsset);
            if (src.clip != clipAsset.clip) src.clip = clipAsset.clip;
            if (!src.isPlaying) src.Play();

            if (fadeInSeconds > 0f)
                src.volume = 0f; // reset to 0 for perceptual ramp on refresh

            if (debugLogs) Debug.Log($"[AudioManager] Loop '{tag}' refreshed (fadeIn={fadeInSeconds:0.###}s, shape={fadeInShape})");
        }

        ResolveVolumePitch(clipAsset, volumeMul, pitchOffset, randomizePitch, randomPitchRange,
            out float targetVol, out float targetPitch);
        src.pitch = targetPitch;

        if (_loopFadeIns.TryGetValue(tag, out var existingFade) && existingFade != null)
        {
            StopCoroutine(existingFade);
            _loopFadeIns[tag] = null;
        }

        if (fadeInSeconds > 0f)
        {
            _loopFadeIns[tag] = StartCoroutine(FadeInVolume(src, targetVol, fadeInSeconds, tag, fadeInShape, customFade));
        }
        else
        {
            src.volume = targetVol;
        }

        return src;
    }

    public void StopLoop(string tag, float fadeSeconds = 0f)
    {
        if (_activeLoops.TryGetValue(tag, out var src) && src)
        {
            if (_loopFadeIns.TryGetValue(tag, out var f) && f != null)
            {
                StopCoroutine(f);
                _loopFadeIns[tag] = null;
            }

            StartCoroutine(FadeThenStopAndDestroy(src, fadeSeconds));
            _activeLoops.Remove(tag);
            if (debugLogs) Debug.Log($"[AudioManager] Loop '{tag}' stopping (fadeOut={fadeSeconds:0.###}s)");
        }
    }

    public void StopOneShots(string tag, float fadeSeconds = 0f)
    {
        if (_activeOneShots.TryGetValue(tag, out var list))
        {
            foreach (var src in list)
                if (src) StartCoroutine(FadeThenStopAndDestroy(src, fadeSeconds));
            list.Clear();
            if (debugLogs) Debug.Log($"[AudioManager] OneShots '{tag}' stopping (fadeOut={fadeSeconds:0.###}s)");
        }
    }

    public void StopAllForTag(string tag, float fadeSeconds = 0f)
    {
        StopLoop(tag, fadeSeconds);
        StopOneShots(tag, fadeSeconds);
    }

    public void StopLoopAfter(string tag, float seconds, float fadeSeconds = 0f)
    {
        StartCoroutine(StopLoopAfterCR(tag, seconds, fadeSeconds));
    }

    // ---------- Internals ----------

    private void ConfigureSourceFromAsset(AudioSource src, SfxClip asset)
    {
        src.playOnAwake = false;
        src.outputAudioMixerGroup = asset.mixerGroup;

        if (asset.is3D)
        {
            src.spatialBlend = 1f;
            src.rolloffMode  = asset.rolloff;
            src.minDistance  = Mathf.Max(0.01f, asset.minDistance);
            src.maxDistance  = Mathf.Max(src.minDistance + 0.01f, asset.maxDistance);
        }
        else
        {
            src.spatialBlend = 0f;
        }
    }

    private void ResolveVolumePitch(
        SfxClip asset,
        float volumeMul,
        float pitchOffset,
        bool randomizePitch,
        float randomPitchRange,
        out float vol,
        out float pitch)
    {
        float p = asset.pitch + pitchOffset;
        if (randomizePitch && randomPitchRange > 0f)
            p += UnityEngine.Random.Range(-randomPitchRange, randomPitchRange);

        vol = Mathf.Clamp01(asset.volume * volumeMul);
        pitch = p;
    }

    private System.Collections.IEnumerator CleanupWhenStopped(string tag, AudioSource src)
    {
        while (src && src.isPlaying) yield return null;
        if (_activeOneShots.TryGetValue(tag, out var list)) list.Remove(src);
    }

    private System.Collections.IEnumerator FadeInVolume(AudioSource src, float targetVol, float seconds, string tag, FadeShape shape, AnimationCurve custom)
    {
        if (!src) yield break;
        float start = src.volume; // should be 0 when we set up, but safe for refresh
        if (seconds <= 0f) { src.volume = targetVol; yield break; }

        float t = 0f;
        while (t < seconds && src)
        {
            t += Time.unscaledDeltaTime;  // unaffected by Time.timeScale
            float u = Mathf.Clamp01(t / seconds);          // 0..1
            float w = EvaluateFade(shape, custom, u);      // curve weight 0..1
            src.volume = Mathf.Lerp(start, targetVol, w);  // perceptual ramp
            yield return null;
        }
        if (src) src.volume = targetVol;

        _loopFadeIns[tag] = null;
    }

    // Perceptual fade mapping
    private static float EvaluateFade(FadeShape shape, AnimationCurve custom, float t)
    {
        switch (shape)
        {
            case FadeShape.Linear:
                return t;
            case FadeShape.EaseInSine:
                // equal-power-ish start: slow at first, faster later
                return Mathf.Sin(t * Mathf.PI * 0.5f);
            case FadeShape.Exponential:
                // stronger ease-in: t^3 (adjust exponent as desired)
                return t * t * t;
            case FadeShape.Logarithmic:
                // very slow start using log; a=9 -> map 0..1 to 0..1
                const float a = 9f;
                return Mathf.Log(1f + a * t) / Mathf.Log(1f + a);
            case FadeShape.Custom:
                if (custom != null) return Mathf.Clamp01(custom.Evaluate(t));
                return t;
            default:
                return t;
        }
    }

    private System.Collections.IEnumerator FadeThenStopAndDestroy(AudioSource src, float seconds)
    {
        if (!src) yield break;

        if (seconds > 0f)
        {
            float start = src.volume;
            float t = 0f;
            while (t < seconds && src)
            {
                t += Time.unscaledDeltaTime;
                // leave fade-out as linear; perceptually this feels fine in most cases
                src.volume = Mathf.Lerp(start, 0f, t / seconds);
                yield return null;
            }
        }

        if (src)
        {
            src.Stop();
            Destroy(src.gameObject);
        }
    }

    private System.Collections.IEnumerator StopLoopAfterCR(string tag, float seconds, float fade)
    {
        yield return new WaitForSeconds(seconds);
        StopLoop(tag, fade);
    }
}
