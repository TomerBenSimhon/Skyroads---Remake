using UnityEngine;
using UnityEngine.Serialization;

public class SlipperyPlatform : MonoBehaviour, IPlatformEffect
{
    [Range(0f, 20f)] [Tooltip("overrides horizontal acceleration and change acceleration by this number")] 
    public float acceleration = 10f;
    [Range(0f, 20f)] [Tooltip("overrides horizontal deceleration by this number")]
    public float deceleration = 10f;
    [Range(0f, 20f)] [Tooltip("overrides horizontal speed by this number")]
    public float speed = 10f;
    [Range(0f, 20f)] [Tooltip("overrides forward acceleration by this number")]
    public float forwardAcceleration = 10f;
    public void Apply(PlayerController player, Rigidbody unused, PlatformDetection runner, ref Coroutine coroutine)
    {
        runner.SetPlatform(PlatformType.Slippery);
        
        player.RuntimeSettings.horizontalAcceleration = acceleration;
        player.RuntimeSettings.horizontalChangeAcceleration = acceleration;
        player.RuntimeSettings.horizontalDeceleration = deceleration;
        player.RuntimeSettings.horizontalSpeed = speed;
        player.RuntimeSettings.forwardAcceleration = forwardAcceleration;
        player.RuntimeSettings.forwardDeceleration = forwardAcceleration;
    }

    public void Remove(PlayerController player, PlatformDetection runner, ref Coroutine coroutine)
    {
        if(runner.CurrentPlatformType == PlatformType.Slippery)
            runner.SetPlatform(PlatformType.None);
        
        player.RuntimeSettings.horizontalAcceleration = player.DefaultSettings.horizontalAcceleration;
        player.RuntimeSettings.horizontalChangeAcceleration = player.DefaultSettings.horizontalChangeAcceleration;
        player.RuntimeSettings.horizontalDeceleration = player.DefaultSettings.horizontalDeceleration;
        player.RuntimeSettings.horizontalSpeed = player.DefaultSettings.horizontalSpeed;
        player.RuntimeSettings.forwardAcceleration = player.DefaultSettings.forwardAcceleration;
        player.RuntimeSettings.forwardDeceleration = player.DefaultSettings.forwardDeceleration;
    }
}
