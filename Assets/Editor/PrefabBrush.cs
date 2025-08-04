using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[InitializeOnLoad]
public static class PrefabBrush
{
    private static GridPlacementData placementData;
    
    static GameObject previewInstance; // 🆕 ghost object in the scene
    static GameObject currentBrushPrefab;
    static Vector3 gridSize = new(2f, 0.5f, 2f);
    static PrefabBrush()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    static void OnSceneGUI(SceneView sceneView)
    {
        PaintBrush();
        SceneView.RepaintAll();
    }

    static void PaintBrush()
    {
        Event e = Event.current;

        if(!currentBrushPrefab) return;
        GhostPreviewLogic();
        
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit)) return;
            
            Vector3 spawnPosition = SnapToFreePlace(hit.point, hit.normal, gridSize);
                
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(currentBrushPrefab);
            obj.transform.position = spawnPosition;
            Undo.RegisterCreatedObjectUndo(obj, "Paint Platform");
            e.Use(); // consume event
        }
    }

    static void GhostPreviewLogic()
    {
        // -- Preview ghost placement --

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 ghostPosition = SnapToFreePlace(hit.point, hit.normal, gridSize);

            // Prevent duplicate ghost objects or old ones lingering
            if (previewInstance == null || previewInstance.name != currentBrushPrefab.name + "_Ghost")
            {
                if (previewInstance != null)
                    Object.DestroyImmediate(previewInstance);

                previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(currentBrushPrefab);
                previewInstance.name = currentBrushPrefab.name + "_Ghost";
                previewInstance.hideFlags = HideFlags.HideAndDontSave;
                SetGhostMaterial(previewInstance);
            }

            previewInstance.transform.position = ghostPosition;
        }
        else
        {
            if (previewInstance != null)
            {
                Object.DestroyImmediate(previewInstance);
                previewInstance = null;
            }
        }

    }
    
    static void SetGhostMaterial(GameObject ghost)
    {
        foreach (var renderer in ghost.GetComponentsInChildren<Renderer>())
        {
            Material[] ghostMats = new Material[renderer.sharedMaterials.Length];

            for (int i = 0; i < ghostMats.Length; i++)
            {
                Material original = renderer.sharedMaterials[i];
                if (original == null) continue;

                // Clone the original material
                Material ghostMat = new Material(original);
                ghostMat.name = "GhostMaterial";

                // ✅ URP transparency setup
                ghostMat.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
                ghostMat.SetOverrideTag("RenderType", "Transparent");
                ghostMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                ghostMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                ghostMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                ghostMat.SetInt("_ZWrite", 0);

                // Enable alpha blending keyword
                ghostMat.DisableKeyword("_ALPHATEST_ON");
                ghostMat.EnableKeyword("_ALPHABLEND_ON");
                ghostMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

                // ✅ URP-specific keyword
                ghostMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

                // Set color alpha
                Color col = ghostMat.color;
                col.a = 0.5f; // visible but transparent
                ghostMat.color = col;

                ghostMats[i] = ghostMat;
                
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            renderer.sharedMaterials = ghostMats;
        }
    }



    static void EraseBrush()
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 1 && !e.alt && currentBrushPrefab)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit)) return;
            
            Vector3 spawnPosition = SnapToGrid(hit.point, gridSize);
            
            
        }
    }

    
    static Vector3 SnapToGrid(Vector3 pos, Vector3 gridSize)
    {
        return new Vector3(
            Mathf.Round(pos.x / gridSize.x) * gridSize.x,
            Mathf.Round(pos.y / gridSize.y) * gridSize.y,
            Mathf.Round(pos.z / gridSize.z) * gridSize.z
        );
    }

    static Vector3 SnapToFreePlace(Vector3 pos, Vector3 normal, Vector3 gridSize)
    {
        return SnapToGrid(pos + KeepLargestComponent(normal).normalized * gridSize.y* 0.5f , gridSize);
    }
    
    static Vector3 KeepLargestComponent(Vector3 v)
    {
        float max = Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        return new Vector3(
            Mathf.Approximately(Mathf.Abs(v.x), max) ? v.x : 0f,
            Mathf.Approximately(Mathf.Abs(v.y), max) ? v.y : 0f,
            Mathf.Approximately(Mathf.Abs(v.z), max) ? v.z : 0f
        );
    }


    public static void SetBrushPrefab(GameObject prefab)
    {
        currentBrushPrefab = prefab;
    }

    public static void SnapInstanceToGrid(GameObject instance)
    {
       instance.transform.position = SnapToGrid(instance.transform.position, gridSize);
    }

    public static void ResetBrushPrefab()
    {
        currentBrushPrefab = null;
    }
}