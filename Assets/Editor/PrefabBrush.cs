
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class PrefabBrush
{
    #region Fields

    static GameObject previewInstance;
    private static GameObject _currentBrushPrefab;
    public static GameObject CurrentBrushPrefab => _currentBrushPrefab;
    
    public static Dictionary<string, GameObject> PrefabsParents = new();

    static Vector3 gridSize = new(2f, 0.5f, 2f);
    static readonly string ghostMaterialPath = "Assets/Editor/Ghost_mat.mat";
    
    // 🆕 Continuous paint state
    private static bool _isPainting;                       // dragging with LMB
    private static float _paintY;                          // locked Y plane during drag
    private static Vector3 _lastCell = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
    private static readonly HashSet<Vector3> _placedThisDrag = new(); // avoid duplicates within one drag

    #endregion

    #region Initialization

    static PrefabBrush()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    static void OnSceneGUI(SceneView sceneView)
    {
        GhostPreviewLogic();
        HandleContinuousPaintInput();
        SceneView.RepaintAll();
    }

    #endregion

    #region Painting Logic

    static void HandleContinuousPaintInput()
    {
        if (!_currentBrushPrefab) return;
        
        Event e = Event.current;
        if (!IsPaintClick(e)) return;

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, Physics.AllLayers, QueryTriggerInteraction.Collide))
            return;

        Vector3 spawnPosition = SnapToFreePlace(hit.point, hit.normal, gridSize);
        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(_currentBrushPrefab);
        obj.transform.position = spawnPosition;

        string prefabSubFolder = EnvironmentPrefabWindow.PrefabToSubFolder(CurrentBrushPrefab);
        if (!PrefabsParents.TryGetValue(prefabSubFolder, out GameObject parent) || parent == null)
        {
            // Try to find existing object in the scene
            parent = GameObject.Find(prefabSubFolder);
            if (parent == null)
            {
                // If not found, create a new empty GameObject
                parent = new GameObject(prefabSubFolder);
                parent.transform.position = Vector3.zero; // Optional: place at origin
                Undo.RegisterCreatedObjectUndo(parent, "Create " + parent.name);
            }

            PrefabsParents[prefabSubFolder] = parent;
        }
        obj.transform.SetParent(parent.transform);
        
        Undo.RegisterCreatedObjectUndo(obj, "Paint Platform");

        e.Use(); // consume event
    }

    static bool IsPaintClick(Event e) => e.type == EventType.MouseDown && e.button == 0 && !e.alt;

    #endregion

    #region Ghost Preview Logic

    static void GhostPreviewLogic()
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, Physics.AllLayers, QueryTriggerInteraction.Collide))
        {
            DestroyGhost();
            return;
        }

        if (!_currentBrushPrefab)
        {
            DestroyGhost();
            return;
        }

        Vector3 ghostPosition = SnapToFreePlace(hit.point, hit.normal, gridSize);

        if (previewInstance == null || previewInstance.name != _currentBrushPrefab.name + "_Ghost")
        {
            CreateGhostInstance();
        }

        previewInstance.transform.position = ghostPosition;
    }

    static void CreateGhostInstance()
    {
        DestroyGhost();

        previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(_currentBrushPrefab);
        previewInstance.name = _currentBrushPrefab.name + "_Ghost";
        previewInstance.hideFlags = HideFlags.DontSave;

        foreach (var collider in previewInstance.GetComponentsInChildren<Collider>())
            collider.enabled = false;

        SetGhostMaterial(previewInstance);
    }

    static void DestroyGhost()
    {
        if (previewInstance != null)
        {
            Object.DestroyImmediate(previewInstance);
            previewInstance = null;
        }
    }

    static void SetGhostMaterial(GameObject ghost)
    {
        Material ghostMat = AssetDatabase.LoadAssetAtPath<Material>(ghostMaterialPath);

        if (ghostMat == null)
        {
            Debug.LogWarning("Ghost material not found at: " + ghostMaterialPath);
            return;
        }

        foreach (var renderer in ghost.GetComponentsInChildren<Renderer>())
        {
            int matCount = renderer.sharedMaterials.Length;
            Material[] ghostMats = new Material[matCount];

            for (int i = 0; i < matCount; i++)
            {
                ghostMats[i] = ghostMat;
            }

            renderer.sharedMaterials = ghostMats;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    #endregion

    #region Snapping Logic

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
        Vector3 newPos = SnapToGrid(pos, gridSize);

        if (!Physics.CheckBox(newPos, gridSize * 0.4f, quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Collide))
            return newPos;

        Vector3 newNormal = KeepLargestComponent(normal).normalized;
        Vector3 addedDistance = Vector3.Scale(gridSize, newNormal);
        newPos += addedDistance;

        return newPos;
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

    #endregion

    #region Public API

    public static void SetBrushPrefab(GameObject prefab)
    {
        _currentBrushPrefab = prefab;
        CreateGhostInstance();
    }

    public static bool IsCurrentBrush(GameObject prefab)
    {
        return CurrentBrushPrefab == prefab;
    }

    public static void ResetBrushPrefab()
    {
        _currentBrushPrefab = null;
        DestroyGhost();
    }

    #endregion
}
