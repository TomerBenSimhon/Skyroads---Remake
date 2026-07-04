using UnityEngine;

/// Event-driven squash & stretch system.
/// - Call OnPlayerJump()  -> stretch along Jump axis
/// - Call OnPlayerBoost() -> stretch along Boost axis
/// - Call OnPlayerLanded()-> squash along Land axis
[AddComponentMenu("VFX/Squash & Stretch (Event Driven, Per Axis)")]
public class PlayerSquashStretch : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Child transform to deform (keep colliders/controllers on the root).")]
    public Transform visual;

    [Header("Volume & Spring")]
    [Tooltip("Preserve volume by inversely scaling the other two axes.")]
    public bool preserveVolume = true;

    [Tooltip("Angular frequency (rad/s). Higher = snappier.")]
    public float springAngularFrequency = 16f;
    [Tooltip("Damping ratio. 1 = critically damped (fastest no-overshoot).")]
    [Range(0.1f, 2f)] public float springDampingRatio = 1.0f;

    [Tooltip("Clamp final scale on the main axis to avoid over-deformation.")]
    public Vector2 axisScaleClamp = new Vector2(0.6f, 1.6f);

    public enum Axis { X, Y, Z }

    [Header("Jump Stretch")]
    [Tooltip("Axis affected when jumping (e.g., Y for vertical stretch).")]
    public Axis jumpAxis = Axis.Y;
    [Tooltip("Scale delta applied on jump (e.g., 0.25 => +25%).")]
    public float jumpStretch = 0.25f;
    [Tooltip("Half-life of jump impulse decay (seconds).")]
    public float jumpHalfLife = 0.12f;

    [Header("Boost Stretch")]
    [Tooltip("Axis affected when boosting (e.g., Z for forward stretch).")]
    public Axis boostAxis = Axis.Z;
    [Tooltip("Scale delta applied on boost (e.g., 0.2 => +20%).")]
    public float boostStretch = 0.20f;
    [Tooltip("Half-life of boost impulse decay (seconds).")]
    public float boostHalfLife = 0.10f;

    [Header("Landing Squash")]
    [Tooltip("Axis affected when landing (e.g., Y for vertical squash).")]
    public Axis landAxis = Axis.Y;
    [Tooltip("Scale delta applied on landing (e.g., 0.3 => -30%).")]
    public float landSquash = 0.30f;
    [Tooltip("Half-life of landing impulse decay (seconds).")]
    public float landHalfLife = 0.10f;

    // runtime state
    private Vector3 _baseScale = Vector3.one;
    private Vector3 _currentScale = Vector3.one;
    private Vector3 _scaleVel = Vector3.zero;

    // impulse intensities
    private float _jumpImp, _boostImp, _landImp;

    void OnEnable()
    {
        GlobalEvents.Raised += OnEvent;
    }

    void OnDisable()
    {
        GlobalEvents.Raised -= OnEvent;
    }

    private void OnEvent(GlobalEvents.Id id, GameObject sender)
    {
        if ((id & GlobalEvents.Id.BoostApplied) != 0)
        {
            OnPlayerBoost();
        }
        if ((id & GlobalEvents.Id.PlayerJumped) != 0)
        {
            OnPlayerJump();
        }
        if ((id & GlobalEvents.Id.PlayerGrounded) != 0)
        {
            OnPlayerLanded();
        }
    }

    void Reset()
    {
        if (!visual && transform.childCount > 0)
            visual = transform.GetChild(0);
    }

    void Awake()
    {
        if (!visual)
        {
            Debug.LogWarning($"{name}: Assign a 'visual' child to deform.");
            enabled = false;
            return;
        }

        _baseScale = visual.localScale;
        _currentScale = Vector3.one;
        _scaleVel = Vector3.zero;
        _jumpImp = _boostImp = _landImp = 0f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // 1) Decay impulses
        _jumpImp  = Decay(_jumpImp,  jumpHalfLife,  dt);
        _boostImp = Decay(_boostImp, boostHalfLife, dt);
        _landImp  = Decay(_landImp,  landHalfLife,  dt);

        // 2) Calculate total scale contribution
        Vector3 targetScale = Vector3.one;

        // Jump → stretch along jumpAxis
        if (_jumpImp > 0f)
            ApplyAxisEffect(ref targetScale, jumpAxis, _jumpImp * jumpStretch, preserveVolume);

        // Boost → stretch along boostAxis
        if (_boostImp > 0f)
            ApplyAxisEffect(ref targetScale, boostAxis, _boostImp * boostStretch, preserveVolume);

        // Land → squash along landAxis
        if (_landImp > 0f)
            ApplyAxisEffect(ref targetScale, landAxis, -_landImp * landSquash, preserveVolume);

        // Clamp the main axis (avoid crazy deformation)
        targetScale.x = Mathf.Clamp(targetScale.x, axisScaleClamp.x, axisScaleClamp.y);
        targetScale.y = Mathf.Clamp(targetScale.y, axisScaleClamp.x, axisScaleClamp.y);
        targetScale.z = Mathf.Clamp(targetScale.z, axisScaleClamp.x, axisScaleClamp.y);

        // 3) Smooth toward target scale
        var p = Springs.Calc(dt, springAngularFrequency, springDampingRatio);
        Springs.Update(ref _currentScale, ref _scaleVel, targetScale, p);

        visual.localScale = Vector3.Scale(_baseScale, _currentScale);
    }

    // Public API — call these from your player logic:
    public void OnPlayerJump(float intensity = 1f)  => _jumpImp  = Mathf.Min(_jumpImp  + intensity, 2f);
    public void OnPlayerBoost(float intensity = 1f) => _boostImp = Mathf.Min(_boostImp + intensity, 2f);
    public void OnPlayerLanded(float intensity = 1f)=> _landImp  = Mathf.Min(_landImp  + intensity, 2f);

    // Helpers
    private static float Decay(float value, float halfLife, float dt)
    {
        if (value <= 0f || halfLife <= 1e-5f) return 0f;
        float lambda = Mathf.Log(2f) / halfLife;
        return value * Mathf.Exp(-lambda * dt);
    }

    private static void ApplyAxisEffect(ref Vector3 scale, Axis axis, float delta, bool preserveVol)
    {
        float main = 1f + delta;
        float cross = preserveVol ? 1f / Mathf.Sqrt(Mathf.Max(1e-4f, main)) : 1f;

        switch (axis)
        {
            case Axis.X: scale.x *= main; scale.y *= cross; scale.z *= cross; break;
            case Axis.Y: scale.y *= main; scale.x *= cross; scale.z *= cross; break;
            case Axis.Z: scale.z *= main; scale.x *= cross; scale.y *= cross; break;
        }
    }
}
