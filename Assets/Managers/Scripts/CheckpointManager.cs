using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    // Pose
    private Vector3 _spawnPoint;
    private Quaternion _spawnRotation = Quaternion.identity;

    // Snapshot of gameplay state (per component)
    private readonly Dictionary<string, object> _snapshot = new();

    // Flags
    private bool _hasCheckpoint;
    private bool _respawnPending;

    public bool HasCheckpoint => _hasCheckpoint;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Called by checkpoint trigger OR initial seeding from Player
    public void SetSpawnPoint(Vector3 pos, Quaternion rot, GameObject player = null)
    {
        _spawnPoint = pos;
        _spawnRotation = rot;
        _hasCheckpoint = true;

        if (player != null)
            SaveStateFrom(player);
    }

    public void SaveStateFrom(GameObject player)
    {
        _snapshot.Clear();
        var savables = player.GetComponentsInChildren<ICheckpointSavable>(true);
        foreach (var s in savables)
            _snapshot[s.SaveKey] = s.CaptureState();
    }

    public void ApplySavedStateTo(GameObject player)
    {
        if (_snapshot.Count == 0) return;
        var savables = player.GetComponentsInChildren<ICheckpointSavable>(true);
        foreach (var s in savables)
            if (_snapshot.TryGetValue(s.SaveKey, out var data))
                s.RestoreState(data);
    }

    // Called BEFORE LoadScene
    public void MarkRespawnPending()
    {
        _respawnPending = true;
    }

    // Consumed by the new Player in Awake
    public bool TryConsumeRespawn(out Vector3 pos, out Quaternion rot)
    {
        if (_respawnPending && _hasCheckpoint)
        {
            _respawnPending = false;
            pos = _spawnPoint;
            rot = _spawnRotation;
            return true;
        }
        pos = default;
        rot = default;
        return false;
    }
}
