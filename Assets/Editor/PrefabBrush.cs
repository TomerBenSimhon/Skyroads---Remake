using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[InitializeOnLoad]
public static class PrefabBrush
{
    static GameObject currentBrushPrefab;
    static Vector3 gridSize = new Vector3(2f, 0.5f, 2f);

    private static HashSet<Vector3> _occupiedPositions = new HashSet<Vector3>();

    static PrefabBrush()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    static void OnSceneGUI(SceneView sceneView)
    {
        PaintLogic();
    }

    static void PaintLogic()
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && currentBrushPrefab)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit)) return;
            
            Vector3 spawnPosition = SnapToGrid(hit.point, gridSize);
            int i = 1;
            while (_occupiedPositions.Contains(spawnPosition))
            {
                i++;
                spawnPosition += KeepLargestComponent(hit.normal).normalized * 0.25f * i;
                spawnPosition = SnapToGrid(spawnPosition, gridSize);
            }
                
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(currentBrushPrefab);
            obj.transform.position = spawnPosition;
            _occupiedPositions.Add(spawnPosition);
            Undo.RegisterCreatedObjectUndo(obj, "Paint Platform");
            e.Use(); // consume event
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
    
    static Vector3 KeepLargestComponent(Vector3 v)
    {
        float max = Mathf.Max(v.x, v.y, v.z);

        return new Vector3(
            v.x == max ? v.x : 0f,
            v.y == max ? v.y : 0f,
            v.z == max ? v.z : 0f
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