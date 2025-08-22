#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using System.Collections.Generic;

namespace HP.Generics
{
    public class w_ProceduralGP : EditorWindow
    {
        private Vector2     scrollPosAll;

        public float        multiplier = 1;
        public float        divider = 1;

        RoadData            roadData;
        SerializedObject    serializedObject;

        bool                initDone = false;

        public string[]     category = { "Procedural Generation", "Road Global Parameters", "Copy a part of a bezier curve" };

        [MenuItem("Tools/HP/Generator/Window Panel")]
        public static void ShowWindow()
        {
            #region
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow.GetWindow(typeof(w_ProceduralGP));
            #endregion
        }

        void OnEnable()
        {
            #region
            //-> Access Track data
            string objectPath = EditorPrefs.GetString("roadDataPath");
            roadData = AssetDatabase.LoadAssetAtPath(objectPath, typeof(UnityEngine.Object)) as RoadData;

            if (roadData)
            {
                serializedObject = new UnityEditor.SerializedObject(roadData);
                serializedObject.Update();
                serializedObject.ApplyModifiedProperties();
                initDone = true;
            }
            #endregion
        }

        void FirstTimeInitWindow()
        {
            #region
            EditorGUI.BeginChangeCheck();

            roadData = (RoadData)EditorGUILayout.ObjectField(roadData, typeof(RoadData), false);

            EditorGUILayout.HelpBox("-Press the small circle on the right of the empty field." +
                "\n-Then select the RoadData file in the list.", MessageType.Info);

            if (EditorGUI.EndChangeCheck())
            {
                string assetPath = AssetDatabase.GetAssetPath(roadData);
                EditorPrefs.SetString("roadDataPath", assetPath);

                serializedObject = new UnityEditor.SerializedObject(roadData);
                serializedObject.Update();
                serializedObject.ApplyModifiedProperties();
                initDone = true;
            }
            #endregion
        }

        void OnGUI()
        {
            #region
            //--> Scrollview

            scrollPosAll = EditorGUILayout.BeginScrollView(scrollPosAll);
            EditorGUILayout.LabelField("");

            
            if (roadData && initDone)
            {
                serializedObject.Update();
                EditorGUI.BeginChangeCheck();
                roadData = (RoadData)EditorGUILayout.ObjectField(roadData, typeof(RoadData), false);
                if (EditorGUI.EndChangeCheck())
                {
                    string assetPath = AssetDatabase.GetAssetPath(roadData);
                    EditorPrefs.SetString("roadDataPath", assetPath);
                }

                ChooseCategorySection();

                serializedObject.ApplyModifiedProperties();
            }
            else
            {
                FirstTimeInitWindow();
            }
            
            EditorGUILayout.EndScrollView();
            #endregion
        }

        void ChooseCategorySection()
        {
            #region
            EditorGUILayout.LabelField("");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Select a category:", EditorStyles.boldLabel, GUILayout.Width(120));
            roadData.currentProcedualGD = EditorGUILayout.Popup(roadData.currentProcedualGD, category, GUILayout.MinWidth(120));
            EditorGUILayout.EndHorizontal();

            switch (roadData.currentProcedualGD)
            {
                case 0:
                    PrefabList();
                    break;
                case 1:
                    GlobalRoadParameters();
                    break;
                case 2:
                    CopyPartOfCurve();
                    break;
            }
            #endregion
        }

        void CopyPartOfCurve()
        {
            #region
            EditorGUILayout.LabelField("");
            EditorGUILayout.LabelField("COPY A PART OF A BEZIER CURVE:", EditorStyles.boldLabel);
          
            if (Selection.activeGameObject && Selection.activeGameObject.GetComponent<Bezier>())
            {
                Bezier bezier = Selection.activeGameObject.GetComponent<Bezier>();

                CopyBezierCurve(bezier);
                PasteBezierCurve(bezier);

            }
            else
            {
                EditorGUILayout.HelpBox("Select an object with a Bezier script attached to it.", MessageType.Warning);
            }
            #endregion
        }

