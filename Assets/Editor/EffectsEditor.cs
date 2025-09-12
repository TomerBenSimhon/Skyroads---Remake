using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(Effects))]
public class EffectsEditor : Editor
{
    SerializedProperty _camList;
    ReorderableList _list;

    // Cache available concrete spec types
    static Type[]   _camTypes;
    static string[] _camTypeDisplay;
    static string[] _camTypeFull;

    void OnEnable()
    {
        _camList = serializedObject.FindProperty("cameraEffects");

        _camTypes = TypeCache.GetTypesDerivedFrom<CameraEffectSpec>()
                             .Where(t => !t.IsAbstract && t.IsClass && t.IsPublic)
                             .OrderBy(t => t.Name)
                             .ToArray();
        _camTypeDisplay = _camTypes.Select(t => t.Name).ToArray();
        _camTypeFull    = _camTypes.Select(t => t.FullName).ToArray();

        _list = new ReorderableList(serializedObject, _camList, true, true, true, true);

        _list.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Camera Effects");
        };

        _list.onAddDropdownCallback = (rect, list) =>
        {
            var menu = new GenericMenu();
            if (_camTypes.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No CameraEffectSpec types found"));
            }
            else
            {
                for (int i = 0; i < _camTypes.Length; i++)
                {
                    int idx = i;
                    menu.AddItem(new GUIContent(_camTypeDisplay[i]), false, () =>
                    {
                        _camList.arraySize++;
                        var elem = _camList.GetArrayElementAtIndex(_camList.arraySize - 1);
                        elem.managedReferenceValue = Activator.CreateInstance(_camTypes[idx]);
                        serializedObject.ApplyModifiedProperties();
                    });
                }
            }
            menu.ShowAsContext();
        };

        _list.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            var elem = _camList.GetArrayElementAtIndex(index);

            // Top row: Type popup + remove button
            var line = new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight);

            // Determine current type index
            int curIdx = GetTypeIndex(elem.managedReferenceFullTypename);
            int newIdx = EditorGUI.Popup(new Rect(line.x, line.y, line.width - 60, line.height),
                                         "Type", Mathf.Max(0, curIdx), _camTypeDisplay);

            if (newIdx != curIdx || elem.managedReferenceValue == null)
            {
                if (newIdx >= 0 && newIdx < _camTypes.Length)
                {
                    elem.managedReferenceValue = Activator.CreateInstance(_camTypes[newIdx]);
                    serializedObject.ApplyModifiedProperties();
                }
            }

            // Draw the managed reference (all its serialized fields)
            if (elem.managedReferenceValue != null)
            {
                var bodyRect = new Rect(rect.x + 10, line.yMax + 4, rect.width - 10, 
                                        EditorGUI.GetPropertyHeight(elem, includeChildren: true));
                elem.isExpanded = true; // ensure children are drawn
                EditorGUI.PropertyField(bodyRect, elem, GUIContent.none, includeChildren: true);
            }
        };

        _list.elementHeightCallback = index =>
        {
            var elem = _camList.GetArrayElementAtIndex(index);
            float h = EditorGUIUtility.singleLineHeight + 6; // type row + padding
            if (elem != null && elem.managedReferenceValue != null)
                h += EditorGUI.GetPropertyHeight(elem, true) + 6; // parameters height
            return h;
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw everything EXCEPT the raw list field (we draw it via ReorderableList)
        DrawPropertiesExcluding(serializedObject, "m_Script", "cameraEffects");

        EditorGUILayout.Space(6);
        if (_camTypes.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "No concrete CameraEffectSpec types found.\n" +
                "Create public [Serializable] classes deriving from CameraEffectSpec (e.g., FovPulseSpec).",
                MessageType.Info);
        }

        _list.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }

    // Helpers
    int GetTypeIndex(string managedRefFullTypename)
    {
        if (string.IsNullOrEmpty(managedRefFullTypename)) return -1;
        int space = managedRefFullTypename.LastIndexOf(' ');
        string full = space >= 0 ? managedRefFullTypename[(space + 1)..] : managedRefFullTypename;
        return Array.IndexOf(_camTypeFull, full);
    }
}
