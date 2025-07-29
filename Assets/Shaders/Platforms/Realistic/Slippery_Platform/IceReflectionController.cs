using UnityEngine;

[ExecuteAlways]
public class StaticReflectionCamera : MonoBehaviour
{
    public RenderTexture reflectionTexture;
    public Material iceMaterial;

    void LateUpdate()
    {
        if (!reflectionTexture || !iceMaterial) return;

        Camera cam = GetComponent<Camera>();
        if (!cam) return;

        cam.targetTexture = reflectionTexture;
        cam.Render();

        iceMaterial.SetTexture("_DynamicReflectionTex", reflectionTexture);
    }
}
