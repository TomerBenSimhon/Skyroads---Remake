// EffectsEditor.cs
// Custom inspector for `Effects` with:
// - Foldable Camera Effects list (clickable header)
// - Per-item foldouts (single arrow)
// - Tight item header: arrow + (optional) label, then popup immediately after
// - Clickable item header label toggles fold
//
// Paste over your existing file.

using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(Effects))]
public class EffectsEditor : Editor
{
    // --- Serialized fields ---
    SerializedProperty _camList;

    // --- Reorderable list for camera effects ---
    ReorderableList _list;

    // --- Foldout for the entire list ---
    bool _showCamList = true;

    // --- Type cache for CameraEffectSpec derivatives ---
    Type[] _camTypes;
    GUIContent[] _camTypeDisplay;

    // --- Layout constants ---
    const float kListHeaderPadLeft = 2f;  // space for list header foldout arrow
    const float kElemHandleOffset  = 10f;  // keep clear of the drag handle
    const float kArrowWidth        = 14f;  // small foldout arrow box
    const float kHeaderGap         = 4f;   // gap between arrow and label
    const float kElemPadLeft       = 27f;  // indent for expanded body
    const float kVertPad           = 6f;
    const float kChildVSpacing     = 2f;
    const float kPopupMinWidth     = 90f;  // minimal popup width
    const float kPopupMaxWidth     = 180f; // max clamp for long type names

    void OnEnable()
    {
        _camList = serializedObject.FindProperty("cameraEffects");

        // Collect all non-abstract derived types from CameraEffectSpec, ordered by name
        var found = TypeCache.GetTypesDerivedFrom<CameraEffectSpec>()
                             .Where(t => t.IsClass && !t.IsAbstract)
                             .OrderBy(t => t.Name)
                             .ToArray();

        _camTypes = found;
        _camTypeDisplay = _camTypes.Select(t => new GUIContent(t.Name)).ToArray();

        // Build reorderable list
        _list = new ReorderableList(serializedObject, _camList, true, true, true, true);

        // List header: full-row clickable foldout
        _list.drawHeaderCallback = rect =>
        {
            _showCamList = EditorGUI.Foldout(rect, _showCamList, GUIContent.none, true);
            var labelRect = new Rect(rect.x + kListHeaderPadLeft, rect.y, rect.width - kListHeaderPadLeft, rect.height);
            EditorGUI.LabelField(labelRect, "Camera Effects");
        };

        // Add dropdown: pick type to add
        _list.onAddDropdownCallback = (rect, list) =>
        {
            var menu = new GenericMenu();
            if (_camTypes == null || _camTypes.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No CameraEffectSpec types found"));
            }
            else
            {
                for (int i = 0; i < _camTypes.Length; i++)
                {
                    int idx = i;
                    menu.AddItem(new GUIContent(_camTypes[i].Name), false, () =>
                    {
                        serializedObject.Update();
                        int newIndex = _camList.arraySize;
                        _camList.InsertArrayElementAtIndex(newIndex);
                        var elem = _camList.GetArrayElementAtIndex(newIndex);
                        elem.managedReferenceValue = Activator.CreateInstance(_camTypes[idx]);
                        elem.isExpanded = true;
                        serializedObject.ApplyModifiedProperties();
                    });
                }
            }
            menu.ShowAsContext();
        };

        // Element GUI: arrow + (optional) label, popup placed right after the label; children below when expanded
        _list.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            if (!_showCamList) return; // if list collapsed, skip

            var elem = _camList.GetArrayElementAtIndex(index);
            if (elem == null) return;

            // Header row area, shifted right to avoid the drag handle
            var line = new Rect(rect.x + kElemHandleOffset, rect.y + 2f, rect.width - kElemHandleOffset, EditorGUIUtility.singleLineHeight);

            // Determine current type index and a friendly header label (we prefer showing tag only to keep things compact)
            int curIdx = GetTypeIndex(elem.managedReferenceFullTypename);
            string typeName = (curIdx >= 0 && curIdx < _camTypeDisplay.Length) ? _camTypeDisplay[curIdx].text : "Effect";

            // Optional tag shown as the header label text
            string headerText = "";
            var tagProp = elem.FindPropertyRelative("tag"); // safe if missing
            if (tagProp != null && tagProp.propertyType == SerializedPropertyType.String && !string.IsNullOrEmpty(tagProp.stringValue))
                headerText = tagProp.stringValue;

            // Small arrow at far left of header line
            var arrowRect = new Rect(line.x, line.y, kArrowWidth, line.height);
            elem.isExpanded = EditorGUI.Foldout(arrowRect, elem.isExpanded, GUIContent.none, true);

            // Label rect starts right after the arrow
            float labelLeft = arrowRect.x + kArrowWidth + kHeaderGap;

            // If there is label text, measure it (cap so it doesn't push popup off-screen)
            float maxLabelWidth = Mathf.Max(0f, line.width - kHeaderGap - kPopupMinWidth);
            float labelWidth = 0f;
            if (!string.IsNullOrEmpty(headerText))
            {
                labelWidth = EditorStyles.boldLabel.CalcSize(new GUIContent(headerText)).x;
                labelWidth = Mathf.Min(labelWidth, maxLabelWidth);
            }
            var labelRect = new Rect(labelLeft, line.y, labelWidth, line.height);

            // Compute popup size based on content, then clamp; place it right after label (or after arrow if no label)
            float popupWidth = Mathf.Clamp(EditorStyles.popup.CalcSize(new GUIContent(typeName)).x + 24f, kPopupMinWidth, kPopupMaxWidth);
            float popupX = (labelWidth > 0f) ? (labelRect.xMax + kHeaderGap) : (labelLeft); // arrow -> (label?) -> popup
            var popupRect = new Rect(popupX, line.y, popupWidth, line.height);

            // Ensure popup stays within the header line; if it would overflow, shrink label first
            if (popupRect.xMax > line.xMax)
            {
                float overflow = popupRect.xMax - line.xMax;
                float newLabelWidth = Mathf.Max(0f, labelWidth - (overflow + 2f));
                if (newLabelWidth != labelWidth)
                {
                    labelWidth = newLabelWidth;
                    labelRect.width = labelWidth;
                    popupRect.x = (labelWidth > 0f) ? (labelRect.xMax + kHeaderGap) : labelLeft;
                }
            }

            // Draw label (if any)
            if (labelWidth > 0f)
                EditorGUI.LabelField(labelRect, headerText, EditorStyles.boldLabel);

            // Click the label area to toggle fold
            if (labelWidth > 0f && Event.current.type == EventType.MouseDown && labelRect.Contains(Event.current.mousePosition))
            {
                elem.isExpanded = !elem.isExpanded;
                GUI.changed = true;
                Event.current.Use();
            }

            // Draw popup (no label)
            EditorGUI.BeginChangeCheck();
            int newIdx = EditorGUI.Popup(popupRect, GUIContent.none, Mathf.Max(0, curIdx), _camTypeDisplay);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.Update();
                if (newIdx >= 0 && newIdx < _camTypes.Length)
                {
                    elem.managedReferenceValue = Activator.CreateInstance(_camTypes[newIdx]);
                    elem.isExpanded = true; // expand when changing type
                }
                serializedObject.ApplyModifiedProperties();
            }

