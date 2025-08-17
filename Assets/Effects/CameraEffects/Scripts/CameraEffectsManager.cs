// CameraEffectsManager.cs
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class CameraEffectsManager : MonoBehaviour
{
    public static CameraEffectsManager I { get; private set; }

    [SerializeField] CinemachineCamera vcam;   // assign in scene OR auto-find
    float defaultFov = 60f;

    readonly List<ICameraEffect> _effects = new();
    float _baseFov;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        //DontDestroyOnLoad(gameObject);

        if (!vcam)
            vcam = FindAnyObjectByType<CinemachineCamera>(FindObjectsInactive.Exclude);

        _baseFov = vcam ? vcam.Lens.FieldOfView : defaultFov;
    }

    public void RebindVcam(CinemachineCamera cam, bool readBase = true)
    {
        vcam = cam;
        if (readBase && vcam) _baseFov = vcam.Lens.FieldOfView;
    }

    public void SetBaseFov(float fov)
    {
        _baseFov = fov;
        if (vcam) vcam.Lens.FieldOfView = fov;
    }

    public void Play(ICameraEffect effect, bool replaceTag = true)
    {
        if (replaceTag && !string.IsNullOrEmpty(effect.Tag))
            CancelTag(effect.Tag);

        effect.OnStart(this);
        _effects.Add(effect);
    }

    public void CancelTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return;
        for (int i = _effects.Count - 1; i >= 0; --i)
            if (_effects[i].Tag == tag) { _effects[i].Cancel(); _effects.RemoveAt(i); }
    }

    void LateUpdate()
    {
        if (!vcam) return;

        float dt = Time.unscaledDeltaTime; // ignore slow-mo; change to deltaTime if you want
        float fovAdd = 0f;

        for (int i = _effects.Count - 1; i >= 0; --i)
        {
            var e = _effects[i];
            var d = e.Tick(dt);
            fovAdd += d.fovAdd;

            if (e.IsFinished)
                _effects.RemoveAt(i);
        }

        vcam.Lens.FieldOfView = _baseFov + fovAdd;
    }
}