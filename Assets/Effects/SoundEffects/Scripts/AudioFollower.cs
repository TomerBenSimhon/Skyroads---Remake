using UnityEngine;

/// <summary>
/// Makes an audio emitter follow a target's position (for 3D sounds).
/// </summary>
public class AudioFollower : MonoBehaviour
{
    private Transform _target;

    public void Init(Transform target) => _target = target;

    private void LateUpdate()
    {
        if (!_target) return;
        transform.position = _target.position;
    }
}