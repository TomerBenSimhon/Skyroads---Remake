using System.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(DashboardEffects))]
public class Dashboard : MonoBehaviour
{
    [Header("First Pop Up Settings")]
    public bool firstPopUpEnabled = false;
    public float popUpDuration = 3f;
    public float popUpDelay = 0f;
    
    [Header("Player Status")]
    public bool isPlayerBroken = false;
    public bool isPlayerShoot = false;
    public bool isPlayerDead = false;

    [Header("Wheel Settings")]
    [SerializeField] private Transform wheel;
    public float wheelAngle = 90f;        // max left/right deflection
    public float wheelTurnSpeed = 10f;    // lerp speed (per second)

    [Header("Button Settings")]
    [SerializeField] private Transform jumpButton;
    [SerializeField] private Transform fireButton;
    public float buttonDelta = 1f;
    public float buttonSpeed = 1f;

    [Header("Bobbing Settings")]
    public float maxBobY = 1f;
    public float maxBobX = 1f;
    public float bobSpeed = 1f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 1f;
    public float rotationAngle = 5f;

    [Header("Screen Effect Settings")]
    [SerializeField] private Transform screenZoomTransform;   // absolute pose target for zoom
    public float screenZoomIn = 0.5f;
    public float screenZoomHold = 2f;
    public float screenZoomOut = 0.5f;
    [SerializeField] private GlobalEvents.Id triggerEvent;
    [SerializeField] private GameObject eventObject;
    [SerializeField] private RightScreenMat rightScreenMat;

    // --- refs
    private PlayerInput _input;
    private Rigidbody _playerRb;
    private PlayerController _playerController;
    private PlayerControllerSettings _controllerSettings;
    private DashboardEffects _dashboardEffects;

    // --- original baselines
    private Vector3 _startPos;
    private Vector3 _startEuler;
    private Vector3 _wheelStartEuler;
    private Vector3 _fireButtonStartPos;
    private Vector3 _jumpButtonStartPos;

    // --- per-frame accumulators (additive deltas)
    private Vector3 _posDelta;   // additive offset in local space
    private Vector3 _eulerDelta; // additive euler (applied as extra rotation)
    
    // --- persistent smoothed offsets (carry across frames)
    private Vector3 _bobOffset;     // current smoothed bob offset
    private Vector3 _bobVel;        // SmoothDamp velocity

    private float _tiltZ;           // current smoothed z-tilt (deg)
    private float _tiltZVel;        // SmoothDamp velocity

    // --- zoom & popup “weights” (0..1) blended on top of baseline BEFORE deltas
    private float _zoomW;                 // driven by coroutine; 0 = base pose, 1 = screenZoomTransform
    private bool _canScreenZoom = true;
    private Coroutine _screenZoomCoroutine;

    private float _popupW;                // 0 = fully offset, 1 = settled
    private Vector3 _popupOffset = new Vector3(0f, -5.29f, 0f); // where the dashboard starts if firstPopUpEnabled

    // --- misc
    private float _maxJumpVel;

    void Awake()
    {
        _playerController = FindFirstObjectByType<PlayerController>();
        if (_playerController)
        {
            _playerRb = _playerController.GetComponent<Rigidbody>();
            _input = _playerController.GetComponent<PlayerInput>();
            _controllerSettings = _playerController.DefaultSettings;
        }

        _dashboardEffects = GetComponent<DashboardEffects>();

        // Do NOT move the transform here anymore — we’ll add a popup offset when applying pose.
        if (firstPopUpEnabled)
            _popupW = 0f;
        else
            _popupW = 1f; // no animation -> already settled
    }

    void Start()
    {
        _startPos = transform.localPosition;
        _startPos.y = 0.29f;
        _startEuler = transform.localEulerAngles;

        _wheelStartEuler = wheel.localEulerAngles;
        _fireButtonStartPos = fireButton.localPosition;
        _jumpButtonStartPos = jumpButton.localPosition;

        _maxJumpVel = Mathf.Sqrt(
            _playerController.DefaultSettings.jumpHeight *
            -2f * Physics.gravity.y *
            _playerController.DefaultSettings.gravity);

        if (firstPopUpEnabled) StartCoroutine(DashboardPopUpCR());
    }

    void OnEnable()
    {
        GlobalEvents.Raised += OnGlobalEvent;
    }

