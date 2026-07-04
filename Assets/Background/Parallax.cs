using System;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    [SerializeField] Transform target;
    [Tooltip("1 is 100% follow the target position, 0 means no movement at all")]
    public Vector3 parallax;
    
    private Vector3 _targetLastFramePos;

    void Start()
    {
        _targetLastFramePos = target.position;
    }

    void LateUpdate()
    {
        _targetLastFramePos = target.position;
    }

    void Update()
    {
        Vector3 targetDelta = target.position - _targetLastFramePos;
        Vector3 targetPos = transform.position + new Vector3(targetDelta.x * parallax.x, targetDelta.y * parallax.y, targetDelta.z * parallax.z);
        
        transform.position = targetPos;
    }
}
