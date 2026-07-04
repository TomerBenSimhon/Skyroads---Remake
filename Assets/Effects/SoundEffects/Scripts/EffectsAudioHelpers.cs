using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public static class EffectsAudioHelpers
{
    // Keep a dedicated per-tag AudioSource so cancel can stop both loops and one-shots.
    public static AudioSource GetOrCreateTaggedSource(
        this Effects self,
        SoundEffectSpec spec,
        Dictionary<string, AudioSource> tagSources)
    {
        if (tagSources.TryGetValue(spec.tag, out var existing) && existing)
            return existing;

        var host = self.GetOrCreateChild($"SFX_{spec.tag}"); // child follows root transform (3D)
        var src  = host.GetComponent<AudioSource>();
        if (!src) src = host.AddComponent<AudioSource>();
        src.playOnAwake = false;

        tagSources[spec.tag] = src;
        return src;
    }

    public static GameObject GetOrCreateChild(this Effects self, string name)
    {
        var t = self.transform.Find(name);
        if (t) return t.gameObject;
        var go = new GameObject(name);
        go.transform.SetParent(self.transform, false);
        return go;
    }

    public static void ConfigureSource(this Effects self, AudioSource src, SoundEffectSpec spec)
    {
        // mixer
        src.outputAudioMixerGroup = spec.sfx.mixerGroup;

        // 2D/3D
        bool is3D = spec.sfx.is3D;
        src.spatialBlend = is3D ? 1f : 0f;

        if (is3D)
        {
            src.rolloffMode = spec.sfx.rolloff;
            src.minDistance = Mathf.Max(0.01f, spec.sfx.minDistance);
            src.maxDistance = Mathf.Max(src.minDistance + 0.01f, spec.sfx.maxDistance);
        }
    }

    public static void StopSource(this Effects self, AudioSource src, float fadeSeconds)
    {
        if (!src) return;
        if (fadeSeconds <= 0f) { src.Stop(); return; }
        self.StartCoroutine(FadeOutAndStop(src, fadeSeconds));
    }

    // Extra-robust cancel: sweep all child AudioSources and stop anything that matches the spec
    // by tag-host name OR by clip identity.
    public static void StopBySpecSweep(this Effects self, SoundEffectSpec spec, float fadeSeconds, bool debugLog = false)
    {
        var children = self.GetComponentsInChildren<AudioSource>(true);
        foreach (var src in children)
        {
            if (!src || !src.isPlaying) continue;

            bool nameMatches = src.gameObject.name == $"SFX_{spec.tag}" || src.gameObject.name.StartsWith($"SFX_{spec.tag}");
            bool clipMatches = (spec.sfx.clip != null && src.clip == spec.sfx.clip);

            if (nameMatches || clipMatches)
            {
                if (debugLog) Debug.Log($"[Effects:{self.name}] Sweep stop: {src.gameObject.name} " +
                                        $"(nameMatch={nameMatches}, clipMatch={clipMatches})");
                self.StopSource(src, fadeSeconds);
            }
        }
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

    public static void StartAutoStop(
        this Effects self,
        string tag,
        float seconds,
        float fadeSeconds,
        Dictionary<string, AudioSource> tagSources,
        Dictionary<string, Coroutine> autoStops)
    {
        if (autoStops.TryGetValue(tag, out var routine) && routine != null)
            self.StopCoroutine(routine);

        autoStops[tag] = self.StartCoroutine(AutoStopAfter(tag, seconds, fadeSeconds, tagSources, autoStops, self));
    }

    private static IEnumerator AutoStopAfter(
        string tag,
        float seconds,
        float fadeSeconds,
        Dictionary<string, AudioSource> tagSources,
        Dictionary<string, Coroutine> autoStops,
        Effects self)
    {
        yield return new WaitForSeconds(seconds);
        if (tagSources.TryGetValue(tag, out var src) && src)
            self.StopSource(src, fadeSeconds);
        autoStops[tag] = null;
    }
}
