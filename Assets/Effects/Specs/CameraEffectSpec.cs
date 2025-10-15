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
            tag,                                          
            delta * magnitudeScale * call.magnitude,
            inTime,
            holdTime,
            outTime,
            curve
        );
    }

}

/// Spec for Perlin noise camera shake (position/rotation only).
[Serializable]
public class PerlinShakeSpec : CameraEffectSpec
{
    [Header("Duration & Envelope")]
    [Min(0f)] public float duration = 0.35f;
    public AnimationCurve envelope = AnimationCurve.EaseInOut(0, 1, 1, 0); // 1→0 damping

    [Header("Amplitude (local space)")]
    public Vector3 posAmplitude = new Vector3(0.1f, 0.1f, 0f);  // meters
    public Vector3 rotAmplitude = new Vector3(2f, 2f, 1.5f);    // degrees (pitch,yaw,roll)

    [Header("Noise")]
    public float frequency = 18f;                                // Hz-ish
    public Vector3 seed    = new Vector3(127.31f, 251.17f, 73.97f);

    public override ICameraEffect Build(EffectCall call)
    {
        float scale = magnitudeScale * call.magnitude;
        return new PerlinShakeEffect(
            tag,
            duration,
            envelope,
            posAmplitude * scale,
            rotAmplitude * scale,
            frequency,
            seed,
            replaceExisting
        );
    }
}