        void CopyBezierCurve(Bezier bezier)
        {
            #region
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy"))
            {
                if(bezier.closestPoint < 0)
                {
                    if (EditorUtility.DisplayDialog("Wrong Selection", "You must select a point on the curve.", "Continue")) { }
                }
                else
                {
                    Undo.RegisterFullObjectHierarchyUndo(bezier.gameObject, bezier.name);
                    roadData.pointsList.Clear();

                    int startPoint = bezier.closestPoint;
                    int endPoint = bezier.closestPoint + 3 * roadData.howManyPointToCopy - 3;
                    for (var i = startPoint; i <= endPoint; i++)
                    {
                        if (i < bezier.pointsList.Count)
                        {
                            PointDescription pos = bezier.pointsList[i];
                            roadData.pointsList.Add(new PointDescription(pos.points, pos.rotation));
                        }
                    }

                    Vector3 newPos = Selection.activeGameObject.transform.position;
                    roadData.curvePosRef = newPos;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(bezier);
                }
            }
            roadData.howManyPointToCopy = EditorGUILayout.IntField(roadData.howManyPointToCopy, GUILayout.MinWidth(50));

            EditorGUILayout.EndHorizontal();
            #endregion
        }

        void PasteBezierCurve(Bezier bezier)
        {
            #region
            if (GUILayout.Button("Paste"))
            {
                Undo.RegisterFullObjectHierarchyUndo(bezier.gameObject, bezier.name);
                bezier.pointsList.Clear();

                for (var i = 0; i < roadData.pointsList.Count; i++)
                {
                    PointDescription pos = roadData.pointsList[i];
                    bezier.pointsList.Add(new PointDescription(pos.points, pos.rotation));
                }
                Vector3 newPos = roadData.curvePosRef;
                Selection.activeGameObject.transform.position = newPos;

                PrefabUtility.RecordPrefabInstancePropertyModifications(bezier);
            }
            #endregion
        }

        void GlobalRoadParameters()
        {
            #region
            EditorGUILayout.LabelField("");
            EditorGUILayout.LabelField("GLOBAL ROAD PARAMETERS:", EditorStyles.boldLabel);

            SerializedProperty m_isGizmosDisplayed = serializedObject.FindProperty("isGizmosDisplayed");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Show Gizmos: ", GUILayout.Width(120));
            EditorGUILayout.PropertyField(m_isGizmosDisplayed, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            SerializedProperty m_groundOffset = serializedObject.FindProperty("groundOffset");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Ground Offset: ", GUILayout.Width(120));
            EditorGUILayout.PropertyField(m_groundOffset, new GUIContent(""));
            EditorGUILayout.EndHorizontal();
            #endregion
        }

        void PrefabList()
        {
            #region
            EditorGUILayout.LabelField("");
            EditorGUILayout.LabelField("PROCEDURAL GENERATION:", EditorStyles.boldLabel);

            SerializedProperty m_isRoadPrefabShown = serializedObject.FindProperty("isRoadPrefabShown");
            SerializedProperty m_roadPrefabList = serializedObject.FindProperty("roadPrefabList");
            SerializedProperty m_currentPrefabSelected = serializedObject.FindProperty("currentPrefabSelected");

            List<string> roadNameList = new List<string>();
            for (var i = 0; i < m_roadPrefabList.arraySize; i++)
                roadNameList.Add(m_roadPrefabList.GetArrayElementAtIndex(i).objectReferenceValue.name);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Prefabs List: ", GUILayout.Width(80));
            EditorGUILayout.PropertyField(m_roadPrefabList, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            if (m_currentPrefabSelected.intValue > m_roadPrefabList.arraySize - 1)
                m_currentPrefabSelected.intValue = 0;

            if (m_roadPrefabList.arraySize > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Select: ", GUILayout.Width(80));

                m_currentPrefabSelected.intValue = EditorGUILayout.Popup(m_currentPrefabSelected.intValue, roadNameList.ToArray(), GUILayout.MinWidth(100));
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Add to the scene", GUILayout.MinWidth(120), GUILayout.Height(30)))
                {
                    GameObject roadObj = (GameObject)PrefabUtility.InstantiatePrefab(m_roadPrefabList.GetArrayElementAtIndex(m_currentPrefabSelected.intValue).objectReferenceValue as GameObject);
                    Undo.RegisterCreatedObjectUndo(roadObj, "roadObj");

                    roadObj.transform.SetAsLastSibling();

                    SerializedProperty m_iD = serializedObject.FindProperty("iD");

                    Bezier bezier = roadObj.GetComponent<Bezier>();
                    bezier.roadID = m_iD.intValue;
                    roadObj.name +=  "_" + m_iD.intValue;
                    m_iD.intValue++;

                    Selection.activeGameObject = roadObj;

                    SceneView.lastActiveSceneView.Focus();
                }
            }
            #endregion
        }  
    }
}

#endif

