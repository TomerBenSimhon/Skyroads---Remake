using UnityEngine;

public class FovPulseEffect : ICameraEffect
{
    public string Tag { get; }
    public bool IsFinished { get; private set; }

    private readonly float _delta;
    private readonly float _inTime, _holdTime, _outTime;
    private readonly AnimationCurve _curve;

    private float _t;              // phase timer
    private int _phase;            // 0=in, 1=hold, 2=out
    private bool _cancelRequested; // flag set by Cancel()

    public FovPulseEffect(
        string tag,
        float delta,
        float inTime,
        float holdTime,
        float outTime,
        AnimationCurve curve = null
    )
    {
        Tag       = string.IsNullOrEmpty(tag) ? "FOV/Pulse" : tag;
        _delta    = delta;
        _inTime   = Mathf.Max(0.0001f, inTime);
        _holdTime = Mathf.Max(0f,       holdTime);
        _outTime  = Mathf.Max(0.0001f,  outTime);
        _curve    = curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    public void OnStart(CameraEffectsManager ctx)
    {
        _t = 0f;
        _phase = 0;            // start IN
        IsFinished = false;
        _cancelRequested = false;
    }

    public CamDelta Tick(float dt)
    {
        // If cancel requested during IN/HOLD, switch to OUT with weight continuity
        if (_cancelRequested && _phase != 2)
        {
            float uIn = 0f;
            if (_phase == 0) uIn = Mathf.Clamp01(_t / _inTime);
            else if (_phase == 1) uIn = 1f;

            // start OUT such that initial OUT weight ~= current weight
            float startUOut = 1f - uIn;                      // approximation
            _phase = 2;
            _t     = Mathf.Clamp01(startUOut) * _outTime;
        }

        float w; // normalized weight 0..1

        switch (_phase)
        {
            case 0: // IN
                _t += dt;
                w = Mathf.Clamp01(_curve.Evaluate(Mathf.Clamp01(_t / _inTime)));
                if (_t >= _inTime) { _phase = (_holdTime > 0f) ? 1 : 2; _t = 0f; }
                break;

            case 1: // HOLD
                _t += dt;
                w = 1f;
                if (_t >= _holdTime) { _phase = 2; _t = 0f; }
                break;

            default: // OUT
                _t += dt;
                // clamp in case curve overshoots above 1
                w = Mathf.Clamp01(1f - _curve.Evaluate(Mathf.Clamp01(_t / _outTime)));
                if (_t >= _outTime) { IsFinished = true; w = 0f; }
                break;
        }

        return new CamDelta { fovAdd = _delta * w };
    }

    public void Cancel()
    {
        _cancelRequested = true; // don’t finish now; enter OUT smoothly in Tick()
    }
}
