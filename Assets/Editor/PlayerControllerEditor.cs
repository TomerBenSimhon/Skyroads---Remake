using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlayerController))]
public class PlayerControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        PlayerController controller = (PlayerController)target;

        if (Application.isPlaying && controller.RuntimeSettings != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Settings (Live)", EditorStyles.boldLabel);

            SerializedObject runtimeSettingsSO = new SerializedObject(controller.RuntimeSettings);
            SerializedProperty prop = runtimeSettingsSO.GetIterator();

            bool expanded = true;
            if (prop.NextVisible(true))
            {
                do
                {
                    if (prop.name == "m_Script") continue; // skip script reference
                    EditorGUILayout.PropertyField(prop, expanded);
                }
                while (prop.NextVisible(false));
            }

            runtimeSettingsSO.ApplyModifiedProperties();
        }
    }
}