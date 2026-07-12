using UnityEngine;
using UnityEditor;

public class CreatePaintingSurface : EditorWindow
{
    [MenuItem("Tools/Create Painting Plane")]
    private static void CreatePaintingPlane()
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plane.name = "PaintingSurface";
        plane.transform.position = Vector3.zero;
        Vector3 scale = new Vector3(10000f, 1, 10000f);
        plane.transform.localScale = scale;
        plane.layer = LayerMask.NameToLayer("Painting Surface");
        plane.GetComponent<MeshRenderer>().enabled = false;
    }
    
    public static GameObject CreatePaintingSurfaceAtPosition(Vector3 position)
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "PaintingSurface_Temp";
        plane.transform.position = position;
        Vector3 scale = new Vector3(10000f, 1, 10000f);
        plane.transform.localScale = scale;
        plane.layer = LayerMask.NameToLayer("Painting Surface");

        // invisible + not saved
        var renderer = plane.GetComponent<MeshRenderer>();
        if (renderer)
            renderer.enabled = false;

        plane.hideFlags = HideFlags.DontSave;
        return plane;
    }

}
