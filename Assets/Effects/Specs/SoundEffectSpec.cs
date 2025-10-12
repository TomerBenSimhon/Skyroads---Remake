using System;
using UnityEngine;

public enum SoundMode { OneShot, Looping }

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

    [Header("Stop")]
    [Tooltip("Seconds to fade-out when stopping. 0 = instant.")]
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

    // delegate to AudioManager
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
            AudioManager.I?.PlayLoop(tag, sfx, followTarget, volMul, pitchOffset, randomizePitch, randomPitchRange);
            if (loopDurationSeconds > 0f)
                AudioManager.I?.StopLoopAfter(tag, loopDurationSeconds, fadeOutOnStopSeconds);
        }
    }

    public void Cancel()
    {
        // Stop loop and any oneshots tagged with this tag
        AudioManager.I?.StopAllForTag(tag, fadeOutOnStopSeconds);
    }
}
