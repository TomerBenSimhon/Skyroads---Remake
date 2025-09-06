// ParticleAnchor.cs
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ParticleAnchor : MonoBehaviour
{
    [SerializeField] private string anchorId = "BoostTrail";
    [Tooltip("Optional. If set, this anchor belongs to this specific owner (e.g., the Boost pad GameObject).")]
    [SerializeField] private GameObject owner;

    [Header("Auto-wire")]
    [Tooltip("If true, all ParticleSystems in this GameObject's hierarchy will be controlled.")]
    [SerializeField] private bool includeChildren = true;

    // Cached systems
    [SerializeField] private List<ParticleSystem> _systems = new();

    public string AnchorId => anchorId;
    public GameObject Owner => owner;

    void OnEnable()
    {
        CacheSystems();
        ParticleEffectsManager.I?.Register(this);
    }

    void OnDisable()
    {
        ParticleEffectsManager.I?.Unregister(this);
    }
    
    void Start() { ParticleEffectsManager.I?.Register(this); }


    void CacheSystems()
    {
        _systems.Clear();
        if (includeChildren)
            GetComponentsInChildren(true, _systems);
        else
        {
            var ps = GetComponent<ParticleSystem>();
            if (ps) _systems.Add(ps);
        }
    }

    // ——— Controls ———

    public void PlayOneShot(float sizeMul, float speedMul, float rateMul)
    {
        // Try to minimally scale common parameters if they are constants
        foreach (var ps in _systems)
        {
            var main = ps.main;
            var emission = ps.emission;

            // Size
            if (main.startSize.mode == ParticleSystemCurveMode.Constant)
                main.startSize = main.startSize.constant * sizeMul;

            // Speed
            if (main.startSpeed.mode == ParticleSystemCurveMode.Constant)
                main.startSpeed = main.startSpeed.constant * speedMul;

            // Rate over time
            if (emission.rateOverTime.mode == ParticleSystemCurveMode.Constant)
                emission.rateOverTime = emission.rateOverTime.constant * rateMul;

            ps.Clear(true);
            ps.Play(true);
        }
    }

    public void StartLoop(float sizeMul, float speedMul, float rateMul, bool restartIfPlaying)
    {
        foreach (var ps in _systems)
        {
            var main = ps.main;
            var emission = ps.emission;

            if (main.startSize.mode == ParticleSystemCurveMode.Constant)
                main.startSize = main.startSize.constant * sizeMul;

            if (main.startSpeed.mode == ParticleSystemCurveMode.Constant)
                main.startSpeed = main.startSpeed.constant * speedMul;

            if (emission.rateOverTime.mode == ParticleSystemCurveMode.Constant)
                emission.rateOverTime = emission.rateOverTime.constant * rateMul;

            if (ps.isPlaying)
            {
                if (restartIfPlaying)
                {
                    ps.Clear(true);
                    ps.Play(true);
                }
                // else keep running
            }
            else
            {
                ps.Play(true);
            }
        }
    }

    public void StopLoop(ParticleStopMode mode)
    {
        foreach (var ps in _systems)
        {
            if (mode == ParticleStopMode.Immediate)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            else
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.45f);
        Gizmos.DrawWireSphere(transform.position, 0.2f);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.15f, $"Anchor: {anchorId}");
    }
#endif
}
