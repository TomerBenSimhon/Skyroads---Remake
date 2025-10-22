using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

/// Sums all ICameraEffect deltas and feeds them to Cinemachine via CameraShakeExtension.
/// No FOV smoothing here — FOV effects (e.g., FovPulseEffect) fully control the easing.
[DefaultExecutionOrder(100000)]
public class CameraEffectsManager : MonoBehaviour
{
    public static CameraEffectsManager I { get; private set; }

    [Header("Cinemachine")]
    [Tooltip("If empty, the first CinemachineCamera found in the scene will be used.")]
    [SerializeField] private CinemachineCamera vcam;
    [SerializeField] private CameraShakeExtension shakeExt; // auto-added if missing
    public float maxFov = 115f;

    [Header("Apply Toggles")]
    [SerializeField] private bool applyPositionShake = true;
    [SerializeField] private bool applyRotationShake = true;
    [SerializeField] private bool applyFovShake      = true;

    // Active effects
    private readonly List<ICameraEffect> _effects = new();

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        if (!vcam)
            vcam = FindAnyObjectByType<CinemachineCamera>(FindObjectsInactive.Exclude);

        if (vcam)
        {
            shakeExt = vcam.GetComponent<CameraShakeExtension>();
            if (!shakeExt) shakeExt = vcam.gameObject.AddComponent<CameraShakeExtension>();
        }
        else
        {
            Debug.LogWarning("[CameraEffectsManager] No CinemachineCamera found in scene.");
        }
    }

    public void RebindVcam(CinemachineCamera newCam)
    {
        vcam = newCam;
        if (vcam)
            shakeExt = vcam.GetComponent<CameraShakeExtension>() ?? vcam.gameObject.AddComponent<CameraShakeExtension>();
        else
            shakeExt = null;
    }

    public void Play(ICameraEffect effect, bool replaceTag = true)
    {
        if (effect == null) return;

        if (replaceTag && !string.IsNullOrEmpty(effect.Tag))
            CancelTag(effect.Tag);

        effect.OnStart(this);
        _effects.Add(effect);
    }

    public void CancelTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return;

        for (int i = _effects.Count - 1; i >= 0; --i)
        {
            if (_effects[i].Tag == tag)
            {
                _effects[i].Cancel();
            }
        }
    }

    private void LateUpdate()
    {
        if (!vcam || !shakeExt) return;

        float dt = Time.unscaledDeltaTime;

        float   fovAdd = 0f;
        Vector3 posAdd = Vector3.zero;
        Vector3 eulAdd = Vector3.zero;

        for (int i = _effects.Count - 1; i >= 0; --i)
        {
            var e = _effects[i];
            CamDelta d = e.Tick(dt);

            fovAdd += d.fovAdd;
            posAdd += d.posAdd;
            eulAdd += d.eulerAdd;

            if (e.IsFinished)
                _effects.RemoveAt(i);
        }

        // Hand off to the Cinemachine extension; it applies inside the CM pipeline
        shakeExt.posAdd   = applyPositionShake ? posAdd : Vector3.zero;
        shakeExt.eulerAdd = applyRotationShake ? eulAdd : Vector3.zero;
        shakeExt.fovAdd   = applyFovShake      ? fovAdd : 0f;
    }
}
