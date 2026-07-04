using UnityEngine;
using Unity.Cinemachine;

/// Apply additive position/rotation/FOV exactly once at the end of the CM pipeline (CM3).
[RequireComponent(typeof(CinemachineCamera))]
public class CameraShakeExtension : CinemachineExtension
{
    // Set every frame by CameraEffectsManager
    [HideInInspector] public Vector3 posAdd;    // local-space meters
    [HideInInspector] public Vector3 eulerAdd;  // degrees (pitch,yaw,roll)
    [HideInInspector] public float   fovAdd;    // degrees

    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage,
        ref CameraState state, float deltaTime)
    {
        // Only modify the final composed state once.
        if (stage != CinemachineCore.Stage.Finalize)
            return;

        // 1) Rotation (add in local space)
        if (eulerAdd.sqrMagnitude > 0f)
        {
            var qAdd = Quaternion.Euler(eulerAdd);
            state.OrientationCorrection = state.OrientationCorrection * qAdd;
        }

        // 2) Position (apply in world space using the corrected orientation)
        if (posAdd.sqrMagnitude > 0f)
        {
            var correctedOrientation = state.RawOrientation * state.OrientationCorrection;
            var worldOffset = correctedOrientation * posAdd;
            state.PositionCorrection += worldOffset;
        }

        // 3) FOV: add for this frame
        if (fovAdd != 0f)
        {
            var lens = state.Lens;
            lens.FieldOfView += fovAdd;
            lens.FieldOfView = Mathf.Clamp(lens.FieldOfView, 0f, CameraEffectsManager.I.maxFov);
            state.Lens = lens;
        }
    }
}