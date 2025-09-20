// CameraEffectSpec.cs
using System;
using UnityEngine;

[Serializable]
public abstract class CameraEffectSpec
{
    [Header("Info")]
    public string tag = "Cam/Generic";
    public float magnitudeScale = 1f;
    public bool replaceExisting = false;

    [Header("Events (per-spec)")]
    public GlobalEvents.Id triggerMask = GlobalEvents.Id.None;
    public GlobalEvents.Id cancelMask  = GlobalEvents.Id.None;

    [Tooltip("Optional filter: if set, only respond when this GameObject is the event sender.")]
    public GameObject eventObject;

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

    public abstract ICameraEffect Build(EffectCall call);
}

// ===== Example concrete spec you already have =====
[Serializable]
public class FovPulseSpec : CameraEffectSpec
{
    [Header("Fov Pulse settings")]
    public float delta = 10f;
    public float inTime = 0.10f, holdTime = 0.10f, outTime = 0.20f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0,0,1,1);

    public override ICameraEffect Build(EffectCall call)
    {
        return new FovPulseEffect(
            delta * magnitudeScale * call.magnitude,
            inTime, holdTime, outTime,
            curve, tag
        );
    }
}