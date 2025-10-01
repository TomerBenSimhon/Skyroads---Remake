using System;
using UnityEngine;

public static class GlobalEvents
{
    [Flags]
    public enum Id
    {
        None = 0,
        BoostApplied = 1 << 0, BoostRemoved = 1 << 1,
        RefuelApplied = 1 << 2, RefuelRemoved = 1 << 3,
        SlipperyApplied = 1 << 4, SlipperyRemoved = 1 << 5,
        PlayerJumped = 1 << 6, PlayerFired = 1 << 7,
        PowerUpApplied = 1 << 8, FixApplied = 1 << 9,
        CoilActivated = 1 << 10, CoilDeactivated = 1 << 11,
        CheckpointTriggered = 1 << 12, 
        OnAwake = 1 << 13,
        PlayerGrounded = 1 << 14
    }

    // Single neutral event
    public static event Action<Id, GameObject> Raised;

    // Single neutral raise method
    public static void Raise(Id id, GameObject sender = null) => Raised?.Invoke(id, sender);
}