using UnityEngine;
using System.Collections;
using Material = UnityEngine.Material;

public class BoostArrows : MonoBehaviour
{
    [Header("Arrows & Effects")]
    public Renderer arrow;             // Assign each arrow's Renderer

    [Header("Colors & Timing")]
    public Color idleColor = Color.red;
    public Color activeColor = Color.yellow;
    public float interval = 0.2f;         // Time between arrow activations

    [Header("Punch Settings")]
    public float punchScale = 1.2f;       // How big the punch gets
    public float punchDuration = 0.1f;    // How fast the punch in/out is

    [Header("Glow Settings")]
    public bool useSineGlow = true;       // Toggle idle arrows glowing
    public float glowSpeed = 2f;          // How fast the glow pulses
    public float glowMin = 0.5f;          // Minimum brightness multiplier
    public float glowMax = 1.5f;          // Maximum brightness multiplier

    private int currentIndex;
    private PlayerController _controller;
    private bool _hasPlayed = false;
    

    void Awake()
    {
        // Cache per-arrow materials so we can tweak Emission
        arrow.material = new Material(arrow.material);
        
        _controller = FindFirstObjectByType<PlayerController>().GetComponent<PlayerController>();
    }
    
    void Update()
    { 
        Vector3 origin = transform.position + _controller.RuntimeSettings.centerOffset;
        Vector3 halfExtents = _controller.RuntimeSettings.halfExtents;
        float distance = _controller.RuntimeSettings.groundHeight;
        
        bool detected = Physics.BoxCast(origin, halfExtents, Vector3.up, out _, Quaternion.identity, distance, LayerMask.GetMask("Player"), QueryTriggerInteraction.Collide);

        if (detected && !_hasPlayed)
        {
            StartCoroutine(PlaySequence());
            _hasPlayed = true;
        }
            
        
        if (!useSineGlow)
            return;

        // Pulse idle arrows
        float sine = Mathf.Sin(Time.time * glowSpeed) * 0.5f + 0.5f;
        float intensity = Mathf.Lerp(glowMin, glowMax, sine);
        
        arrow.material.SetColor("_EmissionColor", idleColor * intensity);
        
    }
    
    IEnumerator PlaySequence()
    {
        // 2. Punch scale
        StartCoroutine(Punch(arrow.transform));

        // 3. Light up arrow
        arrow.material.SetColor("_EmissionColor", activeColor);

        // 4. Wait then revert
        yield return new WaitForSeconds(interval);
        arrow.material.SetColor("_EmissionColor", idleColor);
        
    }

    IEnumerator Punch(Transform t)
    {
        Vector3 original = t.localScale;
        Vector3 target = original * punchScale;
        float timer = 0f;

        // Scale up
        while (timer < punchDuration)
        {
            t.localScale = Vector3.Slerp(original, target, timer / punchDuration);
            Vector3 resetZ = t.localScale;
            resetZ.z = original.z;
            t.localScale = resetZ;
            timer += Time.deltaTime;
            yield return null;
        }

        // Scale down
        timer = 0f;
        while (timer < punchDuration)
        {
            t.localScale = Vector3.Slerp(target, original, timer / punchDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        t.localScale = original;
    }
}
