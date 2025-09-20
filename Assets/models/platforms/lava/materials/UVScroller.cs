using UnityEngine;

public class UVScroller : MonoBehaviour
{
    [Header("Scroll Speed (X = horizontal, Y = vertical)")]
    public Vector2 scrollSpeed = new Vector2(0.5f, 0f);

    private Renderer rend;
    private Vector2 uvOffset = Vector2.zero;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend == null)
        {
            Debug.LogWarning("UVScroller: No Renderer found on this GameObject.");
        }
    }

    void Update()
    {
        if (rend == null) return;

        // Move UVs based on time and speed
        uvOffset += scrollSpeed * Time.deltaTime;

        // Keep values between 0 and 1 to avoid overflow
        uvOffset.x = uvOffset.x % 1f;
        uvOffset.y = uvOffset.y % 1f;

        // Apply to material
        rend.material.mainTextureOffset = uvOffset;
    }
}