using UnityEngine;

public class Dashboard : MonoBehaviour
{
    [Header("Wheel Settings")]
    [SerializeField] private Transform wheel;
    public float wheelAngle = 90f;        // max left/right deflection
    public float wheelTurnSpeed = 10f;    // lerp speed (per second)

    [Header("Bobbing Settings")] 
    public float maxBob = 1f;

    private PlayerInput _input;
    private Rigidbody _playerRb;
    private PlayerController _playerController;
    private PlayerControllerSettings _controllerSettings;
    
    private Vector3 _wheelStartEuler;
    private Vector3 _startPos;
    private float _maxJumpVel;

    void Awake()
    {
        _input = GetComponent<PlayerInput>();
        _playerController = FindFirstObjectByType<PlayerController>();

        if (_playerController)
        {
            _playerRb = _playerController.GetComponent<Rigidbody>();
            _controllerSettings = _playerController.DefaultSettings;
        }
            
    }

    void Start()
    {
        _wheelStartEuler = wheel.localEulerAngles;
        _maxJumpVel = Mathf.Sqrt(_playerController.DefaultSettings.jumpHeight * -2f * Physics.gravity.y * _playerController.DefaultSettings.gravity);
        _startPos = transform.position;
    }

    void Update()
    {
        TurnWheel();
        Bobbing();
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

    private void Bobbing()
    {
        float targetY = Helper.MapValue(_playerRb.linearVelocity.y, -_controllerSettings.terminalVelocity, _maxJumpVel, maxBob, -maxBob);
        transform.position =Vector3.Lerp(transform.position, _startPos + Vector3.up * targetY, Time.deltaTime); 
    }
}