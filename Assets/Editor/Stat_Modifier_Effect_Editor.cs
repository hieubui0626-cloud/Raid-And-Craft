#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(Stat_Modifier_Effect))]
public class Stat_Modifier_Effect_Editor : Editor
{
    private SerializedProperty durationModeProperty;

    private void OnEnable()
    {
        durationModeProperty =
            serializedObject.FindProperty("durationMode");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty property =
            serializedObject.GetIterator();

        bool enterChildren = true;

        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (property.name == "m_Script")
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(property);
                EditorGUI.EndDisabledGroup();
                continue;
            }

            if (property.name == "duration" ||
                property.name == "stackMode")
            {
                bool isTemporary =
                    durationModeProperty.enumValueIndex ==
                    (int)Stat_Modifier_Duration_Mode.Temporary;

                if (!isTemporary)
                    continue;
            }

            EditorGUILayout.PropertyField(property, true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif