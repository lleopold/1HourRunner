//Description: InstantiateObjectUsingBezierCurveEditor: Custom Editor
#if (UNITY_EDITOR)
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


namespace HP.Generics
{
    [CustomEditor(typeof(InstantiateObjectUsingBezierCurve))]
    public class InstantiateObjectUsingBezierCurveEditor : Editor
    {
        SerializedProperty m_seeInspector;
        SerializedProperty m_moreOptions;
        SerializedProperty m_advanced;

        SerializedProperty m_endPathPos;
        SerializedProperty m_startPathPos;
        SerializedProperty m_distVecListPlusOffsetFinal;

        SerializedProperty m_objsList;

        SerializedProperty m_percentageProba;
        SerializedProperty m_objsRandomList;


        SerializedProperty m_objExtraOffset;
        SerializedProperty m_objExtraRotation;
        SerializedProperty m_distanceBetweenDistVec;
        SerializedProperty m_distVecOffset;

        SerializedProperty m_showGizmo;
        SerializedProperty m_prefabRotation;
        SerializedProperty m_interval;

        private bool isProcessDone = true;

        void OnEnable()
        {
            #region
            // Setup the SerializedProperties.
            m_seeInspector = serializedObject.FindProperty("seeInspector");
            m_moreOptions = serializedObject.FindProperty("moreOptions");
            m_advanced = serializedObject.FindProperty("advanced");
            m_startPathPos = serializedObject.FindProperty("startPathPos");
            m_endPathPos = serializedObject.FindProperty("endPathPos");
            m_distVecListPlusOffsetFinal = serializedObject.FindProperty("distVecListPlusOffsetFinal");
            m_objsList = serializedObject.FindProperty("objsList");
            m_objExtraOffset = serializedObject.FindProperty("objExtraOffset");
            m_objExtraRotation = serializedObject.FindProperty("objExtraRotation");
            m_distanceBetweenDistVec = serializedObject.FindProperty("distanceBetweenDistVec");
            m_distVecOffset = serializedObject.FindProperty("distVecOffset");
            m_showGizmo = serializedObject.FindProperty("showGizmo");
            m_prefabRotation = serializedObject.FindProperty("prefabRotation");
            m_interval = serializedObject.FindProperty("interval");

            m_percentageProba = serializedObject.FindProperty("percentageProba");
            m_objsRandomList = serializedObject.FindProperty("objsRandomList");

            #endregion
        }

        public override void OnInspectorGUI()
        {
            #region
            if (m_seeInspector.boolValue)
                DrawDefaultInspector();

            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            if (!m_seeInspector.boolValue)
            {
                EditorGUILayout.LabelField("Inspector: ", GUILayout.Width(60));
                EditorGUILayout.PropertyField(m_seeInspector, new GUIContent(""), GUILayout.Width(20));
                EditorGUILayout.LabelField("Advance: ", GUILayout.Width(60));
                EditorGUILayout.PropertyField(m_advanced, new GUIContent(""), GUILayout.Width(20));
                EditorGUILayout.LabelField("Options: ", GUILayout.Width(60));
                EditorGUILayout.PropertyField(m_moreOptions, new GUIContent(""), GUILayout.Width(20));
            }
            EditorGUILayout.EndHorizontal();


            InstantiateObjectUsingBezierCurve OBC = (InstantiateObjectUsingBezierCurve)target;
            if (OBC.objsList.Count > 0)
                if (GUILayout.Button("Instantiate Objects", GUILayout.Height(30)))
                    InstantiateObjectsProcess();

            if (m_advanced.boolValue)
                AdvanceOptions();

            if (m_moreOptions.boolValue)
                SelectAPartOfThePathSection();

            EditorGUILayout.LabelField("");

            serializedObject.ApplyModifiedProperties();
            #endregion
        }

