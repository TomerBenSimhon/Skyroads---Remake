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

    /// <summary>
    /// Play a one-shot. Spawns a new AudioSource child so it won't cut off.
    /// Returns the AudioSource for optional external control.
    /// </summary>
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

        // follow if 3D and target provided; else it's fine under manager
        if (clipAsset.is3D && followTarget)
        {
            var follower = go.AddComponent<AudioFollower>();
            follower.Init(followTarget);
        }

        // volume/pitch
        ResolveVolumePitch(clipAsset, volumeMul, pitchOffset, randomizePitch, randomPitchRange,
            out float vol, out float pitch);

        src.volume = vol;
        src.pitch  = pitch;
        src.loop   = false;

        // play & auto cleanup
        src.PlayOneShot(clipAsset.clip);
        var lifetime = clipAsset.clip.length / Mathf.Max(0.01f, Mathf.Abs(src.pitch)) + 0.05f;
        Destroy(go, lifetime);

        // track by tag for cancels (optional)
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

    /// <summary>
    /// Start or refresh a looping emitter for the tag.
    /// </summary>
    public AudioSource PlayLoop(
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

            // follow if 3D and target provided
            if (clipAsset.is3D && followTarget)
            {
                var follower = go.AddComponent<AudioFollower>();
                follower.Init(followTarget);
            }

            src.loop = true;
            src.clip = clipAsset.clip;
            src.Play();

            if (debugLogs) Debug.Log($"[AudioManager] Loop '{tag}' created & started");
        }
        else
        {
            // Ensure mixer & spatial config stays in sync with the asset
            ConfigureSourceFromAsset(src, clipAsset);
            if (src.clip != clipAsset.clip) src.clip = clipAsset.clip;
            if (!src.isPlaying) src.Play();

            if (debugLogs) Debug.Log($"[AudioManager] Loop '{tag}' refreshed");
        }

        // apply current volume/pitch
        ResolveVolumePitch(clipAsset, volumeMul, pitchOffset, randomizePitch, randomPitchRange,
            out float vol, out float pitch);
        src.volume = vol;
        src.pitch  = pitch;

        return src;
    }

    /// <summary>
    /// Stop and remove a loop by tag (optional fade).
    /// </summary>
    public void StopLoop(string tag, float fadeSeconds = 0f)
    {
        if (_activeLoops.TryGetValue(tag, out var src) && src)
        {
            StartCoroutine(FadeThenStopAndDestroy(src, fadeSeconds));
            _activeLoops.Remove(tag);
            if (debugLogs) Debug.Log($"[AudioManager] Loop '{tag}' stopping");
        }
    }

    /// <summary>
    /// Stop all one-shots under this tag (optional fade).
    /// </summary>
    public void StopOneShots(string tag, float fadeSeconds = 0f)
    {
        if (_activeOneShots.TryGetValue(tag, out var list))
        {
            foreach (var src in list)
            {
                if (src) StartCoroutine(FadeThenStopAndDestroy(src, fadeSeconds));
            }
            list.Clear();
            if (debugLogs) Debug.Log($"[AudioManager] OneShots '{tag}' stopping");
        }
    }

    /// <summary>
    /// Stop everything for this tag: loop + one-shots (optional fade).
    /// </summary>
    public void StopAllForTag(string tag, float fadeSeconds = 0f)
    {
        StopLoop(tag, fadeSeconds);
        StopOneShots(tag, fadeSeconds);
    }

    /// <summary>
    /// Optional timed auto-stop for loops (e.g., loop while boosting).
    /// </summary>
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

    // cleanup for one-shots tracked under tags
    private System.Collections.IEnumerator CleanupWhenStopped(string tag, AudioSource src)
    {
        // wait until it stops or object is destroyed
        while (src && src.isPlaying) yield return null;

        if (_activeOneShots.TryGetValue(tag, out var list))
        {
            list.Remove(src);
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
                t += Time.unscaledDeltaTime; // unaffected by timescale
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
