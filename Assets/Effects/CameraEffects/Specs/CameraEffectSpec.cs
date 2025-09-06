// CameraEffectSpec.cs
using System;
using UnityEngine;

[Serializable]
public abstract class CameraEffectSpec
{
    [Tooltip("Used by the CameraEffectsManager for replace/stack rules")]
    public string tag = "Cam/Generic";

    [Tooltip("Per-instance multiplier applied to the incoming magnitude")]
    public float magnitudeScale = 1f;
    
    [Tooltip("If true, cancel/replace any running camera effects with the same Tag before adding this one.")]
    public bool replaceExisting = false;   // <- per-effect control (you can swap to TagPolicy if you prefer)

    /// Build the runtime ICameraEffect you already use.
    public abstract ICameraEffect Build(EffectCall call);
}

// FovPulseSpec.cs
[Serializable]
public class FovPulseSpec : CameraEffectSpec
{
    public float delta = 10f;
    public float inTime = 0.10f, holdTime = 0.10f, outTime = 0.20f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0,0,1,1);

    public override ICameraEffect Build(EffectCall call)
    {
        return new FovPulseEffect(
            delta * magnitudeScale * call.magnitude,
            inTime, holdTime, outTime,
            curve, tag)
        ;
    }
}