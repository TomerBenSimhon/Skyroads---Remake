using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio/Sfx Clip", fileName = "SfxClip")]
public class SfxClip : ScriptableObject
{
    public AudioClip clip;
    public AudioMixerGroup mixerGroup;

    [Header("Defaults")]
    [Range(0f,1f)] public float volume = 1f;
    public float pitch = 1f;

    [Header("Default Space")]
    public bool is3D = false;                         // default; spec can override
    public float minDistance = 1f;
    public float maxDistance = 25f;
    public AudioRolloffMode rolloff = AudioRolloffMode.Logarithmic;
}