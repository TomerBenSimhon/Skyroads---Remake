using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    // Pose
    private Vector3 _spawnPoint;
    private Quaternion _spawnRotation = Quaternion.identity;

    // Flags
    private bool _hasCheckpoint;

    public bool HasCheckpoint => _hasCheckpoint;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Called by checkpoint trigger OR initial seeding from Player
    public void SetSpawnPoint(Vector3 pos, Quaternion rot)
    {
        _spawnPoint = pos;
        _spawnRotation = rot;
        _hasCheckpoint = true;
    }

    // Consumed by the new Player in Awake
    public bool TryConsumeRespawn(out Vector3 pos, out Quaternion rot)
    {
        if (_hasCheckpoint)
        {
            pos = _spawnPoint;
            rot = _spawnRotation;
            return true;
        }
        pos = default;
        rot = default;
        return false;
    }

    public void ResetCheckpoint()
    {
        _hasCheckpoint = false;
    }
}
