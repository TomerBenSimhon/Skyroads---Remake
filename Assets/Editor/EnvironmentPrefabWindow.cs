using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class EnvironmentPrefabWindow : EditorWindow
{
    private Vector2 _scrollPosition;
    private readonly List<string> _subFolders = new();
    
    private readonly Dictionary<string, bool> _foldoutsState = new();
    private readonly Dictionary<string, List<GameObject>> _prefabsPerFolder = new(); // ✅ New: cache prefabs per folder

    [MenuItem("Tools/Environment Prefabs Window")]
    public static void ShowWindow()
    {
        GetWindow<EnvironmentPrefabWindow>("Environment Prefabs");
    }

    private void OnEnable()
    {
        ReloadAll();
    }

    private void ReloadAll() // ✅ New: central reload method
    {
        ListSubfolderPaths();
        LoadFoldoutStates();
        LoadPrefabsPerFolder(); // ✅ Load per-folder prefabs once
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void ListSubfolderPaths(string loadPath = "Level Assets")
    {
        _subFolders.Clear();
        
        string fullPath = Path.Combine(Application.dataPath, loadPath);
        
        if (Directory.Exists(fullPath))
        {
            string[] subfolderPaths = Directory.GetDirectories(fullPath);

            foreach (string path in subfolderPaths)
            {
                string relativePath = "Assets" + path.Replace(Application.dataPath, "").Replace('\\', '/');
                _subFolders.Add(relativePath);
            }
        }
        else
        {
            Debug.LogWarning($"Folder not found: {loadPath}");
        }
    }

    private void LoadFoldoutStates()
    {
        _foldoutsState.Clear();

        foreach (string subfolder in _subFolders)
        {
            _foldoutsState.TryAdd(subfolder, false); // 🆕 Changed: use full path as key
        }
    }
    

    private void LoadPrefabsPerFolder() // ✅ New: loads all subfolder prefabs once
    {
        _prefabsPerFolder.Clear();

        foreach (string folderPath in _subFolders)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
            List<GameObject> prefabs = new();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                    prefabs.Add(prefab);
            }

            _prefabsPerFolder[folderPath] = prefabs;
        }
    }

    private void OnGUI()
    {
        if (_prefabsPerFolder == null || _prefabsPerFolder.Count == 0)
        {
            EditorGUILayout.LabelField("No prefabs found in Assets/Level Assets");
            if (GUILayout.Button("Reload Prefabs"))
            {
                ReloadAll(); // 🆕 uses central reload
            }
            return;
        }

        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button("Reset Prefab Brush"))
            PrefabBrush.ResetBrushPrefab();
        EditorGUILayout.EndHorizontal();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        foreach (string subFolderPath in _subFolders)
        {
            string folderName = Path.GetFileName(subFolderPath);

            _foldoutsState[subFolderPath] = EditorGUILayout.Foldout(_foldoutsState[subFolderPath], folderName, true, EditorStyles.foldoutHeader); // 🆕 Changed key

            if (_foldoutsState[subFolderPath])
            {
                if (_prefabsPerFolder.TryGetValue(subFolderPath, out List<GameObject> prefabs)) // ✅ New: use cached prefabs
                {
                    foreach (GameObject prefab in prefabs)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        EditorGUILayout.BeginHorizontal("box");
                        GUILayout.Label(AssetPreview.GetAssetPreview(prefab) ?? AssetPreview.GetMiniThumbnail(prefab), GUILayout.Width(64), GUILayout.Height(64));
                        EditorGUILayout.BeginVertical();
                        GUILayout.Label(prefab.name, EditorStyles.boldLabel);

                        if (GUILayout.Button("Place in Scene"))
                        {
                           // PlacePrefabInScene(prefab, SceneView.lastActiveSceneView.pivot);
                           PrefabBrush.SetBrushPrefab(prefab);
                        }

                        if (GUILayout.Button("Place in Scene (0, 0, 0)"))
                        {
                            PlacePrefabInScene(prefab, Vector3.zero);
                        }

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No prefabs found in folder.", MessageType.Info);
                }
            }

            EditorGUILayout.Space(); // ✅ Optional: add space between foldouts
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Reload Prefabs"))
        {
            ReloadAll(); // 🆕 uses central reload
        }
    }

    private void PlacePrefabInScene(GameObject prefab, Vector3 pos)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        PrefabBrush.SetBrushPrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instance, "Place Prefab");
        instance.transform.position = pos;
        PrefabBrush.SnapInstanceToGrid(instance);
        Selection.activeGameObject = instance;
    }
}
