// ParticleEffectSpec.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Particle Effect Spec")]
public class ParticleEffectSpec : ScriptableObject
{
    [Header("Addressing")]
    [SerializeField] private string anchorId = "BoostTrail";
    [SerializeField] private ParticleScope scope = ParticleScope.UseSource;

    [Header("Behavior")]
    [SerializeField] private ParticlePlayMode playMode = ParticlePlayMode.OneShot;
    [SerializeField] private ParticleStopMode stopMode = ParticleStopMode.StopEmitting;

    [Header("Tag & policy (matches camera lane)")]
    [SerializeField] private string tag = "Speed";
    [SerializeField] private bool replaceExisting = true;

    [Header("Spam control")]
    [Min(0)] [SerializeField] private float cooldownSeconds = 0f;
    [Min(0)] [SerializeField] private int maxActivePerOwner = 1;
    [SerializeField] private bool restartLoopIfAlreadyPlaying = false;

    [Header("Magnitude routing (multiplies if constants)")]
    [Min(0.01f)] [SerializeField] private float sizeMultiplierPerMagnitude = 1f;
    [Min(0.01f)] [SerializeField] private float speedMultiplierPerMagnitude = 1f;
    [Min(0.01f)] [SerializeField] private float rateMultiplierPerMagnitude = 1f;

    // ——— Exposed read-only props ———
    public string AnchorId => anchorId;
    public ParticleScope Scope => scope;
    public ParticlePlayMode PlayMode => playMode;
    public ParticleStopMode StopMode => stopMode;
    public string Tag => tag;
    public bool ReplaceExisting => replaceExisting;
    public float CooldownSeconds => cooldownSeconds;
    public int MaxActivePerOwner => maxActivePerOwner;
    public bool RestartLoopIfAlreadyPlaying => restartLoopIfAlreadyPlaying;

    public void ComputeMultipliers(float magnitude, out float size, out float speed, out float rate)
    {
        size = Mathf.Max(0.0001f, Mathf.Pow(sizeMultiplierPerMagnitude, magnitude));
        speed = Mathf.Max(0.0001f, Mathf.Pow(speedMultiplierPerMagnitude, magnitude));
        rate = Mathf.Max(0.0001f, Mathf.Pow(rateMultiplierPerMagnitude, magnitude));
    }
}
