using System.Collections;
using UnityEngine;

public class ArrowSequence : MonoBehaviour
{
    [Header("Arrows & Effects")]
    public Renderer[] arrows;             // Assign each arrow’s Renderer
    public ParticleSystem[] bursts;       // Assign each arrow’s ParticleSystem

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

    private Material[] materials;         // Instanced materials for emission
    private int currentIndex;

    void Awake()
    {
        // Cache per-arrow materials so we can tweak Emission
        materials = new Material[arrows.Length];
        for (int i = 0; i < arrows.Length; i++)
        {
            materials[i] = arrows[i].material;
            materials[i].SetColor("_EmissionColor", idleColor);
        }
    }

    void Start()
    {
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        currentIndex = 0;

        while (true)
        {
            // 1. Fire particle burst (if assigned)
            if (bursts != null && bursts.Length == arrows.Length)
                bursts[currentIndex].Play();

            // 2. Punch scale
            StartCoroutine(Punch(arrows[currentIndex].transform));

            // 3. Light up arrow
            materials[currentIndex].SetColor("_EmissionColor", activeColor);

            // 4. Wait then revert
            yield return new WaitForSeconds(interval);
            materials[currentIndex].SetColor("_EmissionColor", idleColor);

            // 5. Move to next
            currentIndex = (currentIndex + 1) % arrows.Length;
        }
    }

    IEnumerator Punch(Transform t)
    {
        Vector3 original = t.localScale;
        Vector3 target = original * punchScale;
        float timer = 0f;

        // Scale up
        while (timer < punchDuration)
        {
            t.localScale = Vector3.Lerp(original, target, timer / punchDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        // Scale down
        timer = 0f;
        while (timer < punchDuration)
        {
            t.localScale = Vector3.Lerp(target, original, timer / punchDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        t.localScale = original;
    }

    void Update()
    {
        if (!useSineGlow)
            return;

        // Pulse idle arrows
        float sine = Mathf.Sin(Time.time * glowSpeed) * 0.5f + 0.5f;
        float intensity = Mathf.Lerp(glowMin, glowMax, sine);

        for (int i = 0; i < materials.Length; i++)
        {
            if (i == currentIndex)
                continue;

            materials[i].SetColor("_EmissionColor", idleColor * intensity);
        }
    }
}