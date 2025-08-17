using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class BarrierDetection : MonoBehaviour
{
    [SerializeField] LayerMask barrierLayer;
    
    [Header("Box cast info")]
    [SerializeField] Vector3 barrierCastCenterOffset;
    [SerializeField] Vector3 barrierCastHalfExtents;
    [SerializeField] float barrierDetectionDistance;
    
    private PlayerDeath _playerDeath;

    private void Awake()
    {
        _playerDeath = GetComponent<PlayerDeath>();
    }

    void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) & barrierLayer) == 0) return;

        Vector3 center = transform.position + barrierCastCenterOffset;
        if(Physics.BoxCast(center, barrierCastHalfExtents, Vector3.forward, Quaternion.identity, barrierDetectionDistance, barrierLayer, QueryTriggerInteraction.Collide))    
            _playerDeath.Die();
    }
    
    #if UNITY_EDITOR

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector3 center = transform.position + barrierCastCenterOffset;
        center.z += barrierDetectionDistance;
        Gizmos.DrawCube(center , barrierCastHalfExtents);
    }
    
    #endif
}
