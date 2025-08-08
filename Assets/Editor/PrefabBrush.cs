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
    static bool _isPainting;                       // dragging with LMB
    static float _paintY;                          // locked Y plane during drag
    static Vector3 _lastCell = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
    // 🆕 cache the first-hit normal for this drag
    static Vector3 _lockedNormal = Vector3.up;
    static readonly HashSet<Vector3> _placedThisDrag = new(); // avoid duplicates within one drag

    #endregion

    #region Initialization

    static PrefabBrush()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    static void OnSceneGUI(SceneView sceneView)
    {
        HandleContinuousPaintInput(); // 🆕 replaces click-only flow
        GhostPreviewLogic();
        if(!_currentBrushPrefab) return;
        SceneView.RepaintAll();
    }

    #endregion

    #region Painting Logic

    // 🆕 Input handler for MouseDown/Drag/Up to keep painting across grid cells
    static void HandleContinuousPaintInput()
    {
        if (_currentBrushPrefab == null) return;

        Event e = Event.current;

        // Begin drag: lock the plane Y at the first hit and place immediately
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, Physics.AllLayers, QueryTriggerInteraction.Collide))
            {
                _isPainting = true;
                _placedThisDrag.Clear();
                
                // 🆕 cache the drag normal (dominant axis so it’s stable)
                _lockedNormal = KeepLargestComponent(hit.normal).normalized;
                if (_lockedNormal == Vector3.zero) _lockedNormal = Vector3.up;

                // Lock Y to the snapped height at the click location
                _paintY = SnapToFreePlace(hit.point, hit.normal, gridSize).y;
                _lastCell = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

                TryPlaceAtMouseOnLockedPlane(placeImmediately: true, isFirstPlace: true);
                e.Use();
            }
        }

        // Drag: keep painting when we enter a new snapped cell on the locked plane
        if (_isPainting && e.type == EventType.MouseDrag && e.button == 0)
        {
            TryPlaceAtMouseOnLockedPlane();
            e.Use();
        }

        // End drag
        if (_isPainting && e.type == EventType.MouseUp && e.button == 0)
        {
            _isPainting = false;
            _placedThisDrag.Clear();
            e.Use();
        }
    }

    // 🆕 Intersect mouse with plane at _paintY, snap, and place if entering new cell
    static void TryPlaceAtMouseOnLockedPlane(bool placeImmediately = false, bool isFirstPlace = false)
    {
        Plane plane = new Plane(Vector3.up, new Vector3(0f, _paintY, 0f));
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        if (!plane.Raycast(ray, out float dist)) return;
        Vector3 pointOnPlane = ray.GetPoint(dist);

        Vector3 snapped = isFirstPlace ? SnapToFreePlace(pointOnPlane, _lockedNormal, gridSize)
            : SnapToGrid(pointOnPlane,gridSize);
        snapped.y = _paintY;

        if (!placeImmediately && ApproximatelyCell(snapped, _lastCell)) return;
        if (_placedThisDrag.Contains(snapped)) return;

        PlaceCurrentBrushPrefab(snapped);
        _lastCell = snapped;
        _placedThisDrag.Add(snapped);
    }

    // 🆕 Centralized placement that also handles parenting by subfolder
    static void PlaceCurrentBrushPrefab(Vector3 position)
    {
        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(_currentBrushPrefab);
        obj.transform.position = position;

        // Parent under subfolder group in hierarchy
        string prefabSubFolder = EnvironmentPrefabWindow.PrefabToSubFolder(CurrentBrushPrefab); // assumes static method
        GameObject parent = GetOrCreateParent(prefabSubFolder);
        obj.transform.SetParent(parent.transform);

        Undo.RegisterCreatedObjectUndo(obj, "Paint Platform");
    }

    // 🆕 Get existing parent GO or create one and cache it
    static GameObject GetOrCreateParent(string name)
    {
        if (string.IsNullOrEmpty(name)) name = "Ungrouped";

        if (!PrefabsParents.TryGetValue(name, out GameObject parent) || parent == null)
        {
            parent = GameObject.Find(name);
            if (parent == null)
            {
                parent = new GameObject(name);
                parent.transform.position = Vector3.zero;
                Undo.RegisterCreatedObjectUndo(parent, "Create " + name);
            }
            PrefabsParents[name] = parent;
        }

        return parent;
    }

    // 🆕 compare snapped cells with tiny tolerance
    static bool ApproximatelyCell(Vector3 a, Vector3 b)
    {
        const float eps = 0.0001f;
        return Mathf.Abs(a.x - b.x) < eps &&
               Mathf.Abs(a.y - b.y) < eps &&
               Mathf.Abs(a.z - b.z) < eps;
    }

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
                ghostMats[i] = ghostMat;

            renderer.sharedMaterials = ghostMats;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    #endregion

    #region Snapping Logic

    static Vector3 SnapToGrid(Vector3 pos, Vector3 grid)
    {
        return new Vector3(
            Mathf.Round(pos.x / grid.x) * grid.x,
            Mathf.Round(pos.y / grid.y) * grid.y,
            Mathf.Round(pos.z / grid.z) * grid.z
        );
    }

    static Vector3 SnapToFreePlace(Vector3 pos, Vector3 normal, Vector3 grid)
    {
        Vector3 newPos = SnapToGrid(pos, grid);

        if (!Physics.CheckBox(newPos, grid * 0.4f, quaternion.identity, Physics.AllLayers, QueryTriggerInteraction.Collide))
            return newPos;

        Vector3 newNormal = KeepLargestComponent(normal).normalized;
        Vector3 addedDistance = Vector3.Scale(grid, newNormal);
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
