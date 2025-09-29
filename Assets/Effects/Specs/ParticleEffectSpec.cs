using System;
using UnityEngine;

public enum ParticleMode { OneShot, Looping }

[Serializable]
public class ParticleEffectSpec
{
    [Header("Info")]
    public string tag = "VFX/Generic";

    [Header("Target")]
    public GameObject particleObject;

    [Header("Events (per-spec)")]
    public GlobalEvents.Id triggerMask = GlobalEvents.Id.None;
    public GlobalEvents.Id cancelMask  = GlobalEvents.Id.None;

    [Tooltip("Optional filter: if set, only respond when this GameObject is the event sender.")]
    public GameObject eventObject;

    [Header("Behavior")]
    public ParticleMode mode = ParticleMode.OneShot;
    public float loopDurationSeconds = -1f;
    public bool clearOnStop = false;

    public bool MatchesTrigger(GlobalEvents.Id id, GameObject sender)
    {
        if ((triggerMask & id) == 0) return false;
        if (eventObject != null && sender != eventObject && sender != null) return false;
        return true;
    }

    public bool MatchesCancel(GlobalEvents.Id id, GameObject sender)
    {
        if ((cancelMask & id) == 0) return false;
        if (eventObject != null && sender != eventObject && sender != null) return false;
        return true;
    }
    public void Play()
    {
        if (!particleObject)
        {
            Debug.LogWarning($"[ParticleEffectSpec] '{tag}' has no particleObject assigned.");
            return;
        }
        var ctrl = particleObject.GetComponent<ParticleController>();
        if (!ctrl)
        {
            Debug.LogError($"{particleObject.name} has no ParticleController component.");
            ctrl = particleObject.AddComponent<ParticleController>();
        }

        if (mode == ParticleMode.OneShot)
            ctrl.PlayOneShot();
        else
            ctrl.PlayLoop(loopDurationSeconds);
    }

    public void Stop()
    {
        if (!particleObject) return;
        var ctrl = particleObject.GetComponent<ParticleController>();
        if (!ctrl) return;
        ctrl.Stop(clearOnStop);
    }
}