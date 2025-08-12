using UnityEngine;
using UnityEditor;

public class CreatePaintingSurface : EditorWindow
{
    [MenuItem("Tools/Create Painting Plane")]
    private static void CreatePaintingPlane()
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "PaintingSurface";
        plane.transform.position = Vector3.zero;
        plane.transform.localScale = Vector3.one * 1000f;
        plane.layer = LayerMask.NameToLayer("Painting Surface");
        plane.GetComponent<MeshRenderer>().enabled = false;
    }

}