        void AdvanceOptions()
        {
            #region
            EditorGUILayout.LabelField("");
            EditorGUILayout.LabelField("ADVANCE:", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Set the instantiated objects list.", MessageType.None);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Objects List: ", GUILayout.Width(120));
            EditorGUILayout.PropertyField(m_objsList, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Define the distance the objects are instantiated from the path.", MessageType.None);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Distance From Path: ", GUILayout.Width(120));
            EditorGUILayout.PropertyField(m_distVecOffset, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Define the spawn distance.", MessageType.None);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Spawn Distance: ", GUILayout.Width(120));
            EditorGUILayout.PropertyField(m_distanceBetweenDistVec, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Define the spawn distance multiplier. Actually an object is instantiated every: " +
                 "Spawn Distance x Interval Multiplier " + " = " + m_distanceBetweenDistVec.floatValue * m_interval.intValue + "m", MessageType.None);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Interval Multiplier: ", GUILayout.Width(120));
            EditorGUILayout.PropertyField(m_interval, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Define if objects follow the path rotation.", MessageType.None);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Rotation Along Path:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(m_prefabRotation, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Define extra offset to the instantiated objects.", MessageType.None);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Offset Position: ", GUILayout.Width(120));
            EditorGUILayout.PropertyField(m_objExtraOffset, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Offset Rotation: ", GUILayout.Width(120));
            EditorGUILayout.PropertyField(m_objExtraRotation, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Show Gizmos: ", GUILayout.Width(120));
            EditorGUILayout.PropertyField(m_showGizmo, new GUIContent(""));
            EditorGUILayout.EndHorizontal();
            #endregion
        }

        void InstantiateObjectsProcess()
        {
            #region
            InstantiateObjectUsingBezierCurve OBC = (InstantiateObjectUsingBezierCurve)target;
            UpdateSpawnPointsList();
            while (!isProcessDone) { }

            GameObject objGrp = CreateAFolderForInstantiatedObjects();
            SetupFolderDependingInstatiatedObjects(objGrp);
            while (!isProcessDone) { }

            int StartID = SelectStartPathPoint();
            int endID = SelectLastPathPoint();

            InstantiateObjects(objGrp, StartID, endID, OBC);
            while (!isProcessDone) { }

            CreateWireAndFence(objGrp, OBC);
            while (!isProcessDone) { }
            #endregion
        }

        GameObject CreateAFolderForInstantiatedObjects()
        {
            #region
            InstantiateObjectUsingBezierCurve OBC = (InstantiateObjectUsingBezierCurve)target;
            GameObject objGrp = new GameObject();
            Undo.RegisterCreatedObjectUndo(objGrp, "objGrp");
            objGrp.transform.position = OBC.transform.position + OBC.distVecListPlusOffsetFinal[0].spotPos;

            if (OBC.grpThatContainInstantiateObjects != null && OBC.createFolderInside)
                DestroyImmediate(OBC.grpThatContainInstantiateObjects);


            if (OBC.createFolderInside)
            {
                objGrp.transform.SetParent(OBC.gameObject.transform);
                OBC.grpThatContainInstantiateObjects = objGrp;
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(OBC);
            return objGrp;
            #endregion
        }

        void SetupFolderDependingInstatiatedObjects(GameObject objGrp)
        {
            #region
            isProcessDone = false;
            InstantiateObjectUsingBezierCurve OBC = (InstantiateObjectUsingBezierCurve)target;
            if (OBC.roadStyle == RoadMeshGen.RoadStyle.Wire || OBC.roadStyle == RoadMeshGen.RoadStyle.Fence)
            {
                objGrp.AddComponent<WireGroup>();
                WireGroup wireGroup = objGrp.GetComponent<WireGroup>();
                wireGroup.roadStyle = OBC.roadStyle;

                switch (wireGroup.roadStyle)
                {
                    case RoadMeshGen.RoadStyle.Wire:
                        wireGroup.offsetForward = 2;
                        wireGroup.offsetDown = .5f;
                        wireGroup.precision = 2f;
                        break;

                    case RoadMeshGen.RoadStyle.Fence:
                        wireGroup.offsetForward = 1;
                        wireGroup.offsetDown = 0;
                        wireGroup.precision = 1f;
                        break;
                }
            }
            isProcessDone = true;
            #endregion
        }

        int SelectStartPathPoint()
        {
            #region
            InstantiateObjectUsingBezierCurve OBC = (InstantiateObjectUsingBezierCurve)target;

            if (!m_moreOptions.boolValue)
                return 0;
            else
                return m_startPathPos.intValue;
            #endregion
        }

        int SelectLastPathPoint()
        {
            #region
            InstantiateObjectUsingBezierCurve OBC = (InstantiateObjectUsingBezierCurve)target;

            if (!m_moreOptions.boolValue)
                return OBC.distVecListPlusOffsetFinal.Count;
            else
                return m_endPathPos.intValue;
            #endregion
        }
        //m_percentageProba
        //m_objsRandomList

        GameObject SelectedInstantiatedPrefab(InstantiateObjectUsingBezierCurve OBC, int id)
        {
            if (m_objsRandomList.arraySize > 0)
            {
                int randomValue = UnityEngine.Random.Range(0, 100);

                if (m_percentageProba.intValue > randomValue)
                {
                    List<GameObject> randomList = new List<GameObject>();

                    for (var i = 0; i < m_objsRandomList.arraySize; i++)
                    {
                        for (var j = 0; j < m_objsRandomList.GetArrayElementAtIndex(i).FindPropertyRelative("proba").intValue; j++)
                            randomList.Add((GameObject)m_objsRandomList.GetArrayElementAtIndex(i).FindPropertyRelative("obj").objectReferenceValue);
                    }

                    randomValue = UnityEngine.Random.Range(0, randomList.Count);

                    return (GameObject)PrefabUtility.InstantiatePrefab(randomList[randomValue] as GameObject);
                }


            }


            return (GameObject)PrefabUtility.InstantiatePrefab(OBC.objsList[id % OBC.objsList.Count] as GameObject);
        }

        void InstantiateObjects(GameObject objGrp, int StartID, int endID, InstantiateObjectUsingBezierCurve OBC)
        {
            #region
            isProcessDone = false;
            List<GameObject> objList = new List<GameObject>();
            for (var i = StartID; i < endID; i++)
            {
                if (i % OBC.interval == 0)
                {
                    // Create the object
                    GameObject prefab = SelectedInstantiatedPrefab(OBC, i % OBC.objsList.Count);// (GameObject)PrefabUtility.InstantiatePrefab(OBC.objsList[i % OBC.objsList.Count] as GameObject);

                    objList.Add(prefab);
                    prefab.name = i.ToString();


                    prefab.transform.SetAsLastSibling();

                    // Set the object position and the rotation.
                    int firstPos = OBC.distVecListPlusOffsetFinal[i].firstSpot;
                    Vector3 point = RoadCreation.GetPointPosition(firstPos, firstPos + 1, firstPos + 2, firstPos + 3, 0, OBC.pointsList) + OBC.transform.position;
                    Vector3 endPos = point + 1.5f * 30 * RoadCreation.GetVelocity(firstPos, firstPos + 1, firstPos + 2, firstPos + 3, 0, OBC.pointsList).normalized;
                    Vector3 dir = point - endPos;
                    Vector3 roadUpDir = Vector3.up;

                    float distFromPath = OBC.distVecOffset;
                    if (OBC.distVecOffset == 0) distFromPath = .01f;
                    Vector3 offsetPos = distFromPath * Vector3.Cross(dir, roadUpDir).normalized;

                    prefab.transform.position = OBC.distVecListPlusOffsetFinal[i].spotPos + OBC.transform.position;

                    if (OBC.prefabRotation == InstantiateObjectUsingBezierCurve.PrefabRotation.FollowPathRotation
                        ||
                        OBC.prefabRotation == InstantiateObjectUsingBezierCurve.PrefabRotation.LookAtNextPrefab)
                        prefab.transform.LookAt(endPos);
                    else
                        prefab.transform.LookAt(OBC.distVecListPlusOffsetFinal[i].spotPos + OBC.transform.position + offsetPos);

                    prefab.transform.position +=
                        prefab.transform.right * OBC.objExtraOffset.x +
                        prefab.transform.up * OBC.objExtraOffset.y +
                        prefab.transform.forward * OBC.objExtraOffset.z;

                    prefab.transform.Rotate(OBC.objExtraRotation, Space.Self);

                    prefab.transform.SetParent(objGrp.transform);

                    Undo.RegisterCreatedObjectUndo(prefab, "prefab");
                }
            }

            if (OBC.prefabRotation == InstantiateObjectUsingBezierCurve.PrefabRotation.LookAtNextPrefab)
            {
                for (var i = 0; i < objList.Count - 1; i++)
                {
                    objList[i + 1].transform.LookAt(objList[i].transform);
                }
                objList[0].SetActive(false);
            }



            isProcessDone = true;



            #endregion
        }

        void CreateWireAndFence(GameObject objGrp, InstantiateObjectUsingBezierCurve OBC)
        {
            #region
            isProcessDone = false;

            // Initialize WireGroup script
            if (OBC.roadStyle == RoadMeshGen.RoadStyle.Wire || OBC.roadStyle == RoadMeshGen.RoadStyle.Fence)
            {
                int HowManyObjects = objGrp.transform.childCount;
                for (var i = 0; i < HowManyObjects; i++)
                    objGrp.GetComponent<WireGroup>().listWire.Add(objGrp.transform.GetChild(i).GetComponent<Wire>());

                // Create wire and fence
                WireGroup wireGrp = objGrp.GetComponent<WireGroup>();
                for (var i = 0; i < wireGrp.listWire.Count - 1; i++)
                {
                    for (var j = 0; j < wireGrp.listWire[i].GetComponent<Wire>().wireList.Count; j++)
                    {
                        Bezier bezier = wireGrp.listWire[i].GetComponent<Wire>().wireList[j].GetComponent<Bezier>();

                        wireGrp.listWire[i].GetComponent<Wire>().wireList[j].transform.rotation = Quaternion.identity;

                        Transform wirePivotPosStart = wireGrp.listWire[i].GetComponent<Wire>().wireList[j].transform;
                        Transform wirePivotPosEnd = wireGrp.listWire[i + 1].GetComponent<Wire>().wireList[j].transform;

                        Vector3 dir = Vector3.zero;
                        float distanceTangent = 0;
                        Vector3 down = Vector3.zero;

                        switch (OBC.roadStyle)
                        {
                            case RoadMeshGen.RoadStyle.Wire:
                                dir = (wirePivotPosEnd.position - wirePivotPosStart.position).normalized;
                                distanceTangent = 2;
                                down = Vector3.up * .5f;
                                break;

                            case RoadMeshGen.RoadStyle.Fence:
                                dir = (wirePivotPosEnd.position - wirePivotPosStart.position).normalized;
                                distanceTangent = 1;
                                down = Vector3.zero;
                                break;
                        }


                        bezier.pointsList.Add(new PointDescription(wirePivotPosStart.position - bezier.transform.position, Quaternion.identity));
                        bezier.pointsList.Add(new PointDescription(wirePivotPosStart.position + dir * distanceTangent - down - bezier.transform.position, Quaternion.identity));

                        bezier.pointsList.Add(new PointDescription(wirePivotPosEnd.position - dir * distanceTangent - down - bezier.transform.position, Quaternion.identity));
                        bezier.pointsList.Add(new PointDescription(wirePivotPosEnd.position - bezier.transform.position, Quaternion.identity));

                        RoadMeshGen.ReturnTotalCurveDistance(bezier, .75f);

                        while (!RoadMeshGen.isProcessDone) { }

                        bezier.GetComponent<MeshFilter>().sharedMesh = null;

                        int pointsListSize = bezier.pointsList.Count;

                        switch (OBC.roadStyle)
                        {
                            case RoadMeshGen.RoadStyle.Wire:
                                RoadMeshGen.RoadMesh(0, pointsListSize, bezier, bezier.distVecList.Count, OBC.roadStyle, RoadMeshGen.PlaneFace.Front, false, 3);
                                break;

                            case RoadMeshGen.RoadStyle.Fence:
                                if (j % 2 == 0)
                                    RoadMeshGen.RoadMesh(0, pointsListSize, bezier, bezier.distVecList.Count, OBC.roadStyle, RoadMeshGen.PlaneFace.Front, true, 3);
                                else
                                    RoadMeshGen.RoadMesh(0, pointsListSize, bezier, bezier.distVecList.Count, OBC.roadStyle, RoadMeshGen.PlaneFace.Back, true, 3);
                                break;
                        }

                        while (!RoadMeshGen.isProcessDone) { }
                    }
                }
            }
            isProcessDone = true;
            #endregion
        }

        void UpdateSpawnPointsList()
        {
            #region
            isProcessDone = false;
            InstantiateObjectUsingBezierCurve OBC = (InstantiateObjectUsingBezierCurve)target;
            InstantiateObjectVarious.UpdateSpawnPoints(OBC);
            while (InstantiateObjectVarious.isProcessDone) { }

            SceneView.RepaintAll();
            isProcessDone = true;
            #endregion
        }

        void SelectAPartOfThePathSection()
        {
            #region

            EditorGUILayout.LabelField("");
            EditorGUILayout.LabelField("MORE OPTIONS:", EditorStyles.boldLabel);

            InstantiateObjectUsingBezierCurve OBC = (InstantiateObjectUsingBezierCurve)target;

            if (GUILayout.Button("Update Path", GUILayout.Height(30)))
            {
                UpdateSpawnPointsList();
                while (!isProcessDone) { }
            }

            if (OBC.distVecListPlusOffsetFinal.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Start: ", GUILayout.Width(100));
                m_startPathPos.intValue = EditorGUILayout.IntSlider(m_startPathPos.intValue, 0, m_distVecListPlusOffsetFinal.arraySize - 1);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("End: ", GUILayout.Width(100));
                m_endPathPos.intValue = EditorGUILayout.IntSlider(m_endPathPos.intValue, 0, m_distVecListPlusOffsetFinal.arraySize - 1);
                EditorGUILayout.EndHorizontal();

            }
            // Prevent Bugs
            if (m_startPathPos.intValue > m_endPathPos.intValue) m_startPathPos.intValue = m_endPathPos.intValue;
            if (m_endPathPos.intValue < m_startPathPos.intValue) m_endPathPos.intValue = m_startPathPos.intValue;

            if (m_startPathPos.intValue >= OBC.distVecListPlusOffsetFinal.Count) m_startPathPos.intValue = Mathf.Clamp(m_distVecListPlusOffsetFinal.arraySize - 1, 0, m_distVecListPlusOffsetFinal.arraySize);
            if (m_endPathPos.intValue >= OBC.distVecListPlusOffsetFinal.Count) m_endPathPos.intValue = Mathf.Clamp(m_distVecListPlusOffsetFinal.arraySize - 1, 0, m_distVecListPlusOffsetFinal.arraySize);
            #endregion
        }

        void OnSceneGUI()
        {
            #region
            if (m_moreOptions.boolValue)
                DisplayPathSelection();
            #endregion
        }

        void DisplayPathSelection()
        {
            #region
            InstantiateObjectUsingBezierCurve OBC = (InstantiateObjectUsingBezierCurve)target;

            if (OBC.distVecListPlusOffsetFinal.Count > 1)
            {
                float size = HandleUtility.GetHandleSize(OBC.distVecListPlusOffsetFinal[0].spotPos + OBC.transform.position);
                Handles.color = Color.green;

#if UNITY_2022_OR_NEWER
            Handles.FreeMoveHandle(OBC.distVecListPlusOffsetFinal[m_startPathPos.intValue].spotPos+ OBC.transform.position, size * .1f, Vector3.zero, Handles.SphereHandleCap);
#else
                var fmh_506_130_638326399978671349 = Quaternion.identity; Handles.FreeMoveHandle(OBC.distVecListPlusOffsetFinal[m_startPathPos.intValue].spotPos + OBC.transform.position, size * .1f, Vector3.zero, Handles.SphereHandleCap); ;
#endif

#if UNITY_2022_OR_NEWER
            Handles.FreeMoveHandle(OBC.distVecListPlusOffsetFinal[m_endPathPos.intValue].spotPos + OBC.transform.position, size * .1f, Vector3.zero, Handles.SphereHandleCap);
#else
                var fmh_512_128_638326399978675556 = Quaternion.identity; Handles.FreeMoveHandle(OBC.distVecListPlusOffsetFinal[m_endPathPos.intValue].spotPos + OBC.transform.position, size * .1f, Vector3.zero, Handles.SphereHandleCap);
#endif
            }
            #endregion
        }
    }
}

#endif