    void OnDisable()
    {
        GlobalEvents.Raised -= OnGlobalEvent;
    }

    void OnGlobalEvent(GlobalEvents.Id id, GameObject sender)
    {
        //player status
        UpdatePlayerStatus(id, sender);
        
        //screen zoom
        if (triggerEvent == GlobalEvents.Id.None) return;
        if ((triggerEvent & id) == 0) return;
        if (eventObject != null && eventObject != sender) return;
        if (!_canScreenZoom) return;

        if (_screenZoomCoroutine != null) StopCoroutine(_screenZoomCoroutine);
        _screenZoomCoroutine = StartCoroutine(ScreenZoomCR());
    }

    

    void Update()
    {
        // 1) reset accumulators
        _posDelta = Vector3.zero;
        _eulerDelta = Vector3.zero;

        // 2) contributions (these only modify posDelta/eulerDelta, children, or weights)
        TurnWheel();         // child-only (independent)
        PressButtons();      // children-only (independent)
        ContributeBobbing(); // adds to _posDelta
        ContributeDashTilt();// adds to _eulerDelta.z

        // 3) compose final pose ONCE (base blended pose + additive deltas)
        ApplyPose();
    }

    // -----------------------
    // Contributions (deltas)
    // -----------------------

    private void ContributeBobbing()
    {
        // map velocity -> desired offset
        float bobY = Helper.MapValue(
            _playerRb.linearVelocity.y,
            -_controllerSettings.terminalVelocity, _maxJumpVel,
            maxBobY, -maxBobY);

        float bobX = Helper.MapValue(
            _playerRb.linearVelocity.x,
            -_controllerSettings.horizontalSpeed, _controllerSettings.horizontalSpeed,
            maxBobX, -maxBobX);

        Vector3 desired = new Vector3(bobX, bobY, 0f);

        // SmoothDamp toward desired; bobSpeed is "units per second" feel:
        // convert to SmoothDamp 'smoothTime' (lower = snappier)
        float smoothTime = Mathf.Max(0.0001f, 1f / Mathf.Max(0.0001f, bobSpeed));
        _bobOffset = Vector3.SmoothDamp(_bobOffset, desired, ref _bobVel, smoothTime);
    }

    private void ContributeDashTilt()
    {
        float input = _input.HorizontalMovementInput;
        float desiredZ = -rotationAngle * input;

        // Smooth tilt in degrees
        float smoothTime = Mathf.Max(0.0001f, 1f / Mathf.Max(0.0001f, rotationSpeed));
        _tiltZ = Mathf.SmoothDamp(_tiltZ, desiredZ, ref _tiltZVel, smoothTime);
    }


    // -----------------------
    // Children (independent)
    // -----------------------

    private void TurnWheel()
    {
        if(isPlayerDead) return;
        
        float input = _input.HorizontalMovementInput;
        if (isPlayerBroken) input *= 0.5f;
        float targetZ = _wheelStartEuler.z + wheelAngle * input;
        float currentZ = wheel.localEulerAngles.z;
        float newZ = Mathf.LerpAngle(currentZ, targetZ, wheelTurnSpeed * Time.deltaTime);
        var e = wheel.localEulerAngles; e.z = newZ; wheel.localEulerAngles = e;
    }

    private void PressButtons()
    {
        if (!isPlayerBroken)
        {
            Vector3 targetJump = _jumpButtonStartPos;
            targetJump.y += _input.JumpHeld ? -buttonDelta : 0f;
            jumpButton.localPosition = Vector3.Lerp(jumpButton.localPosition, targetJump, buttonSpeed * Time.deltaTime);
        }
        if(isPlayerShoot)
        {
            Vector3 targetFire = _fireButtonStartPos;
            targetFire.y += _input.ShootHeld ? -buttonDelta : 0f;
            fireButton.localPosition = Vector3.Lerp(fireButton.localPosition, targetFire, buttonSpeed * Time.deltaTime);
        }
    }

    // -----------------------
    // Final composition
    // -----------------------

