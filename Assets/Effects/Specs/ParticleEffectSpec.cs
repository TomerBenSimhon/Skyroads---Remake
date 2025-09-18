using System;
using UnityEngine;

public enum ParticleMode { OneShot, Looping }

[Serializable]
public class ParticleEffectSpec
{
    [Tooltip("Purely informational, to match your Spec pattern.")]
    public string tag = "VFX/Generic";

    [Header("Target")]
    [Tooltip("The GameObject that has the ParticleSystem (or a child).")]
    public GameObject particleObject;

    [Header("Behavior")]
    public ParticleMode mode = ParticleMode.OneShot;

    [Tooltip("For Looping: if > 0, auto-stop after this many seconds; if <= 0, runs until Effects.Cancel()")]
    public float loopDurationSeconds = -1f;

    [Tooltip("When stopping, also clear existing particles instantly.")]
    public bool clearOnStop = false;

    /// Play according to mode (called by Effects.Play)
    public void Play(EffectCall call)
    {
        if (!particleObject)
        {
            Debug.LogWarning($"[ParticleEffectSpec] '{tag}' has no particleObject assigned.", particleObject);
            return;
        }

        var ctrl = particleObject.GetComponent<ParticleController>();
        if (!ctrl) ctrl = particleObject.AddComponent<ParticleController>();

        switch (mode)
        {
            case ParticleMode.OneShot:
                ctrl.PlayOneShot();
                break;
            case ParticleMode.Looping:
                ctrl.PlayLoop(loopDurationSeconds);
                break;
        }
    }

    /// Stop according to mode (called by Effects.Cancel)
    public void Stop(EffectCall call)
    {
        if (!particleObject) return;
        var ctrl = particleObject.GetComponent<ParticleController>();
        if (!ctrl) return;

        // We keep it simple: any Cancel() stops loopers; one-shots are no-ops unless you want to force-stop them too
        ctrl.Stop(clearOnStop);
    }
}