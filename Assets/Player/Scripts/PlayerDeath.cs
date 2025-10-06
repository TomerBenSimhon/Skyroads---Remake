using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    [Range(0f, 5f)] public float deathTimer;
    [Range(0f, 5f)] public float noFuelTimer;

    public bool IsDead { get; private set; }

    private PlayerController _playerController;
    private Rigidbody _rigidbody;
    private GameObject _playerVisuals;

    // Snapshot of initial runtime settings so we can restore after "no fuel"
    private float _initForward;
    private float _initHorizontalSpeed;
    private float _initHorizontalDeceleration;
    private float _initJump;
    private float _initTurn;

    void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        _rigidbody = GetComponent<Rigidbody>();
        _playerVisuals = transform.Find("Visuals").gameObject;
    }

    void Start()
    {
        // Cache initial movement settings
        _initForward   = _playerController.RuntimeSettings.forwardSpeed;
        _initHorizontalSpeed= _playerController.RuntimeSettings.horizontalSpeed;
        _initHorizontalDeceleration = _playerController.RuntimeSettings.horizontalDeceleration;
        _initJump      = _playerController.RuntimeSettings.jumpHeight;
        _initTurn      = _playerController.RuntimeSettings.turningAngle;
    }

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;
        ActivatePlayer(false);
        GameManager.Instance.RestartLevel(deathTimer);
        GlobalEvents.Raise(GlobalEvents.Id.PlayerDied);
    }

    public void DieNoFuel()
    {
        if (IsDead) return;
        IsDead = true;
        NoFuelEffect();
        GameManager.Instance.RestartLevel(noFuelTimer);
        GlobalEvents.Raise(GlobalEvents.Id.PlayerDied);
    }

    public void ActivatePlayer(bool activate)
    {
        _playerController.enabled = activate;
        _playerVisuals.SetActive(activate);

        if (activate)
        {
            // restore initial movement settings after no-fuel
            _playerController.RuntimeSettings.forwardSpeed           = _initForward;
            _playerController.RuntimeSettings.horizontalSpeed        = _initHorizontalSpeed;
            _playerController.RuntimeSettings.horizontalDeceleration = _initHorizontalDeceleration;
            _playerController.RuntimeSettings.jumpHeight             = _initJump;
            _playerController.RuntimeSettings.turningAngle           = _initTurn;

            IsDead = false;
        }
        else
        {
            if (_rigidbody)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
        }
    }

    private void NoFuelEffect()
    {
        _playerController.RuntimeSettings.forwardSpeed = 0f;
        _playerController.RuntimeSettings.horizontalSpeed = 0f;
        _playerController.RuntimeSettings.horizontalDeceleration = _playerController.RuntimeSettings.forwardDeceleration;
        _playerController.RuntimeSettings.jumpHeight = 0f;
        _playerController.RuntimeSettings.turningAngle = 0f;
    }
}
