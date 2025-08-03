using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class EnvironmentPrefabWindow : EditorWindow
{
    private Vector2 _scrollPosition;
    private List<GameObject> _prefabs;

    [MenuItem("Tools/Environment Prefabs Window")]
    public static void ShowWindow()
    {
        GetWindow<EnvironmentPrefabWindow>("Environment Prefabs");
    }

    private void OnEnable()
    {
        LoadPrefabs();
    }

    private void LoadPrefabs()
    {
        _prefabs.Clear();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Level Assets" });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                _prefabs.Add(prefab);
        }
    }

    private void OnGUI()
    {
        if (_prefabs == null || _prefabs.Count == 0)
        {
            EditorGUILayout.LabelField("No prefabs found in Assets/Level Assets");
            if (GUILayout.Button("Reload Prefabs"))
            {
                LoadPrefabs();
            }
            return;
        }

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        foreach (GameObject prefab in _prefabs)
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

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Reload Prefabs"))
        {
            LoadPrefabs();
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
