using System.Collections;
using UnityEngine;

public class BoostPlatform : MonoBehaviour, IPlatformEffect
{
    private PlatformDetection platformDetection;
    
    [Header("Modifiers")]
    [Range(1f,2f)] [Tooltip("multiply forward velocity by this number at the start of the boost")] 
    public float initialImpulseModifier;
    [Range(1f,5f)] [Tooltip("how many times can the forward velocity be multiplied")]
    public int boostStackCount = 2;
    [Range(1f,5f)] [Tooltip("multiply groundSpringStrength by this number at the start of the boost")]
    public float springModifier;
    [Range(1f,5f)] [Tooltip("multiply jumpHeight by this number at the start of the boost")]
    public float jumpModifier;
    
    [Header("Boost Settings")]
    [Range(1f,100f)] [Tooltip("overrides forward speed by this number")] 
    public float boostSpeed = 20f;
    [Range(1f,300f)] [Tooltip("overrides forward acceleration by this number")] 
    public float boostAcceleration = 20f;
    [Range(0.1f,2f)]public float boostDuration = 0.5f;
    
    
    // a coroutine has to start and stop from the same instance 
    // thats why we are passing in the PlatformDetection instance so we can start and stop the boostCoroutine from there
    public void Apply(PlayerController player, Rigidbody rb, PlatformDetection runner, ref Coroutine boostCoroutine)
    {
        runner.SetPlatform(PlatformType.Boost);
        
        if (boostCoroutine != null)
        {
            runner.StopCoroutine(boostCoroutine);
            boostCoroutine = null;
        }
        
        Vector3 boost = rb.linearVelocity;
        boost.z = rb.linearVelocity.z * initialImpulseModifier;
        boost.z = Mathf.Clamp(boost.z, 0f, player.DefaultSettings.forwardSpeed * initialImpulseModifier * boostStackCount);
        rb.linearVelocity = boost.z > rb.linearVelocity.z ? boost : rb.linearVelocity;
        
        player.RuntimeSettings.forwardAcceleration = boostAcceleration;
        player.RuntimeSettings.forwardSpeed = boostSpeed;
        player.RuntimeSettings.groundSpringStrength = springModifier * player.DefaultSettings.groundSpringStrength;
        player.RuntimeSettings.jumpHeight = jumpModifier * player.DefaultSettings.jumpHeight;
    }

    public void Remove(PlayerController player, PlatformDetection runner, ref Coroutine boostCoroutine)
    {
        boostCoroutine = runner.StartCoroutine(StopBoost(player, boostDuration, runner));
    }

    IEnumerator StopBoost(PlayerController player, float duration, PlatformDetection runner)
    {
        yield return new WaitForSeconds(duration);
        
        player.RuntimeSettings.forwardAcceleration = player.DefaultSettings.forwardAcceleration;
        player.RuntimeSettings.forwardSpeed = player.DefaultSettings.forwardSpeed;
        player.RuntimeSettings.groundSpringStrength = player.DefaultSettings.groundSpringStrength;
        player.RuntimeSettings.jumpHeight = player.DefaultSettings.jumpHeight;
        
        if(runner.CurrentPlatformType == PlatformType.Boost)
            runner.SetPlatform(PlatformType.None);
    }
}