    private void ApplyPose()
    {
        // Base pose blended with screen zoom (absolute), THEN apply popup, THEN add deltas
        Vector3 basePos   = Vector3.Lerp(_startPos, screenZoomTransform ? screenZoomTransform.localPosition : _startPos, _zoomW);
        Quaternion baseRo = Quaternion.Slerp(Quaternion.Euler(_startEuler),
                                             screenZoomTransform ? screenZoomTransform.localRotation : Quaternion.Euler(_startEuler),
                                             _zoomW);

        // First-pop-up offset: add an extra downward offset that fades out to zero
        Vector3 popupOffsetNow = Vector3.Lerp(_popupOffset, Vector3.zero, _popupW);
        basePos += popupOffsetNow;

        // Now add this frame's contributions
        // 1) persistent smoothed offsets:
        basePos += _bobOffset;

        // 2) per-frame “plug-in” deltas (optional for other effects)
        basePos += _posDelta;
        Quaternion finalRot = baseRo * Quaternion.Euler(0f, 0f, _tiltZ) * Quaternion.Euler(_eulerDelta);

        transform.localPosition = basePos;
        transform.localRotation = finalRot;

    }

    // -----------------------
    // Coroutines: weights only
    // -----------------------

    private IEnumerator ScreenZoomCR()
    {
        _canScreenZoom = false;
        _dashboardEffects.SwitchScreenMat(rightScreenMat, screenZoomIn + screenZoomHold);

        // Ease in (0 → 1) with smoothstep
        float t = 0f;
        while (t < screenZoomIn)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / screenZoomIn);
            // simple ease in/out
            _zoomW = n * n * (3f - 2f * n);
            yield return null;
        }
        _zoomW = 1f;

        // Hold
        yield return new WaitForSeconds(screenZoomHold);

        // Ease out (1 → 0)
        t = 0f;
        while (t < screenZoomOut)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / screenZoomOut);
            _zoomW = 1f - n * n * (3f - 2f * n);
            yield return null;
        }
        _zoomW = 0f;

        _canScreenZoom = true;
        _screenZoomCoroutine = null;
    }

    private IEnumerator DashboardPopUpCR()
    {
        yield return new WaitForSeconds(popUpDelay);
        float t = 0f;
        while (t < popUpDuration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / popUpDuration);
            _popupW = n * n * (3f - 2f * n); // same smoothstep easing
            yield return null;
        }
        _popupW = 1f;
    }
    
    private void UpdatePlayerStatus(GlobalEvents.Id id, GameObject sender)
    {
        switch (id)
        {
            case GlobalEvents.Id.PlayerDied:
                isPlayerDead = true;
                break;
            case GlobalEvents.Id.PlayerRespawned:
                isPlayerDead = false;
                break;
            case GlobalEvents.Id.FixApplied:
                isPlayerBroken = false;
                break;
            case GlobalEvents.Id.PowerUpApplied:
                isPlayerShoot = true;
                break;
        }
    }
    
    #if UNITY_EDITOR
    // --- Inline custom inspector ---
    [CustomEditor(typeof(Dashboard))]
    private class DashboardEditor : Editor
    {
        private bool showPopUp;
        private bool showStatus;
        private bool showWheel;
        private bool showButtons;
        private bool showBobbing;
        private bool showRotation;
        private bool showScreen;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            showPopUp = EditorGUILayout.Foldout(showPopUp, "First Pop Up Settings", true);
            if (showPopUp)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("firstPopUpEnabled"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("popUpDuration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("popUpDelay"));
            }

            showStatus = EditorGUILayout.Foldout(showStatus, "Player Status", true);
            if (showStatus)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("isPlayerBroken"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("isPlayerShoot"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("isPlayerDead"));
            }

            showWheel = EditorGUILayout.Foldout(showWheel, "Wheel Settings", true);
            if (showWheel)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("wheel"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("wheelAngle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("wheelTurnSpeed"));
            }

            showButtons = EditorGUILayout.Foldout(showButtons, "Button Settings", true);
            if (showButtons)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpButton"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("fireButton"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("buttonDelta"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("buttonSpeed"));
            }

            showBobbing = EditorGUILayout.Foldout(showBobbing, "Bobbing Settings", true);
            if (showBobbing)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxBobY"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxBobX"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bobSpeed"));
            }

            showRotation = EditorGUILayout.Foldout(showRotation, "Rotation Settings", true);
            if (showRotation)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationAngle"));
            }

            showScreen = EditorGUILayout.Foldout(showScreen, "Screen Effect Settings", true);
            if (showScreen)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("screenZoomTransform"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("screenZoomIn"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("screenZoomHold"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("screenZoomOut"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("triggerEvent"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("eventObject"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rightScreenMat"));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif

}



