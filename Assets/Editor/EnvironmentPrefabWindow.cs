using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Codice.Client.BaseCommands;
using Unity.VisualScripting;
using Object = UnityEngine.Object;

public class EnvironmentPrefabWindow : EditorWindow
{
    public static bool IsWindowOpen;
    
    private Vector2 _scrollPosition;

    private static string _searchQuery = "";
    private static string _lastSearchQuery = "";

    private static string _parentQuery;
    public static string ParentQuery => _parentQuery;
    private const string kParentKey = "EnvPrefabWindow.ParentQuery";

    private readonly List<string> _subFolderPaths = new();
    private  Dictionary<string, bool> _foldoutsState = new();
    private static readonly Dictionary<string, List<GameObject>> _prefabsPerFolder = new();
    private readonly Dictionary<Object, Texture2D> _previewCache = new();
    
    // 🆕 UI state for toolbar selection (mirrors PrefabBrush.Mode)
    private int _toolIndex = 0;
    private static readonly string[] _toolLabels = { "Brush", "Line", "Rectangle" };
    private int _modeIndex = 0;
    private static readonly string[] _modeLabels = { "Paint", "Erase" };


    [MenuItem("Tools/Environment Prefabs Window")]
    public static void ShowWindow()
    {
        GetWindow<EnvironmentPrefabWindow>("Environment Prefabs");
    }

    private void OnEnable()
    {
        _toolIndex = (int)PrefabBrush.Tool;
        _modeIndex = (int)PrefabBrush.Mode;
        _parentQuery = EditorPrefs.GetString(kParentKey, _parentQuery ?? "");
        ReloadAll();
    }

    private void OnDisable()
    {
        EditorPrefs.SetString(kParentKey, _parentQuery ?? "");
    }

    private void OnBecameInvisible()
    {
        IsWindowOpen = false;
    }

    private void OnBecameVisible()
    {
        IsWindowOpen = true;
    }

    #region Reload / Load Logic

    private void ReloadAll()
    {
        _previewCache.Clear();
        LoadSubfolderPaths();
        LoadFoldoutStates();
        LoadPrefabsPerFolder();
        PrefabBrush.LoadPaintingSurfaces();
        
    }

    private void LoadSubfolderPaths(string loadPath = "Level Assets")
    {
        _subFolderPaths.Clear();
        string fullPath = Path.Combine(Application.dataPath, loadPath);

        if (!Directory.Exists(fullPath))
        {
            Debug.LogWarning($"Folder not found: {loadPath}");
            return;
        }

        foreach (string path in Directory.GetDirectories(fullPath))
        {
            string relativePath = "Assets" + path.Replace(Application.dataPath, "").Replace('\\', '/');
            _subFolderPaths.Add(relativePath);
        }
    }

    private void LoadFoldoutStates()
    {
        _foldoutsState.Clear();
        foreach (string subfolder in _subFolderPaths)
            _foldoutsState[subfolder] = false;
    }

    private void LoadPrefabsPerFolder()
    {
        _prefabsPerFolder.Clear();

        foreach (string folderPath in _subFolderPaths)
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
        HandleSearchFoldoutLogic();
        DrawPrefabScrollView();

        DrawPrefabEntry(PrefabBrush.CurrentBrushPrefab);
        if (GUILayout.Button("Reload Prefabs")) ReloadAll();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        _searchQuery = GUILayout.TextField(_searchQuery, GUI.skin.FindStyle("ToolbarSearchTextField"));
        if (GUILayout.Button("x", GUI.skin.FindStyle("ToolbarSearchCancelButton")))
            _searchQuery = "";
        EditorGUILayout.EndHorizontal();

        DrawModeToolbar();
        
        if(!PrefabBrush.CurrentBrushPrefab) return;
        if (GUILayout.Button("Reset Prefab Brush"))
            PrefabBrush.ResetBrushPrefab();
    }
    
    // 🆕 Tool mode toolbar
    private void DrawModeToolbar()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        int newModeIndex = GUILayout.Toolbar(_modeIndex, _modeLabels, EditorStyles.toolbarButton, GUILayout.MinHeight(22));
        EditorGUILayout.EndHorizontal();

        if (newModeIndex != _modeIndex)
        {
            _modeIndex = newModeIndex;
            PrefabBrush.SetMode((PrefabBrush.BrushMode)_modeIndex);
        }
        
        //for now show only brush on erase
        bool isErase = _modeIndex == 1;
        string[] toolLabels = isErase ? new[] { "Brush" } : _toolLabels;
        if (isErase)
        {
            _toolIndex = 0;
            PrefabBrush.SetTool(PrefabBrush.BrushTool.Brush);
        }
        
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        int newToolIndex = GUILayout.Toolbar(_toolIndex, toolLabels, EditorStyles.toolbarButton, GUILayout.MinHeight(22));
        EditorGUILayout.EndHorizontal();

        if (newToolIndex != _toolIndex)
        {
            _toolIndex = newToolIndex;
            PrefabBrush.SetTool((PrefabBrush.BrushTool)_toolIndex);
        }
    }


    private void HandleSearchFoldoutLogic()
    {
        bool searchCleared = !string.IsNullOrEmpty(_lastSearchQuery) && string.IsNullOrEmpty(_searchQuery);
        bool searchBegun = string.IsNullOrEmpty(_lastSearchQuery) && !string.IsNullOrEmpty(_searchQuery);
        _lastSearchQuery = _searchQuery;

        if (searchCleared)
        {
            List<string> keys = new(_foldoutsState.Keys);
            foreach (string key in keys)
                _foldoutsState[key] = false; 
        } 
        else if (searchBegun)
        {
            List<string> keys = new(_foldoutsState.Keys);
            foreach (string key in keys)
                _foldoutsState[key] = true; 
        }
    }

    private void DrawPrefabScrollView()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        foreach (string subFolderPath in _subFolderPaths)
        {
            string folderName = Path.GetFileName(subFolderPath);

            _foldoutsState[subFolderPath] = EditorGUILayout.Foldout(_foldoutsState[subFolderPath], folderName, true, EditorStyles.foldoutHeader);

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
        if (!prefab) return;
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(20);
        EditorGUILayout.BeginHorizontal("box");

        Texture2D preview = GetCachedPreview(prefab);
        GUILayout.Label(preview, GUILayout.Width(64), GUILayout.Height(64));

        EditorGUILayout.BeginVertical();
        GUILayout.Label(prefab.name, EditorStyles.boldLabel);

        DrawPrefabSelectButton(prefab);

        if (PrefabBrush.IsCurrentBrush(prefab))
        {
            DrawParentCreateTextField();
        }

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
        {
            PrefabBrush.SetBrushPrefab(prefab);
            PrefabBrush.SetMode(PrefabBrush.BrushMode.Paint);
            _modeIndex = 0;
        }

        GUI.backgroundColor = originalColor;
    }

    private void DrawParentCreateTextField()
    {
        GUILayout.Label("Type empty parent name");
        EditorGUILayout.BeginHorizontal();
        _parentQuery = GUILayout.TextField(_parentQuery);
        if (GUILayout.Button("x", GUI.skin.FindStyle("ToolbarSearchCancelButton")))
            _parentQuery = "";
        EditorGUILayout.EndHorizontal();
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
