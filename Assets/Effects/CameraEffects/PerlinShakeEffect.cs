using UnityEngine;

/// One-shot Perlin noise camera shake that adds local position & rotation only.
/// NOTE: No FOV contribution here (fovAdd is always 0) — FOV is owned by FovPulseEffect.
public class PerlinShakeEffect : ICameraEffect
{
    public string Tag { get; private set; }
    public bool IsFinished { get; private set; }

    private readonly float _duration;
    private readonly AnimationCurve _env;
    private readonly Vector3 _posAmp;
    private readonly Vector3 _rotAmp;
    private readonly float _freq;
    private readonly Vector3 _seed;

    private float _t; // elapsed

    public PerlinShakeEffect(
        string tag,
        float duration,
        AnimationCurve envelope,
        Vector3 posAmplitude,
        Vector3 rotAmplitude,
        float frequency,
        Vector3 seed,
        bool replaceExisting // kept for consistency with spec; not used at runtime
    )
    {
        Tag       = tag;
        _duration = Mathf.Max(0f, duration);
        _env      = envelope ?? AnimationCurve.Linear(0, 1, 1, 0);
        _posAmp   = posAmplitude;
        _rotAmp   = rotAmplitude;
        _freq     = Mathf.Max(0.01f, frequency);
        _seed     = seed;

        IsFinished = false;
        _t = 0f;
    }

    public void OnStart(CameraEffectsManager ctx) { /* no-op */ }

    public CamDelta Tick(float dt)
    {
        if (IsFinished) return CamDelta.Zero;

        _t += Mathf.Max(0f, dt);
        float u = (_duration > 0f) ? Mathf.Clamp01(_t / _duration) : 1f;
        float w = Mathf.Clamp01(_env.Evaluate(u)); // 1→0 damping over lifetime

        // timebase
        float time = Time.unscaledTime * _freq;

        // Per-axis Perlin mapped to [-1,1]
        float nx = Mathf.PerlinNoise(_seed.x, time) * 2f - 1f;
        float ny = Mathf.PerlinNoise(_seed.y, time + 17.123f) * 2f - 1f;
        float nz = Mathf.PerlinNoise(_seed.z, time + 33.789f) * 2f - 1f;

        Vector3 pos = new Vector3(nx * _posAmp.x, ny * _posAmp.y, nz * _posAmp.z) * w;
        Vector3 eul = new Vector3(nx * _rotAmp.x, ny * _rotAmp.y, nz * _rotAmp.z) * w;

        if (_t >= _duration)
            IsFinished = true;

        return new CamDelta
        {
            fovAdd   = 0f,  // <-- NO FOV contribution
            posAdd   = pos,
            eulerAdd = eul
        };
    }

    public void Cancel()
    {
        IsFinished = true;
    }
}
