using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

[DisallowMultipleComponent]
public class PlayerRespawnOnAwake : MonoBehaviour
{
    private void Awake()
    {
        var cm = CheckpointManager.Instance;
        if (cm == null) return;

        // Seed a baseline once (first time only)
        if (!cm.HasCheckpoint)
            cm.SetSpawnPoint(transform.position, transform.rotation, gameObject);

        // Consume respawn request (if any)
        if (cm.TryConsumeRespawn(out var pos, out var rot))
        {
            // 1) Restore gameplay state first (can enable scripts & run their OnEnable/Start)
            cm.ApplySavedStateTo(gameObject);

            // 2) Teleport after state is applied
            Teleport(pos, rot);
            print(pos);
        }
    }

    private void Teleport(Vector3 pos, Quaternion rot, bool snapCam = true)
    {
        Vector3 oldPos = transform.position;
        
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            bool wasKin = rb.isKinematic;
            
            rb.isKinematic = true;
            rb.position = pos;
            rb.rotation = rot;
            transform.SetPositionAndRotation(pos, rot);
            
            rb.isKinematic = wasKin;
        }
        else
        {
            transform.SetPositionAndRotation(pos, rot);
        }
        
        if (!snapCam) return;
        CinemachineCore.OnTargetObjectWarped(gameObject.transform, transform.position - oldPos);
    }

}