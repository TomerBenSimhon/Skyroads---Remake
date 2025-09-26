using System;
using Unity.VisualScripting;
using UnityEngine;

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

    private void Awake()
    {
        _playerPlatform = FindAnyObjectByType<PlatformDetection>();
        _player = _playerPlatform.transform;
        
        _startPosition = refuelCoilTop.position;
        _endPosition = _startPosition + Vector3.up * motionDistance;
    }

    private void Update()
    {
        if (!_player) return;
        
        Vector3 target = _startPosition;

        if (_playerPlatform.CurrentPlatformType == PlatformType.Refuel && Vector3.Distance(refuelCoilTop.position, _player.position) <= range )
            target = _endPosition;
        
        refuelCoilTop.position = Vector3.Lerp(refuelCoilTop.position, target, motionSpeed * Time.deltaTime);
    }
}
