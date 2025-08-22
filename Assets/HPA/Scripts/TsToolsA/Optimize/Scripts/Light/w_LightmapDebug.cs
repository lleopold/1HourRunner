// Description : w_LightmapDebug: Allows to multiple or divide the lightmap scale of an object.
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace HP.Generics
{
    public class w_LightmapDebug : EditorWindow
    {
        private Vector2 scrollPosAll;

        public float    multiplier = 6;
        public float    divider = 6;

        public float    maxLightmapValue = 10;

        public class WrongScaleParams
        {
            public float wrongSacle;
            public GameObject objWrongScale;

            public WrongScaleParams(float _wrongSacle, GameObject _objWrongScale)
            {
                wrongSacle = _wrongSacle;
                objWrongScale = _objWrongScale;
            }
        }

        public List<WrongScaleParams> wrongScaleList = new List<WrongScaleParams>();

        [MenuItem("Tools/HP/Lightmap Size Modifier")]
        public static void ShowWindow()
        {
            #region
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow.GetWindow(typeof(w_LightmapDebug));
            #endregion
        }

        void OnEnable()
        {
            #region
            if (EditorPrefs.HasKey("LightmapDebug"))
                multiplier = EditorPrefs.GetFloat("LightmapDebug");

            if (EditorPrefs.HasKey("LightmapDebugDivider"))
                divider = EditorPrefs.GetFloat("LightmapDebugDivider");
            #endregion
        }

        void OnGUI()
        {
            #region
            scrollPosAll = EditorGUILayout.BeginScrollView(scrollPosAll);
            EditorGUILayout.LabelField("");
            SectionMultiple();
            //SectionDivide();
            SectionReset();
            //SectionMaxObjectScaleInLightmap();
            EditorGUILayout.EndScrollView();
            #endregion
        }

        void SectionMultiple()
        {
            #region
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Multiplier:", GUILayout.Width(70));
            multiplier = EditorGUILayout.FloatField(multiplier, GUILayout.Width(50));

            if (GUILayout.Button("Update Lightmap Multiple"))
            {
                GameObject selectedObj = Selection.activeGameObject;

                if (selectedObj)
                {
                    wrongScaleList.Clear();

                    MeshRenderer[] meshRenderers = selectedObj.transform.GetComponentsInChildren<MeshRenderer>();

                    foreach (MeshRenderer meshRenderer in meshRenderers)
                    {
                        if (meshRenderer.enabled)
                        {
                            SerializedObject serializedObject2 = new UnityEditor.SerializedObject(meshRenderer);
                            SerializedProperty m_ScaleInLightmap = serializedObject2.FindProperty("m_ScaleInLightmap");
                            serializedObject2.Update();
                            float newScale = m_ScaleInLightmap.floatValue * multiplier;
                            m_ScaleInLightmap.floatValue = newScale;

                            if (newScale > 10 || newScale < .1f)
                                wrongScaleList.Add(new WrongScaleParams(newScale, meshRenderer.gameObject));

                            serializedObject2.ApplyModifiedProperties();
                        }
                    }
                }
                EditorPrefs.SetFloat("LightmapDebug", multiplier);
                if (EditorUtility.DisplayDialog("Lightmap Process:", "Done", "Continue")) { }
            }
            EditorGUILayout.EndHorizontal();
            #endregion
        }

        void SectionDivide()
        {
            #region
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Divider:", GUILayout.Width(70));
            divider = EditorGUILayout.FloatField(divider, GUILayout.Width(50));

            if (GUILayout.Button("Update Lightmap Divide"))
            {
                GameObject selectedObj = Selection.activeGameObject;

                if (selectedObj)
                {
                    wrongScaleList.Clear();
                    MeshRenderer[] meshRenderers = selectedObj.transform.GetComponentsInChildren<MeshRenderer>();

                    foreach (MeshRenderer meshRenderer in meshRenderers)
                    {
                        if (meshRenderer.enabled)
                        {
                            SerializedObject serializedObject2 = new UnityEditor.SerializedObject(meshRenderer);
                            SerializedProperty m_ScaleInLightmap = serializedObject2.FindProperty("m_ScaleInLightmap");
                            serializedObject2.Update();

                            float newScale = m_ScaleInLightmap.floatValue / divider;
                            m_ScaleInLightmap.floatValue = newScale;

                            if (newScale > 10 || newScale < .1f)
                                wrongScaleList.Add(new WrongScaleParams(newScale, meshRenderer.gameObject));

                            serializedObject2.ApplyModifiedProperties();
                        }
                    }
                }

                EditorPrefs.SetFloat("LightmapDebugDivider", divider);

                if (EditorUtility.DisplayDialog("Lightmap Process:", "Done", "Continue")) { }
            }
            EditorGUILayout.EndHorizontal();
            #endregion
        }

        void SectionReset()
        {
            #region
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Reset:", GUILayout.Width(123));

            if (GUILayout.Button("Revert prefab ScaleInLightmap"))
            {
                GameObject selectedObj = Selection.activeGameObject;

                if (selectedObj)
                {
                    MeshRenderer[] meshRenderers = selectedObj.transform.GetComponentsInChildren<MeshRenderer>();

                    foreach (MeshRenderer meshRenderer in meshRenderers)
                    {
                        if (meshRenderer.enabled && PrefabUtility.IsPartOfAnyPrefab(meshRenderer))
                        {
                            SerializedObject serializedObject2 = new UnityEditor.SerializedObject(meshRenderer);
                            SerializedProperty m_ScaleInLightmap = serializedObject2.FindProperty("m_ScaleInLightmap");
                            serializedObject2.Update();

                            PrefabUtility.RevertPropertyOverride(m_ScaleInLightmap, InteractionMode.UserAction);

                            serializedObject2.ApplyModifiedProperties();
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            #endregion
        }

        void SectionMaxObjectScaleInLightmap()
        {
            #region
            EditorGUILayout.LabelField("");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("ScaleInLightmap Max Value:", GUILayout.Width(170));

            maxLightmapValue = EditorGUILayout.FloatField(maxLightmapValue, GUILayout.Width(50));
            if (GUILayout.Button("Check Objects"))
            {
                GameObject selectedObj = Selection.activeGameObject;

                if (selectedObj)
                {
                    wrongScaleList.Clear();
                    MeshRenderer[] meshRenderers = selectedObj.transform.GetComponentsInChildren<MeshRenderer>();

                    foreach (MeshRenderer meshRenderer in meshRenderers)
                    {
                        if (meshRenderer.enabled)
                        {
                            SerializedObject serializedObject2 = new UnityEditor.SerializedObject(meshRenderer);
                            SerializedProperty m_ScaleInLightmap = serializedObject2.FindProperty("m_ScaleInLightmap");
                            serializedObject2.Update();

                            if (m_ScaleInLightmap.floatValue > maxLightmapValue)
                                wrongScaleList.Add(new WrongScaleParams(m_ScaleInLightmap.floatValue, meshRenderer.gameObject));

                            serializedObject2.ApplyModifiedProperties();
                        }
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            if (wrongScaleList.Count > 0)
            {
                EditorGUILayout.LabelField("Objects out of the limit:", GUILayout.Width(70));

                for (var i = 0; i < wrongScaleList.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    wrongScaleList[i].objWrongScale = (GameObject)EditorGUILayout.ObjectField(wrongScaleList[i].objWrongScale, typeof(GameObject), true);
                    EditorGUILayout.FloatField(wrongScaleList[i].wrongSacle);
                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Clear list"))
                    wrongScaleList.Clear();
            }
           
            #endregion
        }
    }
}

#endif

