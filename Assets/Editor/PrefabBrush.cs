using System;
using System.Collections.Generic;
using Unity.Mathematics;
using System.Linq;
using Codice.Client.BaseCommands;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PrefabBrush
{
    #region Fields

    private static LayerMask _paintingLayer = LayerMask.GetMask("Painting Surface");

    static GameObject previewInstance;
    private static GameObject _currentBrushPrefab;
    public static GameObject CurrentBrushPrefab => _currentBrushPrefab;

    public static Dictionary<string, GameObject> PrefabsParents = new();

    static Vector3 gridSize = new(2f, 0.5f, 2f);
    static readonly string ghostMaterialPath = "Assets/Editor/Ghost_mat.mat";

    // 🆕 Continuous paint state
    static bool _isPainting;                       // dragging with LMB
    static float _paintY = -1000f;                          // locked Y plane during drag
    static Vector3 _lastCell = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
    // 🆕 cache the first-hit normal for this drag
    static Vector3 _lockedNormal = Vector3.up;
    static readonly HashSet<Vector3> _placedThisDrag = new(); // avoid duplicates within one drag
    
    // 🆕 Rectangle tool state
    private static bool _isRectDragging;
    private static Vector3 _rectStartCell;
    private static readonly List<GameObject> _rectGhosts = new(); // pooled ghosts for area preview
    private static GameObject _tempPaintingSurface;
    
    private static GameObject _paintingSurface;
    
    
    public enum BrushMode {Paint, Line, Rectangle}
    private static BrushMode _mode = BrushMode.Paint;
    public static BrushMode Mode => _mode;

    #endregion
    

    #region Initialization

    static PrefabBrush()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        DestroyGhost();
        CleanupTempSurfaces();
        
    }

    static void OnSceneGUI(SceneView sceneView)
    {
        switch (_mode)
        {
            case BrushMode.Paint:
                HandleContinuousPaintInput();
                if (!_isPainting)
                {
                    GhostPreviewLogic();
                    break;
                }
                DestroyGhost();
                break;
            
            case BrushMode.Line:
                HandleContinuousPaintInput(); //to do
                break;
            
            case BrushMode.Rectangle:
                HandleRectToolInput();
                if (!_isRectDragging)
                {
                    GhostPreviewLogic();
                    break;
                }
                DestroyGhost();
                break;
        }
        
        HandlePaintingSurfaceInput();
        
        if (_mode != BrushMode.Rectangle || !_isRectDragging)
            CleanupTempSurfaces();
        
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
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _paintingLayer, QueryTriggerInteraction.Collide))
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
            else
            {
                _paintY = -1000f;
                return;
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

    static void HandleLineToolInput()
    {
         /* coming next */
    }

    private static void HandleRectToolInput()
    {
        if (CurrentBrushPrefab == null) return;

        Event e = Event.current;
        // MouseDown: record start, lock Y, begin drag
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _paintingLayer, QueryTriggerInteraction.Collide))
            {
                _isRectDragging = true;
                
                // lock plane Y using your existing free-place snap
                Vector3 start = SnapToFreePlace(hit.point, hit.normal, gridSize);
                _paintY = start.y;
                _rectStartCell = new Vector3(
                    Mathf.Round(start.x / gridSize.x) * gridSize.x,
                    _paintY,
                    Mathf.Round(start.z / gridSize.z) * gridSize.z
                );

                _tempPaintingSurface = CreatePaintingSurfaceAtPosition(new Vector3(_rectStartCell.x, _paintY - gridSize.y, _rectStartCell.z));
                
                e.Use();
            }
            else
            {
                _paintY = -1000f;
                return;
            }
        }

        // MouseDrag: update ghosts to fill rectangle from start to current snapped cell
        if (_isRectDragging && e.type == EventType.MouseDrag && e.button == 0)
        {
            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity, _paintingLayer, QueryTriggerInteraction.Collide))
            {
                var current = SnapToGrid(hit.point, gridSize);
                current.y = _paintY;

                var cells = GetRectCells(_rectStartCell, current, gridSize);
                UpdateRectGhosts(cells);

                e.Use();
            }
        }

        // MouseUp: place at all cells, then clear ghosts
        if (_isRectDragging && e.type == EventType.MouseUp && e.button == 0)
        {
            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity, _paintingLayer, QueryTriggerInteraction.Collide))
            {
                var endCell = SnapToGrid(hit.point, gridSize);
                endCell.y = _paintY;

                var cells = GetRectCells(_rectStartCell, endCell, gridSize);

                // place all (skip occupied if needed)
                foreach (var cell in cells)
                {
                    // same occupancy style you use in paint:
                    if (Physics.CheckBox(cell, gridSize * 0.4f, Quaternion.identity, _paintingLayer, QueryTriggerInteraction.Collide))
                        continue;

                    PlaceCurrentBrushPrefab(cell);
                }
            }

            ClearRectGhosts();
            _isRectDragging = false;
            
            Object.DestroyImmediate(_tempPaintingSurface);
            _tempPaintingSurface = null;
            
            e.Use();
        }
    }

    static void HandlePaintingSurfaceInput()
    {
        if (CurrentBrushPrefab == null) return;
        if(_paintingSurface == null) return;
        
        Event e = Event.current;

        if (e.type == EventType.ScrollWheel)
        {
            if (e.delta.y > 0)
            {
                _paintingSurface.transform.position -= Vector3.up * gridSize.y;
            }

            if (e.delta.y < 0)
            {
                _paintingSurface.transform.position += Vector3.up * gridSize.y;
            }
            
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

        if (Physics.CheckBox(snapped, gridSize * 0.4f, quaternion.identity, _paintingLayer,
                QueryTriggerInteraction.Collide))
            return;

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
        GameObject parent = GetOrCreateParent(EnvironmentPrefabWindow.ParentQuery);
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
    
    // Build the grid of cells between two snapped corners (inclusive)
    private static List<Vector3> GetRectCells(Vector3 a, Vector3 b, Vector3 step)
    {
        var cells = new List<Vector3>(64);

        float minX = Mathf.Min(a.x, b.x);
        float maxX = Mathf.Max(a.x, b.x);
        float minZ = Mathf.Min(a.z, b.z);
        float maxZ = Mathf.Max(a.z, b.z);
        float y = a.y; // both are on the locked Y

        // iterate by grid step to cover all snapped cells
        for (float x = minX; x <= maxX + 0.001f; x += step.x)
        {
            for (float z = minZ; z <= maxZ + 0.001f; z += step.z)
            {
                cells.Add(new Vector3(
                    Mathf.Round(x / step.x) * step.x,
                    y,
                    Mathf.Round(z / step.z) * step.z
                ));
            }
        }

        return cells;
    }

    // Make sure we have enough pooled ghosts, then place/update them
    private static void UpdateRectGhosts(List<Vector3> cells)
    {
        EnsureRectGhostPool(cells.Count);

        // enable and position needed ghosts
        for (int i = 0; i < cells.Count; i++)
        {
            var g = _rectGhosts[i];
            if (g == null)
            {
                _rectGhosts[i] = CreateGhostForPool();
                g = _rectGhosts[i];
            }

            g.hideFlags = HideFlags.DontSave;
            g.SetActive(true);
            g.transform.position = cells[i];
        }

        // disable any extra ghosts in the pool
        for (int i = cells.Count; i < _rectGhosts.Count; i++)
        {
            if (_rectGhosts[i] != null)
                _rectGhosts[i].SetActive(false);
        }
    }

    private static void EnsureRectGhostPool(int needed)
    {
        while (_rectGhosts.Count < needed)
        {
            _rectGhosts.Add(CreateGhostForPool());
        }
    }

    private static GameObject CreateGhostForPool()
    {
        if (CurrentBrushPrefab == null) return null;

        var g = (GameObject)PrefabUtility.InstantiatePrefab(CurrentBrushPrefab);
        g.name = CurrentBrushPrefab.name + "_Ghost"; // consistent
        g.hideFlags = HideFlags.DontSave;

        foreach (var col in g.GetComponentsInChildren<Collider>())
            col.enabled = false;

        SetGhostMaterial(g);
        g.SetActive(false);
        return g;
    }

    private static void ClearRectGhosts()
    {
        for (int i = 0; i < _rectGhosts.Count; i++)
        {
            if (_rectGhosts[i] != null)
                Object.DestroyImmediate(_rectGhosts[i]);
        }
        _rectGhosts.Clear();
    }


    #endregion

    #region Ghost Preview Logic

    static void GhostPreviewLogic()
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _paintingLayer, QueryTriggerInteraction.Collide))
        {
            DestroyGhost();
            return;
        }

        if (!_currentBrushPrefab)
        {
            DestroyGhost();
            return;
        }

        Debug.Log(hit.collider.gameObject.name);
        Vector3 ghostPosition = SnapToFreePlace(hit.point, hit.normal, gridSize);

        if (previewInstance == null || previewInstance.name != _currentBrushPrefab.name + "_Ghost")
        {
            CreateGhostInstance();
        }

        previewInstance.transform.position = ghostPosition;
        
        // …then show it (only now do we activate)
        if (!previewInstance.activeSelf)
            previewInstance.SetActive(true);
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
        
        previewInstance.SetActive(false);
    }

    static void DestroyGhost()
    {
        if (previewInstance != null)
        {
            Object.DestroyImmediate(previewInstance);
            previewInstance = null;
            return;
        }

        foreach (var ghost in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (ghost.name.EndsWith("_Ghost"))
            {
                Object.DestroyImmediate(ghost);
            }
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

        if (!Physics.CheckBox(newPos, grid * 0.4f, quaternion.identity, _paintingLayer, QueryTriggerInteraction.Collide))
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
        ClearRectGhosts();
    }

    public static bool IsCurrentBrush(GameObject prefab)
    {
        return CurrentBrushPrefab == prefab;
    }

    public static void ResetBrushPrefab()
    {
        _currentBrushPrefab = null;
        DestroyGhost();
        ClearRectGhosts();
        CleanupTempSurfaces();
    }

    public static void SetMode(BrushMode mode)
    {
        _mode = mode;
        DestroyGhost();
        ClearRectGhosts();
        CleanupTempSurfaces();
    }
    public static void LoadPaintingSurfaces()
    {
        _paintingSurface = GameObject.Find("PaintingSurface");
        if(_paintingSurface == null)
            Debug.LogError("No painting surface found");
    }

    static void CleanupTempSurfaces()
    {
        if (_tempPaintingSurface != null)
        {
            Object.DestroyImmediate(_tempPaintingSurface);
            _tempPaintingSurface = null;
        }

        // Also nuke any that might have survived domain reloads:
        var all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var go in all)
        {
            if (go != null && go.name == "PaintingSurface_Temp")
                Object.DestroyImmediate(go);
        }
    }
    
    public static GameObject CreatePaintingSurfaceAtPosition(Vector3 position)
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "PaintingSurface_Temp";
        plane.transform.position = position;
        plane.transform.localScale = Vector3.one * 1000f;
        plane.layer = LayerMask.NameToLayer("Painting Surface");

        // invisible + not saved
        var renderer = plane.GetComponent<MeshRenderer>();
        if (renderer)
            renderer.enabled = false;

        //plane.hideFlags = HideFlags.HideAndDontSave;
        return plane;
    }


    #endregion
}
