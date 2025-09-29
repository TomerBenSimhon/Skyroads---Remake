using System.Collections;
using UnityEngine;

public class SkyBrainShader : MonoBehaviour
{
    [SerializeField] private GlobalEvents.Id triggerMask = GlobalEvents.Id.None;
    
    [Header("Shader Parameters")] 
    float _alphaMin;
    [SerializeField][Range(0, 1)] private float alphaMax;
    
    [Header("Timing Settings")]
    [SerializeField] private float inTime;
    [SerializeField] private float holdTime;
    [SerializeField] private float outTime;
    
    private Material _material;
    private Coroutine _coroutine;

    void Awake()
    {
        _material = GetComponent<Renderer>().material;
        _alphaMin = _material.GetFloat("_Alpha");
    }

    void OnEnable()
    {
        GlobalEvents.Raised += OnGlobalEvent;
    }

    void OnDisable()
    {
        GlobalEvents.Raised -= OnGlobalEvent;
    }

    void OnGlobalEvent(GlobalEvents.Id id, GameObject sender)
    {
        if (triggerMask == GlobalEvents.Id.None) return;
        if ((triggerMask & id) == 0f) return;

        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
            _coroutine = StartCoroutine(AlphaSequence(inTime));
        }
        else
        {
            _coroutine = StartCoroutine(AlphaSequence());
        }
    }

    IEnumerator AlphaSequence(float t = 0f)
    {
        while (t < inTime)
        {
            t += Time.deltaTime;
            float targetAlpha = Mathf.Lerp(_alphaMin, alphaMax, t / inTime);
            _material.SetFloat("_Alpha", targetAlpha);
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(holdTime);
        t = 0f;
        while (t < outTime)
        {
            t += Time.deltaTime;
            float targetAlpha = Mathf.Lerp(alphaMax, _alphaMin, t / outTime);
            _material.SetFloat("_Alpha", targetAlpha);
            yield return new WaitForEndOfFrame();
        }
        
        _coroutine = null;
    }
}
