using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class EnvironmentPrefabWindow : EditorWindow
{
    private Vector2 _scrollPosition;

    private static string _searchQuery = "";
    private static string _lastSearchQuery = "";

    private readonly List<string> _subFolders = new();
    private  Dictionary<string, bool> _foldoutsState = new();
    private static readonly Dictionary<string, List<GameObject>> _prefabsPerFolder = new();
    private readonly Dictionary<Object, Texture2D> _previewCache = new();

    [MenuItem("Tools/Environment Prefabs Window")]
    public static void ShowWindow()
    {
        GetWindow<EnvironmentPrefabWindow>("Environment Prefabs");
    }

    private void OnEnable() => ReloadAll();

    #region Reload / Load Logic

    private void ReloadAll()
    {
        _previewCache.Clear();
        ListSubfolderPaths();
        LoadFoldoutStates();
        LoadPrefabsPerFolder();
    }

    private void ListSubfolderPaths(string loadPath = "Level Assets")
    {
        _subFolders.Clear();
        string fullPath = Path.Combine(Application.dataPath, loadPath);

        if (!Directory.Exists(fullPath))
        {
            Debug.LogWarning($"Folder not found: {loadPath}");
            return;
        }

        foreach (string path in Directory.GetDirectories(fullPath))
        {
            string relativePath = "Assets" + path.Replace(Application.dataPath, "").Replace('\\', '/');
            _subFolders.Add(relativePath);
        }
    }

    private void LoadFoldoutStates()
    {
        _foldoutsState.Clear();
        foreach (string subfolder in _subFolders)
            _foldoutsState[subfolder] = false;
    }

    private void LoadPrefabsPerFolder()
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

    #endregion

    #region GUI

    private void OnGUI()
    {
        if (_prefabsPerFolder.Count == 0)
        {
            EditorGUILayout.LabelField("No prefabs found in Assets/Level Assets");
            if (GUILayout.Button("Reload Prefabs")) ReloadAll();
            return;
        }

        DrawToolbar();
        HandleSearchCleared();
        DrawPrefabScrollView();

        if (GUILayout.Button("Reload Prefabs")) ReloadAll();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        _searchQuery = GUILayout.TextField(_searchQuery, GUI.skin.FindStyle("ToolbarSearchTextField"));
        if (GUILayout.Button("x", GUI.skin.FindStyle("ToolbarSearchCancelButton"))) _searchQuery = "";
        EditorGUILayout.EndHorizontal();

        if(!PrefabBrush.CurrentBrushPrefab) return;
        if (GUILayout.Button("Reset Prefab Brush"))
            PrefabBrush.ResetBrushPrefab();
    }

    private void HandleSearchCleared()
    {
        bool searchCleared = !string.IsNullOrEmpty(_lastSearchQuery) && string.IsNullOrEmpty(_searchQuery);
        _lastSearchQuery = _searchQuery;

        if (!searchCleared) return;

        List<string> keys = new(_foldoutsState.Keys);
        foreach (string key in keys)
            _foldoutsState[key] = false;
    }

    private void DrawPrefabScrollView()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        foreach (string subFolderPath in _subFolders)
        {
            string folderName = Path.GetFileName(subFolderPath);
            bool showFoldout = !string.IsNullOrEmpty(_searchQuery) || _foldoutsState[subFolderPath];

            _foldoutsState[subFolderPath] = EditorGUILayout.Foldout(
                showFoldout, folderName, true, EditorStyles.foldoutHeader);

            if (!_foldoutsState[subFolderPath]) continue;

            if (_prefabsPerFolder.TryGetValue(subFolderPath, out List<GameObject> prefabs))
            {
                foreach (GameObject prefab in prefabs)
                {
                    if (!string.IsNullOrEmpty(_searchQuery) &&
                        !prefab.name.ToLower().Contains(_searchQuery.ToLower()))
                        continue;

                    DrawPrefabEntry(prefab);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No prefabs found in folder.", MessageType.Info);
            }

            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawPrefabEntry(GameObject prefab)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(20);
        EditorGUILayout.BeginHorizontal("box");

        Texture2D preview = GetCachedPreview(prefab);
        GUILayout.Label(preview, GUILayout.Width(64), GUILayout.Height(64));

        EditorGUILayout.BeginVertical();
        GUILayout.Label(prefab.name, EditorStyles.boldLabel);

        DrawPrefabSelectButton(prefab);

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPrefabSelectButton(GameObject prefab)
    {
        Color originalColor = GUI.backgroundColor;

        if (PrefabBrush.IsCurrentBrush(prefab))
            GUI.backgroundColor = new Color(1f, 0.85f, 0.3f);

        if (GUILayout.Button("Select " + prefab.name))
            PrefabBrush.SetBrushPrefab(prefab);

        GUI.backgroundColor = originalColor;
    }

    #endregion

    #region Preview

    private Texture2D GetCachedPreview(GameObject prefab)
    {
        if (_previewCache.TryGetValue(prefab, out Texture2D cachedPreview) && cachedPreview != null)
            return cachedPreview;

        Texture2D newPreview = AssetPreview.GetAssetPreview(prefab);
        if (newPreview != null)
        {
            _previewCache[prefab] = newPreview;
            return newPreview;
        }

        Repaint(); // keep repainting until preview is ready
        return AssetPreview.GetMiniThumbnail(prefab); // fallback

    }

    #endregion
    
    #region Public API

    public static string PrefabToSubFolder(GameObject prefab)
    {
        foreach (var pair in _prefabsPerFolder)
        {
            if (pair.Value.Contains(prefab))
            {
                // Get only the last part of the path (the folder name)
                return Path.GetFileName(pair.Key);
            }
        }

        return "Random objects";
    }

    
    #endregion
}
