
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public enum PlatformType
{
    None, Boost, Slippery, Refuel, Death
}
public class PlatformDetection : MonoBehaviour
{ 
    [field:SerializeField] public PlatformType CurrentPlatformType { get; private set; } = PlatformType.None;
    
    [SerializeField] private LayerMask platformLayer;

    private PlayerController _controller;
    private Rigidbody _rb;
    private GameObject _currentPlatform;
    
    private bool _isOnSpecialPlatform;

    private Coroutine _currentPlatformCoroutine; // for other special platforms to start (example: boostPlatform)
    
    void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        HandlePlatformEnter();
        HandlePlatformExit();
    }

    void HandlePlatformEnter()
    {
        if(!TryDetectPlatform(out RaycastHit hit)) return;                          // checks if we detected a new special platform
        if(!hit.collider.TryGetComponent(out IPlatformEffect newEffect)) return;   
        
        TryRemoveEffect(_currentPlatform);    // If on a different platform, remove old effect                             
        
        _currentPlatform = hit.collider.gameObject;     //assigns _currentPlatform to the new platform and applies the effect 
        newEffect.Apply(_controller, _rb, this, ref _currentPlatformCoroutine);
        _isOnSpecialPlatform = true;
        
        print("Platform detected and applied effect.");
    }

    void HandlePlatformExit()
    {
        if (!_isOnSpecialPlatform) return;       // checks if we are no longer on a special platform
        if(IsStillOnPlatform()) return;
    
        TryRemoveEffect(_currentPlatform);       // removes the effect and removes _currentPlatform
        _currentPlatform = null;
        _isOnSpecialPlatform = false;
        
        print("No Platform detected and removed effect.");
    }
    
    private bool TryDetectPlatform(out RaycastHit hit, float extraDistance = 0f)
    {
        Vector3 origin = transform.position + _controller.RuntimeSettings.centerOffset;
        Vector3 halfExtents = _controller.RuntimeSettings.halfExtents;
        float distance = _controller.RuntimeSettings.groundHeight + extraDistance;

        bool detected = Physics.BoxCast(origin, halfExtents, Vector3.down, out hit, Quaternion.identity, distance, platformLayer, QueryTriggerInteraction.Collide);

        return detected && (_currentPlatform == null || _currentPlatform != hit.collider.gameObject);
    }
    
    private bool IsStillOnPlatform(float extraDistance = 0f)
    {
        Vector3 origin = transform.position + _controller.RuntimeSettings.centerOffset;
        Vector3 halfExtents = _controller.RuntimeSettings.halfExtents;
        float distance = _controller.RuntimeSettings.groundHeight + extraDistance;
        
        bool boxCastHit = Physics.BoxCast(origin, halfExtents, Vector3.down, out RaycastHit hit, Quaternion.identity, distance, platformLayer, QueryTriggerInteraction.Collide);
        if (boxCastHit)
            return hit.collider.gameObject == _currentPlatform;
        
        bool overlapHit = Physics.OverlapBox(origin, halfExtents, Quaternion.identity, platformLayer, QueryTriggerInteraction.Collide).Length > 0;
        return overlapHit;
    }
    
    private void TryRemoveEffect(GameObject platform)
    {
        if (platform != null && platform.TryGetComponent(out IPlatformEffect effect))
        {
            effect.Remove(_controller, this, ref _currentPlatformCoroutine);
        }
    }

    public void SetPlatform(PlatformType newPlatformType)
    {
        CurrentPlatformType = newPlatformType;
    }
}
