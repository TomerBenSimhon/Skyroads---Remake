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
        PowerUpApplied = 1 << 8, FixApplied = 1 << 9
    }

    public static event Action<Id, GameObject> Triggered;
    public static event Action<Id, GameObject> Cancelled;

    public static void Trigger(Id id, GameObject sender = null)  => Triggered?.Invoke(id, sender);
    public static void Cancel(Id id, GameObject sender = null)   => Cancelled?.Invoke(id, sender);
}