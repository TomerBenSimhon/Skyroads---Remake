using UnityEngine;
using UnityEngine.Serialization;

public class Dashboard : MonoBehaviour
{
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

    private PlayerInput _input;
    private Rigidbody _playerRb;
    private PlayerController _playerController;
    private PlayerControllerSettings _controllerSettings;
    
    private Vector3 _wheelStartEuler;
    private Vector3 _startPos;
    private Vector3 _startEuler;
    private Vector3 _fireButtonStartPos;
    private Vector3 _jumpButtonStartPos;
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
            
    }

    void Start()
    {
        _wheelStartEuler = wheel.localEulerAngles;
        _maxJumpVel = Mathf.Sqrt(_playerController.DefaultSettings.jumpHeight * -2f * Physics.gravity.y * _playerController.DefaultSettings.gravity);
        _startPos = transform.localPosition;
        _startEuler = transform.localEulerAngles;
        _fireButtonStartPos = fireButton.localPosition;
        _jumpButtonStartPos = jumpButton.localPosition;
    }

    void Update()
    {
        TurnWheel();
        PressButtons();
        Bobbing();
        RotateDashboard();
    }

    private void TurnWheel()
    {
        float input = _input.HorizontalMovementInput;

        // desired angle around Z, relative to start:
        float targetZ = _wheelStartEuler.z + wheelAngle * input;

        // smooth shortest-path interpolation over the 0/360 seam:
        float currentZ = wheel.localEulerAngles.z;
        float newZ = Mathf.LerpAngle(currentZ, targetZ, wheelTurnSpeed * Time.deltaTime);

        var e = wheel.localEulerAngles;
        e.z = newZ;
        wheel.localEulerAngles = e;
    }

    private void PressButtons()
    {
        Vector3 targetJump = _jumpButtonStartPos;
        targetJump.y += _input.JumpHeld ? -buttonDelta : 0f;
        
        jumpButton.localPosition = Vector3.Lerp(jumpButton.localPosition, targetJump, buttonSpeed * Time.deltaTime);

        Vector3 targetFire = _fireButtonStartPos;
        targetFire.y += _input.ShootHeld ? -buttonDelta : 0f;
        
        fireButton.localPosition = Vector3.Lerp(fireButton.localPosition, targetFire, buttonSpeed * Time.deltaTime);
    }

    private void Bobbing()
    {
        Vector3 target = _startPos;

        target.y += Helper.MapValue(_playerRb.linearVelocity.y,
            -_controllerSettings.terminalVelocity, _maxJumpVel,
            maxBobY, -maxBobY);
        
         target.x += Helper.MapValue(_playerRb.linearVelocity.x, 
            -_controllerSettings.horizontalSpeed, _controllerSettings.horizontalSpeed, 
            maxBobX, -maxBobX);
        
        transform.localPosition =Vector3.Lerp(transform.localPosition, target, bobSpeed * Time.deltaTime); 
    }

    private void RotateDashboard()
    {
        float input = _input.HorizontalMovementInput;

        // desired angle around Z, relative to start:
        float targetZ = _startEuler.z - rotationAngle * input;

        // smooth shortest-path interpolation over the 0/360 seam:
        float currentZ = transform.localEulerAngles.z;
        float newZ = Mathf.LerpAngle(currentZ, targetZ, rotationSpeed * Time.deltaTime);

        var e = transform.localEulerAngles;
        e.z = newZ;
        transform.localEulerAngles = e;
    }
}