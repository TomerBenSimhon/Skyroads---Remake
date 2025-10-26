using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    [Header("Hierarchy")]
    [Tooltip("Parent transform for spawned emitters (auto-filled to this object).")]
    [SerializeField] private Transform emittersRoot;

    [Header("One-Shot Pool")]
    [SerializeField, Min(0)] private int oneshotPoolSize = 24;
    [SerializeField] private bool prewarmVoices = true;

    [Header("Loop Pool")]
    [SerializeField, Min(0)] private int loopPoolSize = 8;

    // active loop emitters by tag
    private readonly Dictionary<string, PooledLoopEmitter> _activeLoops = new();
    // fade-in coroutines per loop tag
    private readonly Dictionary<string, Coroutine> _loopFadeIns = new();

    // one-shot pool
    private readonly Queue<PooledAudioEmitter> _freeEmitters = new();
    private readonly List<PooledAudioEmitter>  _allEmitters  = new();

    // loop pool
    private readonly Queue<PooledLoopEmitter> _freeLoopEmitters = new();
    private readonly List<PooledLoopEmitter>  _allLoopEmitters  = new();

    // track one-shots by tag (for cancels)
    private readonly Dictionary<string, List<PooledAudioEmitter>> _activeOneShots = new();

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

        BuildOneShotPool();
        BuildLoopPool();
        if (prewarmVoices) StartCoroutine(PrewarmPoolVoices());
    }

    private void BuildOneShotPool()
    {
        for (int i = 0; i < oneshotPoolSize; i++)
        {
            var go = new GameObject($"OneShot_{i:00}");
            go.transform.SetParent(emittersRoot, false);
            var src = go.AddComponent<AudioSource>();
            var emitter = go.AddComponent<PooledAudioEmitter>();
            emitter.Init(this, src);
            _allEmitters.Add(emitter);
            _freeEmitters.Enqueue(emitter);
        }
        if (debugLogs) Debug.Log($"[AudioManager] Built one-shot pool: {oneshotPoolSize}");
    }

    private void BuildLoopPool()
    {
        for (int i = 0; i < loopPoolSize; i++)
        {
            var go = new GameObject($"Loop_{i:00}");
            go.transform.SetParent(emittersRoot, false);
            var src = go.AddComponent<AudioSource>();
            var emitter = go.AddComponent<PooledLoopEmitter>();
            emitter.Init(this, src);
            _allLoopEmitters.Add(emitter);
            _freeLoopEmitters.Enqueue(emitter);
        }
        if (debugLogs) Debug.Log($"[AudioManager] Built loop pool: {loopPoolSize}");
    }

    private System.Collections.IEnumerator PrewarmPoolVoices()
    {
        var silent = AudioClip.Create("__silent__", 64, 1, AudioSettings.outputSampleRate, false);
        // one-shots
        foreach (var e in _allEmitters)
        {
            var s = e.Source;
            s.volume = 0f; s.clip = silent; s.Play(); s.Stop();
            yield return null;
        }
        // loops
        foreach (var e in _allLoopEmitters)
        {
            var s = e.Source;
            s.volume = 0f; s.clip = silent; s.loop = true; s.Play(); s.Stop();
            yield return null;
        }
    }

    internal void ReturnEmitter(PooledAudioEmitter emitter)
    {
        foreach (var kv in _activeOneShots)
            kv.Value.Remove(emitter);
        _freeEmitters.Enqueue(emitter);
    }

    internal void ReturnLoopEmitter(PooledLoopEmitter emitter)
    {
        _freeLoopEmitters.Enqueue(emitter);
    }

    private PooledAudioEmitter RentEmitter()
    {
        if (_freeEmitters.Count > 0) return _freeEmitters.Dequeue();
        // grow if needed
        var go = new GameObject($"OneShot_Extra_{_allEmitters.Count:00}");
        go.transform.SetParent(emittersRoot, false);
        var src = go.AddComponent<AudioSource>();
        var emitter = go.AddComponent<PooledAudioEmitter>();
        emitter.Init(this, src);
        _allEmitters.Add(emitter);
        if (debugLogs) Debug.LogWarning("[AudioManager] One-shot pool grew; consider increasing oneshotPoolSize.");
        return emitter;
    }

    private PooledLoopEmitter RentLoopEmitter()
    {
        if (_freeLoopEmitters.Count > 0) return _freeLoopEmitters.Dequeue();
        // grow if needed
        var go = new GameObject($"Loop_Extra_{_allLoopEmitters.Count:00}");
        go.transform.SetParent(emittersRoot, false);
        var src = go.AddComponent<AudioSource>();
        var emitter = go.AddComponent<PooledLoopEmitter>();
        emitter.Init(this, src);
        _allLoopEmitters.Add(emitter);
        if (debugLogs) Debug.LogWarning("[AudioManager] Loop pool grew; consider increasing loopPoolSize.");
        return emitter;
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

        ResolveVolumePitch(clipAsset, volumeMul, pitchOffset, randomizePitch, randomPitchRange,
            out float vol, out float pitch);

        var emitter = RentEmitter();

        var src = emitter.Source;
        src.outputAudioMixerGroup = clipAsset.mixerGroup;
        if (clipAsset.is3D)
        {
            src.rolloffMode = clipAsset.rolloff;
            src.minDistance = Mathf.Max(0.01f, clipAsset.minDistance);
            src.maxDistance = Mathf.Max(src.minDistance + 0.01f, clipAsset.maxDistance);
        }

        emitter.transform.localPosition = Vector3.zero;
        emitter.PlayOneShot(clipAsset.clip, vol, pitch, clipAsset.is3D, followTarget);

        if (!_activeOneShots.TryGetValue(tag, out var list))
        {
            list = new List<PooledAudioEmitter>();
            _activeOneShots[tag] = list;
        }
        list.Add(emitter);

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
        AnimationCurve customFade = null   // <- add this
    )
    {
        if (!clipAsset || !clipAsset.clip)
        {
            Debug.LogWarning("[AudioManager] PlayLoop: missing SfxClip or AudioClip");
            return null;
        }

        // get or create pooled loop emitter for this tag
        if (!_activeLoops.TryGetValue(tag, out var loop) || loop == null)
        {
            loop = RentLoopEmitter();
            _activeLoops[tag] = loop;
            if (debugLogs) Debug.Log($"[AudioManager] Loop '{tag}' rented from pool");
        }

        // resolve vol/pitch
        ResolveVolumePitch(clipAsset, volumeMul, pitchOffset, randomizePitch, randomPitchRange,
            out float targetVol, out float targetPitch);

        // start loop, potentially at 0 volume for fade-in
        float startVol = (fadeInSeconds > 0f) ? 0f : targetVol;
        loop.Begin(clipAsset, followTarget, startVol, targetPitch);

        // cancel any old fade for this tag
        if (_loopFadeIns.TryGetValue(tag, out var existing) && existing != null)
        {
            StopCoroutine(existing);
            _loopFadeIns[tag] = null;
        }

        // fade to target if requested
        if (fadeInSeconds > 0f)
            _loopFadeIns[tag] = StartCoroutine(
                FadeInVolume(loop.Source, targetVol, fadeInSeconds, tag, fadeInShape, customFade) // <- pass it
            );
        else
            loop.Source.volume = targetVol;


        return loop.Source;
    }

    public void StopLoop(string tag, float fadeSeconds = 0f)
    {
        if (_activeLoops.TryGetValue(tag, out var loop) && loop)
        {
            if (_loopFadeIns.TryGetValue(tag, out var f) && f != null)
            {
                StopCoroutine(f);
                _loopFadeIns[tag] = null;
            }

            StartCoroutine(FadeThenReturnLoop(loop, fadeSeconds));
            _activeLoops.Remove(tag);
            if (debugLogs) Debug.Log($"[AudioManager] Loop '{tag}' stopping (fadeOut={fadeSeconds:0.###}s)");
        }
    }

    public void StopOneShots(string tag, float fadeSeconds = 0f)
    {
        if (_activeOneShots.TryGetValue(tag, out var list))
        {
            foreach (var e in list)
            {
                if (e && e.gameObject.activeSelf)
                {
                    var s = e.Source;
                    if (fadeSeconds > 0f && s)
                        StartCoroutine(FadeLinear(s, s.volume, 0f, fadeSeconds));
                }
            }
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

    private static float LinFromDb(float db) => Mathf.Pow(10f, db / 20f);

    private System.Collections.IEnumerator FadeInVolume(
        AudioSource src, float targetVol, float seconds, string tag, FadeShape shape, AnimationCurve custom // <- add custom
    )
    {
        if (!src) yield break;
        float start = src.volume;
        if (seconds <= 0f) { src.volume = targetVol; yield break; }

        float t = 0f;
        while (t < seconds && src)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / seconds); // 0..1
            float w = EvaluateFade(shape, u, custom);   // <- pass custom
            src.volume = Mathf.Lerp(start, targetVol, w);
            yield return null;
        }
        if (src) src.volume = targetVol;
        _loopFadeIns[tag] = null;
    }

    private static float EvaluateFade(FadeShape shape, float t, AnimationCurve custom = null)
    {
        switch (shape)
        {
            case FadeShape.Linear:       return t;
            case FadeShape.EaseInSine:   return Mathf.Sin(t * Mathf.PI * 0.5f);
            case FadeShape.Exponential:  return t * t * t;
            case FadeShape.Logarithmic:  const float a = 9f; return Mathf.Log(1f + a * t) / Mathf.Log(1f + a);
            case FadeShape.Custom:       return (custom != null) ? Mathf.Clamp01(custom.Evaluate(t)) : t;
            default:                     return t;
        }
    }



    private System.Collections.IEnumerator FadeLinear(AudioSource src, float from, float to, float seconds)
    {
        if (!src) yield break;
        if (seconds <= 0f) { src.volume = to; yield break; }

        float t = 0f;
        while (t < seconds && src)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        if (src) src.volume = to;
    }

    private System.Collections.IEnumerator FadeThenReturnLoop(PooledLoopEmitter loop, float seconds)
    {
        var src = loop?.Source;
        if (!src || !loop) yield break;

        if (seconds > 0f)
        {
            float start = src.volume;
            float t = 0f;
            while (t < seconds && src)
            {
                t += Time.deltaTime;
                src.volume = Mathf.Lerp(start, 0f, t / seconds);
                yield return null;
            }
        }

        loop.End(); // returns to pool
    }

    private System.Collections.IEnumerator StopLoopAfterCR(string tag, float seconds, float fade)
    {
        yield return new WaitForSeconds(seconds);
        StopLoop(tag, fade);
    }

    // (optional) preload utility you already use for big clips
    public Coroutine Preload(SfxClip clipAsset)
    {
        if (!clipAsset || clipAsset.clip == null) return null;
        return StartCoroutine(PreloadClipCR(clipAsset.clip));
    }

    private System.Collections.IEnumerator PreloadClipCR(AudioClip clip)
    {
        if (clip.loadState == AudioDataLoadState.Loaded) yield break;
        clip.LoadAudioData();
        while (clip.loadState == AudioDataLoadState.Loading) yield return null;
    }
}
