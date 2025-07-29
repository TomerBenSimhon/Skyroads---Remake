using UnityEngine;

public class DeathPlatform : MonoBehaviour, IPlatformEffect
{
    public void Apply(PlayerController player, Rigidbody unused, PlatformDetection runner, ref Coroutine unused2)
    {
        runner.SetPlatform(PlatformType.Death);
        if (!player.TryGetComponent(out PlayerDeath death)) return;
        death.Die();
    }

    public void Remove(PlayerController player, PlatformDetection runner, ref Coroutine coroutine)
    {
        // nothing here
    }
}
