using UnityEditor;
using UnityEngine;

public interface IPlatformEffect
{
    void Apply(PlayerController player, Rigidbody rb, PlatformDetection runner, ref Coroutine coroutine);
    void Remove(PlayerController player, PlatformDetection runner, ref Coroutine coroutine);
}