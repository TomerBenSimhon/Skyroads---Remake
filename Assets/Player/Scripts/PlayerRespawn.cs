using UnityEngine;
using Unity.Cinemachine;

[DisallowMultipleComponent]
public class PlayerRespawn : MonoBehaviour
{
    private PlayerDeath _playerDeath;
    private PlayerFuel _playerFuel;

    void Awake()
    {
        _playerDeath = GetComponent<PlayerDeath>();
        _playerFuel = GetComponent<PlayerFuel>();
    }

    private void Start()
    {
        var cm = CheckpointManager.Instance;
        if (cm == null) return;

        // Seed a baseline spawn once
        if (!cm.HasCheckpoint)
        {
            cm.SetSpawnPoint(transform.position, transform.rotation);

            // Use the player's configured starting fuel as first checkpoint fuel
            if (_playerFuel) _playerFuel.SetCheckpointFuel(_playerFuel.startingFuel);
        }
    }

    public void Respawn()
    {
        var cm = CheckpointManager.Instance;
        if (cm == null) return;

        if (cm.TryConsumeRespawn(out var pos, out var rot))
        {
            Teleport(pos, rot);
        }

        _playerDeath.ActivatePlayer(true);
        GlobalEvents.Raise(GlobalEvents.Id.PlayerRespawned, gameObject);
    }

    private void Teleport(Vector3 pos, Quaternion rot, bool snapCam = true)
    {
        var oldPos = transform.position;

        if (TryGetComponent<Rigidbody>(out var rb))
        {
            bool wasKin = rb.isKinematic;

            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.position = pos;
            rb.rotation = rot;
            transform.SetPositionAndRotation(pos, rot);

            rb.isKinematic = wasKin;
        }
        else
        {
            transform.SetPositionAndRotation(pos, rot);
        }

        if (snapCam)
        {
            CinemachineCore.OnTargetObjectWarped(transform, transform.position - oldPos);
        }
    }
}