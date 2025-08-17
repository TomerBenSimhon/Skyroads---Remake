// Effects.cs  (camera-only for now)
using System.Collections.Generic;
using UnityEngine;

public class Effects : MonoBehaviour
{
    [Header("Camera Effects")]
    [SerializeReference] 
    private List<CameraEffectSpec> cameraEffects;

    /// Call this from gameplay when you want to play effects on this object.
    public void Play(Transform target = null, float magnitude = 1f, Vector3? positionOverride = null)
    {
        var call = new EffectCall {
            source   = transform,
            target   = target,
            position = positionOverride ?? transform.position,
            magnitude= magnitude
        };

        if (CameraEffectsManager.I != null && cameraEffects != null)
        {
            for (int i = 0; i < cameraEffects.Count; i++)
            {
                var spec = cameraEffects[i];
                if (spec == null) continue;
                var eff = spec.Build(call);
                if (eff != null)
                {
                    bool policy = spec.replaceExisting;
                    CameraEffectsManager.I.Play(eff, policy);
                }
            }
        }
    }
}