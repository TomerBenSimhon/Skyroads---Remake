using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public static class EffectsAudioHelpers
{
    // Create/reuse an AudioSource for this spec. Loops get a per-tag child.
    public static AudioSource GetOrCreateSource(
        this Effects self,
        SoundEffectSpec spec,
        Dictionary<string, AudioSource> loopSources)
    {
        if (spec.mode == SoundMode.Looping)
        {
            if (loopSources.TryGetValue(spec.tag, out var existing) && existing)
                return existing;
        }

        // Loops → dedicated child under Effects (follows transform for 3D).
        // One-shots → reuse/attach to root object.
        var host = (spec.mode == SoundMode.Looping)
            ? self.GetOrCreateChild($"SFX_{spec.tag}")
            : self.gameObject;

        var src = host.GetComponent<AudioSource>();
        if (!src) src = host.AddComponent<AudioSource>();
        src.playOnAwake = false;

        if (spec.mode == SoundMode.Looping)
            loopSources[spec.tag] = src;

        return src;
    }

    // Ensure/find a named child
    public static GameObject GetOrCreateChild(this Effects self, string name)
    {
        var t = self.transform.Find(name);
        if (t) return t.gameObject;
        var go = new GameObject(name);
        go.transform.SetParent(self.transform, false);
        return go;
    }

    // Apply mixer + 2D/3D + distances
    public static void ConfigureSource(this Effects self, AudioSource src, SoundEffectSpec spec)
    {
        src.outputAudioMixerGroup = spec.sfx.mixerGroup;

        bool use3D = spec.Use3D();
        src.spatialBlend = use3D ? 1f : 0f;

        if (use3D)
        {
            src.rolloffMode = spec.sfx.rolloff;
            src.minDistance = Mathf.Max(0.01f, spec.sfx.minDistance);
            src.maxDistance = Mathf.Max(src.minDistance + 0.01f, spec.sfx.maxDistance);
        }
    }

    // Stop with optional fade (runs on Effects for coroutine)
    public static void StopSource(this Effects self, AudioSource src, float fadeSeconds)
    {
        if (!src) return;
        if (fadeSeconds <= 0f) { src.Stop(); return; }
        self.StartCoroutine(FadeOutAndStop(src, fadeSeconds));
    }

    // Start/replace an auto-stop timer for a looping tag
    public static void StartAutoStop(
        this Effects self,
        string tag,
        float seconds,
        float fadeSeconds,
        Dictionary<string, AudioSource> loopSources,
        Dictionary<string, Coroutine> autoStops)
    {
        if (autoStops.TryGetValue(tag, out var routine) && routine != null)
            self.StopCoroutine(routine);

        autoStops[tag] = self.StartCoroutine(AutoStopAfter(tag, seconds, fadeSeconds, loopSources, autoStops, self));
    }

    // --- coroutines ---

    private static IEnumerator FadeOutAndStop(AudioSource src, float seconds)
    {
        float startVol = src.volume;
        float t = 0f;
        while (t < seconds && src && src.isPlaying)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(startVol, 0f, t / seconds);
            yield return null;
        }
        if (src) { src.Stop(); src.volume = startVol; }
    }

    private static IEnumerator AutoStopAfter(
        string tag,
        float seconds,
        float fadeSeconds,
        Dictionary<string, AudioSource> loopSources,
        Dictionary<string, Coroutine> autoStops,
        Effects self)
    {
        yield return new WaitForSeconds(seconds);
        if (loopSources.TryGetValue(tag, out var src) && src)
            self.StopSource(src, fadeSeconds);
        autoStops[tag] = null;
    }
}
