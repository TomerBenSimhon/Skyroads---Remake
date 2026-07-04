using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ParticleController : MonoBehaviour
{
    [SerializeField] private ParticleSystem ps;
    private Coroutine stopRoutine;

    private void Awake()
    {
        if (!ps) ps = GetComponentInChildren<ParticleSystem>(true);
        if (!ps) Debug.LogError($"[ParticleController] No ParticleSystem found on {name}", this);
    }

    public void PlayOneShot()
    {
        if (!ps) return;
        ps.Play(true);
    }

    public void PlayLoop(float durationSeconds = -1f)
    {
        if (!ps) return;
        var main = ps.main;
        main.loop = true;
        ps.Play(true);

        if (stopRoutine != null) { StopCoroutine(stopRoutine); stopRoutine = null; }
        if (durationSeconds > 0f)
            stopRoutine = StartCoroutine(AutoStopAfter(durationSeconds));
    }

    public void Stop(bool clear = false)
    {
        if (!ps) return;
        if (stopRoutine != null) { StopCoroutine(stopRoutine); stopRoutine = null; }
        ps.Stop(true, clear ? ParticleSystemStopBehavior.StopEmittingAndClear
            : ParticleSystemStopBehavior.StopEmitting);
    }

    private IEnumerator AutoStopAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Stop(false);
        stopRoutine = null;
    }
}