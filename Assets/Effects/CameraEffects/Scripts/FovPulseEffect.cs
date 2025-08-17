using UnityEngine;

public class FovPulseEffect : ICameraEffect
{
    public string Tag { get; }
    public bool IsFinished { get; private set; }

    private readonly float _delta;                 // e.g., +8f
    private readonly float _inTime, _holdTime, _outTime;
    private readonly AnimationCurve _curve;        // 0..1 easing curve

    private float _t;                              // phase timer
    private int _phase;                            // 0=in, 1=hold, 2=out

    public FovPulseEffect(
        float delta, float inTime, float holdTime, float outTime,
        AnimationCurve curve = null, string tag = "FOV/Boost")
    {
        _delta = delta;
        _inTime = Mathf.Max(0.0001f, inTime);
        _holdTime = Mathf.Max(0f, holdTime);
        _outTime = Mathf.Max(0.0001f, outTime);
        _curve = curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1);
        Tag = tag;
    }

    public void OnStart(CameraEffectsManager ctx)
    {
        _t = 0f; _phase = 0; IsFinished = false;
    }

    public CamDelta Tick(float dt)
    {
        if (IsFinished) return default;

        float w; // weight 0..1
        switch (_phase)
        {
            case 0: // IN
                _t += dt;
                w = _curve.Evaluate(Mathf.Clamp01(_t / _inTime));
                if (_t >= _inTime) { _phase = _holdTime > 0f ? 1 : 2; _t = 0f; }
                break;

            case 1: // HOLD
                _t += dt; w = 1f;
                if (_t >= _holdTime) { _phase = 2; _t = 0f; }
                break;

            default: // OUT
                _t += dt;
                w = 1f - _curve.Evaluate(Mathf.Clamp01(_t / _outTime));
                if (_t >= _outTime) { IsFinished = true; w = 0f; }
                break;
        }

        return new CamDelta { fovAdd = _delta * w };
    }

    public void Cancel() { IsFinished = true; }
}