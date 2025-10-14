using UnityEngine;
using System.Collections;

public class PooledAudioEmitter : MonoBehaviour
{
    private AudioSource _src;
    private Transform _followTarget;
    private AudioManager _owner;

    [SerializeField] private bool _isInUse;

    public void Init(AudioManager owner, AudioSource src)
    {
        _owner = owner;
        _src = src;
        _src.playOnAwake = false;
        gameObject.SetActive(false);
    }

    public void PlayOneShot(AudioClip clip, float volume, float pitch, bool is3D, Transform followTarget)
    {
        _followTarget = is3D ? followTarget : null;
        _src.volume = volume;
        _src.pitch  = pitch;
        _src.spatialBlend = is3D ? 1f : 0f;
        _isInUse = true;
        gameObject.SetActive(true);

        _src.Stop();
        _src.clip = null;                 // ensure PlayOneShot takes effect
        _src.PlayOneShot(clip);

        StopAllCoroutines();
        StartCoroutine(ReturnWhenDone(Mathf.Max(0.01f, clip.length / Mathf.Abs(_src.pitch))));
    }

    private IEnumerator ReturnWhenDone(float dur)
    {
        // small headroom to be safe
        yield return new WaitForSeconds(dur + 0.05f);
        _isInUse = false;
        gameObject.SetActive(false);
        _owner.ReturnEmitter(this);
    }

    private void LateUpdate()
    {
        if (_followTarget)
            transform.position = _followTarget.position;
    }

    public bool IsFree => !_isInUse && !gameObject.activeSelf;
    public AudioSource Source => _src;
}