using System.Collections;
using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    [Range(0f, 5f)] public float deathTimer;
    [Range(0f, 5f)] public float noFuelTimer;
    
    [Header("Dissolve Settings")]
    [Range(0f, 2f)] public float dissolveTimer;
    [Range(0f, 2f)] public float dissolveDelay_death;
    [Range(0f, 2f)] public float dissolveDelay_noFuel;

    public bool IsDead { get; private set; }

    private PlayerController _playerController;
    private Rigidbody _rigidbody;
    private GameObject _playerVisuals;
    private Renderer _renderer;

    // Snapshot of initial runtime settings so we can restore after "no fuel"
    private float _initForward;
    private float _initHorizontalSpeed;
    private float _initHorizontalDeceleration;
    private float _initJump;
    private float _initTurn;

    private Coroutine _dissolveCR;

    void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        _rigidbody = GetComponent<Rigidbody>();
        _playerVisuals = transform.Find("Visuals").gameObject;
        _renderer = GetComponentInChildren<Renderer>();
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
        if (_dissolveCR == null) _dissolveCR = StartCoroutine(Dissolve(dissolveDelay_death));
        GameManager.Instance.RestartLevel(deathTimer);
        GlobalEvents.Raise(GlobalEvents.Id.PlayerDied);
    }

    public void DieNoFuel()
    {
        if (IsDead) return;
        IsDead = true;
        NoFuelEffect();
        if (_dissolveCR == null) _dissolveCR = StartCoroutine(Dissolve(dissolveDelay_noFuel));
        GameManager.Instance.RestartLevel(noFuelTimer);
        GlobalEvents.Raise(GlobalEvents.Id.PlayerDied);
    }

    public void ActivatePlayer(bool activate)
    {
        _playerController.enabled = activate;

        if (activate)
        {
            // restore initial movement settings after no-fuel
            _playerController.RuntimeSettings.forwardSpeed           = _initForward;
            _playerController.RuntimeSettings.horizontalSpeed        = _initHorizontalSpeed;
            _playerController.RuntimeSettings.horizontalDeceleration = _initHorizontalDeceleration;
            _playerController.RuntimeSettings.jumpHeight             = _initJump;
            _playerController.RuntimeSettings.turningAngle           = _initTurn;

            IsDead = false;
            
            if(_dissolveCR != null) StopCoroutine(_dissolveCR);
            _dissolveCR = null;
            
            _renderer.material.SetFloat("_Dissolve", 0);
            _renderer.material.SetFloat("_EdgeWidth", 0);
            _renderer.material.SetFloat("_OutlineEnabled", 1f);
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

    private IEnumerator Dissolve(float delay = 0.2f)
    {
        _renderer.material.SetFloat("_OutlineEnabled", 0f);
        yield return new WaitForSeconds(delay);

        float t = 0f;
        while (t < dissolveTimer)
        {
            t += Time.deltaTime;
            _renderer.material.SetFloat("_Dissolve", t / dissolveTimer);
            _renderer.material.SetFloat("_EdgeWidth", t / dissolveTimer / 2.5f);
            yield return null;
        }
        
        _dissolveCR = null;
    }
}
