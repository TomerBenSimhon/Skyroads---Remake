// GlobalEvents.cs (static, no MonoBehaviour needed)
using System;
using UnityEngine;

public static class GlobalEvents
{
    public enum Id { BoostApplied, BoostRemoved }

    public static event Action<Id, GameObject> Triggered;
    public static event Action<Id, GameObject> Cancelled;

    public static void Trigger(Id id, GameObject sender)  => Triggered?.Invoke(id, sender);
    public static void Cancel(Id id, GameObject sender)   => Cancelled?.Invoke(id, sender);
}