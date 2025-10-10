using System;
using UnityEngine;

public enum SoundMode { OneShot, Looping }
public enum SpaceOverride { UseAssetDefault, Force2D, Force3D }

[Serializable]
public class SoundEffectSpec
{
    [Header("Info")]
    public string tag = "SFX/Generic";

    [Header("Asset")]
    public SfxClip sfx;                               // holds the clip & defaults

    [Header("Events (per-spec)")]
    public GlobalEvents.Id triggerMask = GlobalEvents.Id.None;
    public GlobalEvents.Id cancelMask  = GlobalEvents.Id.None;

    [Tooltip("Optional filter: only react when this sender raises the event.")]
    public GameObject eventObject;

    [Header("Behavior")]
    public SoundMode mode = SoundMode.OneShot;
    [Tooltip("Override 2D/3D set on SfxClip; leave 'UseAssetDefault' to respect asset.")]
    public SpaceOverride spaceOverride = SpaceOverride.UseAssetDefault;

    [Range(0f,1f)] public float volumeMultiplier = 1f;
    public float pitchOffset = 0f;
    public bool randomizePitch = false;
    [Min(0f)] public float randomPitchRange = 0.1f;   // +/- around base pitch

    [Tooltip("If > 0, loops can auto-stop after this many seconds (-1 = no auto-stop).")]
    public float loopDurationSeconds = -1f;

    [Header("Stop Options")]
    [Tooltip("Seconds to fade-out when stopping a loop. 0 = immediate.")]
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

    // runtime-resolved effective spatial mode
    public bool Use3D()
    {
        return spaceOverride switch
        {
            SpaceOverride.Force2D => false,
            SpaceOverride.Force3D => true,
            _ => (sfx && sfx.is3D)
        };
    }

    // resolve final volume/pitch (you can scale by EffectCall.magnitude later)
    public void ResolveVolumePitch(out float vol, out float pitch, float magnitude = 1f)
    {
        float baseVol = sfx ? sfx.volume : 1f;
        float basePitch = sfx ? sfx.pitch : 1f;

        float p = basePitch + pitchOffset;
        if (randomizePitch && randomPitchRange > 0f)
            p += UnityEngine.Random.Range(-randomPitchRange, randomPitchRange);

        vol = Mathf.Clamp01(baseVol * volumeMultiplier /* * magnitude */);
        pitch = p;
    }
}


