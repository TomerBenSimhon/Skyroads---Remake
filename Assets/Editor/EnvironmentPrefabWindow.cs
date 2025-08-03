using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class EnvironmentPrefabWindow : EditorWindow
{
    private Vector2 _scrollPosition;
    private List<GameObject> _allPrefabs = new();
    private List<GameObject> _subPrefabs = new();
    private List<string> _subFolders= new();
    
    private Dictionary<string, bool> _foldoutsState = new();
    private Dictionary<string, List<GameObject>> _prefabsPerFolder = new();
    

    [MenuItem("Tools/Environment Prefabs Window")]
    public static void ShowWindow()
    {
        GetWindow<EnvironmentPrefabWindow>("Environment Prefabs");
    }

    private void OnEnable()
    {
        LoadPrefabs();
        ListSubfolderPaths();
        LoadFoldoutStates();
    }

    private void ListSubfolderPaths()
    {
        _subFolders.Clear();
        
        string targetPath = "Assets/Level Assets"; // Change to your target folder
        string fullPath = Path.Combine(Application.dataPath, targetPath.Substring("Assets/".Length));

        if (Directory.Exists(fullPath))
        {
            string[] subfolderPaths = Directory.GetDirectories(fullPath);

            Debug.Log($"Subfolder paths in '{targetPath}':");

            foreach (string path in subfolderPaths)
            {
                // Convert absolute path back to Unity-relative path
                string relativePath = "Assets" + path.Replace(Application.dataPath, "").Replace('\\', '/');
                _subFolders.Add(relativePath);
                Debug.Log(relativePath);
            }
        }
        else
        {
            Debug.LogWarning($"Folder not found: {targetPath}");
        }
    }

    private void LoadFoldoutStates()
    {
        _foldoutsState.Clear();
        
        List<string> subfolderNames = new();
        foreach (string subfolder in _subFolders)
        {
            string name = Path.GetFileName(subfolder);
            subfolderNames.Add(name);
        }
        
        foreach (string label in subfolderNames)
        {
            _foldoutsState.TryAdd(label, false);
        }
    }

    private void LoadPrefabs(string[] loadPath = null)
    {
        if (loadPath == null)
            loadPath = new[] { "Assets/Level Assets" };
        
        _allPrefabs.Clear();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", loadPath);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                _allPrefabs.Add(prefab);
        }
    }
    
    private void LoadSubPrefabs(string[] loadPath = null)
    {
        if (loadPath == null)
            loadPath = new[] { "Assets/Level Assets" };
        
        _subPrefabs.Clear();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", loadPath);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                _subPrefabs.Add(prefab);
        }
    }

    private void OnGUI()
    {
        if (_allPrefabs == null || _allPrefabs.Count == 0)
        {
            EditorGUILayout.LabelField("No prefabs found in Assets/Level Assets");
            if (GUILayout.Button("Reload Prefabs"))
            {
                LoadPrefabs();
                ListSubfolderPaths();
                LoadFoldoutStates();
            }
            return;
        }
        
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        foreach (string subFolderPath in _subFolders)
        {
            LoadSubPrefabs(new[] { subFolderPath });
            string folderName = Path.GetFileName(subFolderPath);
            
            _foldoutsState[folderName] = EditorGUILayout.Foldout(_foldoutsState[folderName], folderName);

            if (_foldoutsState[folderName])
            {
                foreach (GameObject prefab in _subPrefabs)
                {
                    EditorGUILayout.BeginHorizontal("box");
                    GUILayout.Label(AssetPreview.GetAssetPreview(prefab) ?? AssetPreview.GetMiniThumbnail(prefab), GUILayout.Width(64), GUILayout.Height(64));
                    EditorGUILayout.BeginVertical();
                    GUILayout.Label(prefab.name, EditorStyles.boldLabel);
                    if (GUILayout.Button("Place in Scene"))
                    {
                        PlacePrefabInScene(prefab,SceneView.lastActiveSceneView.pivot);
                    }

                    if (GUILayout.Button("Place in Scene (0, 0, 0)"))
                    {
                        PlacePrefabInScene(prefab, Vector3.zero);
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        
        EditorGUILayout.EndScrollView();
        
        if (GUILayout.Button("Reload Prefabs"))
        {
            LoadPrefabs();
            ListSubfolderPaths();
            LoadFoldoutStates();
        }
    }
    
    private void PlacePrefabInScene(GameObject prefab, Vector3 pos)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instance, "Place Prefab");
        instance.transform.position = pos;
        Selection.activeGameObject = instance;
    }
}