            // Body (only when expanded) — render children only to avoid a second arrow
            if (elem.isExpanded && elem.managedReferenceValue != null)
            {
                float y = line.yMax + 4f;
                float x = rect.x + kElemPadLeft;
                float w = rect.width - kElemPadLeft;
                DrawManagedRefChildren(elem, new Rect(x, y, w, 0f));
            }
        };

        // Element height: header + (optional) body
        _list.elementHeightCallback = index =>
        {
            if (!_showCamList) return 0f; // when list collapsed, hide elements

            var elem = _camList.GetArrayElementAtIndex(index);
            float h = EditorGUIUtility.singleLineHeight + kVertPad; // header + padding
            if (elem != null && elem.managedReferenceValue != null && elem.isExpanded)
                h += GetManagedRefChildrenHeight(elem) + kVertPad;
            return h;
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw everything except the list—it's drawn via ReorderableList
        DrawPropertiesExcluding(serializedObject, "m_Script", "cameraEffects");

        EditorGUILayout.Space(6);

        // Helpful notice if no spec types available
        if (_camTypes == null || _camTypes.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "No CameraEffectSpec types found. Create a class deriving from CameraEffectSpec (non-abstract) to add items.",
                MessageType.Info);
        }

        // Draw header always; draw elements only if expanded
        if (_list != null)
        {
            if (_showCamList)
            {
                _list.DoLayoutList();
            }
            else
            {
                // Manually draw only the header row (no elements, no footer)
                Rect headerRect = GUILayoutUtility.GetRect(0f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
                _list.drawHeaderCallback?.Invoke(headerRect);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ---------- Helpers: draw children of a managed-reference property, no root foldout ----------

    void DrawManagedRefChildren(SerializedProperty root, Rect startRect)
    {
        var prop = root.Copy();
        var end  = root.GetEndProperty();

        bool enterChildren = prop.NextVisible(true);

        float y = startRect.y;
        float x = startRect.x;
        float w = startRect.width;

        while (enterChildren && !SerializedProperty.EqualContents(prop, end))
        {
            float h = EditorGUI.GetPropertyHeight(prop, true);
            var r = new Rect(x, y, w, h);
            EditorGUI.PropertyField(r, prop, true);

            y += h + kChildVSpacing;

            enterChildren = prop.NextVisible(false);
        }
    }

    float GetManagedRefChildrenHeight(SerializedProperty root)
    {
        float total = 0f;

        var prop = root.Copy();
        var end  = root.GetEndProperty();

        bool enterChildren = prop.NextVisible(true);
        while (enterChildren && !SerializedProperty.EqualContents(prop, end))
        {
            total += EditorGUI.GetPropertyHeight(prop, true) + kChildVSpacing;
            enterChildren = prop.NextVisible(false);
        }

        if (total > 0f) total -= kChildVSpacing;
        return total;
    }

    // Map managedReferenceFullTypename to index in _camTypes
    int GetTypeIndex(string managedRefFullTypename)
    {
        if (string.IsNullOrEmpty(managedRefFullTypename) || _camTypes == null) return -1;

        for (int i = 0; i < _camTypes.Length; i++)
        {
            var t = _camTypes[i];
            if (t == null) continue;

            string asm = t.Assembly.GetName().Name;
            string full = t.FullName;
            if (!string.IsNullOrEmpty(asm) && !string.IsNullOrEmpty(full))
            {
                if (managedRefFullTypename.Contains(asm) && managedRefFullTypename.Contains(full))
                    return i;
            }
        }
        return -1;
    }
}
