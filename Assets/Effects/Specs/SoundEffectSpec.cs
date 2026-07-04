using System;
using UnityEngine;

public enum SoundMode { OneShot, Looping }

// Perceptual fade shapes for volume (amplitude) ramps
public enum FadeShape
{
    Linear,         // w = t
    EaseInSine,     // w = sin(t * PI/2)
    Exponential,    // w = t^k  (k>1, we use k=3)
    Logarithmic,    // w = log(1 + a*t)/log(1+a) (a=9)
    Custom          // use provided AnimationCurve (0..1 -> 0..1)
}

[Serializable]
public class SoundEffectSpec
{
    [Header("Tag & Asset")]
    public string tag = "SFX/Generic";
    public SfxClip sfx;

    [Header("Events")]
    public GlobalEvents.Id triggerMask = GlobalEvents.Id.None;
    public GlobalEvents.Id cancelMask  = GlobalEvents.Id.None;
    [Tooltip("If set, only respond to events from this sender.")]
    public GameObject eventObject;

    [Header("Behavior")]
    public SoundMode mode = SoundMode.OneShot;
    [Range(0f,1f)] public float volumeMultiplier = 1f;
    public float pitchOffset = 0f;
    public bool randomizePitch = false;
    [Min(0f)] public float randomPitchRange = 0.1f;
    [Tooltip("If > 0, loops will auto-stop after this many seconds (-1 = none).")]
    public float loopDurationSeconds = -1f;

    [Header("Fades")]
    [Tooltip("Seconds to fade IN when a loop starts/restarts.")]
    public float fadeInOnStartSeconds = 0f;
    [Tooltip("Fade-in curve shape for loops.")]
    public FadeShape fadeInShape = FadeShape.EaseInSine;     // NEW
    [Tooltip("Used only if FadeInShape = Custom. Time: 0..1, Value: 0..1")]
    public AnimationCurve customFadeIn = AnimationCurve.Linear(0, 0, 1, 1); // NEW

    [Tooltip("Seconds to fade OUT when stopping. 0 = instant.")]
    public float fadeOutOnStopSeconds = 0.05f;

    public bool MatchesTrigger(GlobalEvents.Id id, GameObject sender)
    {
        if ((triggerMask & id) == 0) return false;
        if (eventObject && sender && sender != eventObject) return false;
        return true;
    }

    public bool MatchesCancel(GlobalEvents.Id id, GameObject sender)
    {
        if ((cancelMask & id) == 0) return false;
        if (eventObject && sender && sender != eventObject) return false;
        return true;
    }

    // Delegate to AudioManager
    public void Trigger(Transform followTarget, float magnitude = 1f)
    {
        if (!sfx) { Debug.LogWarning($"[SoundEffectSpec] '{tag}' missing SfxClip"); return; }

        float volMul = volumeMultiplier /* * magnitude */;

        if (mode == SoundMode.OneShot)
        {
            AudioManager.I?.PlayOneShot(tag, sfx, followTarget, volMul, pitchOffset, randomizePitch, randomPitchRange);
        }
        else
        {
            AudioManager.I?.PlayLoop(
                tag, sfx, followTarget,
                volMul, pitchOffset, randomPitchRange: randomPitchRange, randomizePitch: randomizePitch,
                fadeInSeconds: fadeInOnStartSeconds,
                fadeInShape: fadeInShape,
                customFade: customFadeIn
            );

            if (loopDurationSeconds > 0f)
                AudioManager.I?.StopLoopAfter(tag, loopDurationSeconds, fadeOutOnStopSeconds);
        }
    }

    public void Cancel()
    {
        // Stop loop and any one-shots for this tag
        AudioManager.I?.StopAllForTag(tag, fadeOutOnStopSeconds);
    }
}
