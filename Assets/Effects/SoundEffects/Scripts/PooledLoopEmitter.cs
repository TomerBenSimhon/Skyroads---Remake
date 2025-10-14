using UnityEngine;

public class PooledLoopEmitter : MonoBehaviour
{
    private AudioSource _src;
    private Transform _followTarget;
    private AudioManager _owner;
    private bool _inUse;

    public void Init(AudioManager owner, AudioSource src)
    {
        _owner = owner;
        _src = src;
        _src.playOnAwake = false;
        gameObject.SetActive(false);
    }

    public void Begin(SfxClip asset, Transform followTarget, float initialVolume, float pitch)
    {
        // Mixer + spatial config from asset
        _src.outputAudioMixerGroup = asset.mixerGroup;
        if (asset.is3D)
        {
            _src.spatialBlend = 1f;
            _src.rolloffMode  = asset.rolloff;
            _src.minDistance  = Mathf.Max(0.01f, asset.minDistance);
            _src.maxDistance  = Mathf.Max(_src.minDistance + 0.01f, asset.maxDistance);
            _followTarget = followTarget;
        }
        else
        {
            _src.spatialBlend = 0f;
            _followTarget = null;
            transform.localPosition = Vector3.zero;
        }

        _src.loop = true;
        _src.clip = asset.clip;
        _src.pitch = pitch;
        _src.volume = initialVolume;

        _inUse = true;
        gameObject.SetActive(true);
        _src.Play();
    }

    public void End()
    {
        _inUse = false;
        _src.Stop();
        gameObject.SetActive(false);
        _owner.ReturnLoopEmitter(this);
    }

    private void LateUpdate()
    {
        if (_followTarget) transform.position = _followTarget.position;
    }

    public bool IsFree => !_inUse && !gameObject.activeSelf;
    public AudioSource Source => _src;
}