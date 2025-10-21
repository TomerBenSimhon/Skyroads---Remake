using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class RefuelCoils : MonoBehaviour
{
    
    [SerializeField] Transform refuelCoilTop;
    [SerializeField] private float range = 3f;
    [SerializeField] private float motionDistance = 0.5f;
    [SerializeField] private float motionSpeed = 2f;
    
    
    private Transform _player;
    private PlatformDetection _playerPlatform;
    private Vector3 _startPosition;
    private Vector3 _endPosition;
    public bool _initialized;
    public bool debug;

    private void Awake()
    {
        _player = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include).transform;
        if(_player)
            _playerPlatform = _player.GetComponent<PlatformDetection>();
        
        _startPosition = refuelCoilTop.position;
        _endPosition = _startPosition + Vector3.up * motionDistance;
    }

    private void Update()
    {
       MoveCoilTop();
    }

    void MoveCoilTop()
    {
        if (!_player) return;
        
        Vector3 target = _startPosition;

        bool isPlayerNearAndRefueling = _playerPlatform.CurrentPlatformType == PlatformType.Refuel &&
                                        Vector3.Distance(refuelCoilTop.position, _player.position) <= range;

        if (isPlayerNearAndRefueling )
            target = _endPosition;
        
        refuelCoilTop.position = Vector3.Lerp(refuelCoilTop.position, target, motionSpeed * Time.deltaTime);
        if(debug) Debug.Log(isPlayerNearAndRefueling);

        //event logic
        if (isPlayerNearAndRefueling && !_initialized)
        {
            _initialized = true;
            GlobalEvents.Raise(GlobalEvents.Id.CoilActivated, gameObject);
        }
        else if(!isPlayerNearAndRefueling && _initialized)
        {
            _initialized = false;
            GlobalEvents.Raise(GlobalEvents.Id.CoilDeactivated, gameObject);
        }
    }
}
