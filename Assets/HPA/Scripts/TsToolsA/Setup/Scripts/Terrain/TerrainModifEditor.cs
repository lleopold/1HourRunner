//Description: TerrainModifEditor: Custom Editor
#if (UNITY_EDITOR)
using UnityEngine;
using UnityEditor;

namespace HP.Generics
{
    [CustomEditor(typeof(TerrainModif))]
    public class TerrainModifEditor : Editor
    {
        SerializedProperty SeeInspector;                                            // use to draw default Inspector

        void OnEnable()
        {
            #region
            // Setup the SerializedProperties.
            SeeInspector = serializedObject.FindProperty("seeInspector");
            #endregion
        }

        public override void OnInspectorGUI()
        {
            #region
            if (SeeInspector.boolValue)
                DrawDefaultInspector();

            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Show Inspector: ", GUILayout.Width(100));
            EditorGUILayout.PropertyField(SeeInspector, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();

            #endregion
        }

        public bool IsBetween(float testValue, float bound1, float bound2)
        {
            #region
            return (testValue >= Mathf.Min(bound1, bound2) && testValue <= Mathf.Max(bound1, bound2));
            #endregion
        }

    }
}

#endif

