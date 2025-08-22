//Description: RoadCrossEditor: Custom Editor
#if (UNITY_EDITOR)
using UnityEngine;
using UnityEditor;

using System.Collections.Generic;

namespace HP.Generics
{
    [CustomEditor(typeof(RoadCross))]
    public class RoadCrossEditor : Editor
    {
        SerializedProperty SeeInspector;                                            // use to draw default Inspector

        SerializedProperty m_borderWidth;
        SerializedProperty m_borderSize;
        SerializedProperty m_borderSlopeSize;
        SerializedProperty m_coverOffset;
        SerializedProperty m_currentroadCrossGroundPreset;
        SerializedProperty m_showTerrainBorderParams;
        SerializedProperty m_indexCrossRoadGroundPrefab;

        private Vector3 lastCrossRoadPosition = Vector3.zero;
        private Quaternion lastCrossRoadRotation = Quaternion.identity;

        public bool showGizmo = false;

        private bool isProcessDone = true;

        void OnEnable()
        {
            #region
            // Setup the SerializedProperties.
            SeeInspector = serializedObject.FindProperty("seeInspector");

            m_borderWidth = serializedObject.FindProperty("borderWidth");
            m_borderSize = serializedObject.FindProperty("borderSize");
            m_borderSlopeSize = serializedObject.FindProperty("borderSlopeSize");
            m_coverOffset = serializedObject.FindProperty("coverOffset");
            m_currentroadCrossGroundPreset = serializedObject.FindProperty("currentroadCrossGroundPreset");
            m_showTerrainBorderParams = serializedObject.FindProperty("showTerrainBorderParams");
            m_indexCrossRoadGroundPrefab = serializedObject.FindProperty("indexCrossRoadGroundPrefab");

            RoadCross myScript = (RoadCross)target;
            lastCrossRoadPosition = myScript.transform.position;
            lastCrossRoadRotation = myScript.transform.rotation;

            #endregion
        }

        public override void OnInspectorGUI()
        {
            #region
            if (SeeInspector.boolValue)
                DrawDefaultInspector();

            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Show Inspector: ", GUILayout.Width(120));
            EditorGUILayout.PropertyField(SeeInspector, new GUIContent(""));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("");

            UpdateTerrainSection();
            CreateNewRoadSection();
            CrossRoadManipulation();

            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.LabelField("");
            #endregion
        }

        void CrossRoadManipulation()
        {
            #region
            EditorGUILayout.LabelField("");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cross Road Manipulation: ", EditorStyles.boldLabel);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Rotate: ", GUILayout.Width(150));

            if (GUILayout.Button("+90", GUILayout.MinWidth(30)))
                RotateCrossRoadTheGround(90);
            if (GUILayout.Button("-90", GUILayout.MinWidth(30)))
                RotateCrossRoadTheGround(-90);
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Replace Cross Road with: ", GUILayout.Width(150));

            RoadCross roadCross = (RoadCross)target;
            List<string> groundPrefabList = new List<string>();
            for (var i = 0; i < roadCross.roadData.crossRoadGroundPrefabList.Count; i++)
                groundPrefabList.Add(roadCross.roadData.crossRoadGroundPrefabList[i].name);

            m_indexCrossRoadGroundPrefab.intValue = EditorGUILayout.Popup(m_indexCrossRoadGroundPrefab.intValue, groundPrefabList.ToArray(), GUILayout.MinWidth(50));
            if (GUILayout.Button("Apply", GUILayout.MinWidth(40)))
                ReplaceWithNewGround();

            EditorGUILayout.EndHorizontal();
            #endregion
        }

        void RotateCrossRoadTheGround(int angle)
        {
            #region
            RoadCross roadCross = (RoadCross)target;
            Undo.RegisterFullObjectHierarchyUndo(roadCross.gameObject, "roadCross");
            roadCross.grpDecal.transform.GetChild(1).GetChild(0).eulerAngles += new Vector3(0, angle, 0);
            #endregion
        }

        void ReplaceWithNewGround()
        {
            #region
            RoadCross roadCross = (RoadCross)target;

            roadCross.grpDecal.transform.GetChild(0).gameObject.SetActive(false);

            Undo.RegisterFullObjectHierarchyUndo(roadCross.gameObject, roadCross.name);

            if (roadCross.grpDecal.transform.GetChild(1).childCount > 0)
                Undo.DestroyObjectImmediate(roadCross.grpDecal.transform.GetChild(1).GetChild(0).gameObject);

            GameObject crossRoadGround = (GameObject)PrefabUtility.InstantiatePrefab(roadCross.roadData.crossRoadGroundPrefabList[m_indexCrossRoadGroundPrefab.intValue] as GameObject, roadCross.grpDecal.transform.GetChild(1));
            Undo.RegisterCreatedObjectUndo(crossRoadGround, "crossRoadGround");

            // Setup ground and anchors
            CrossRoadPreset crossRoadPreset = roadCross.grpDecal.transform.GetChild(1).GetChild(0).GetComponent<CrossRoadPreset>();
            for (var i = 0; i < roadCross.anchorList.Count; i++)
                roadCross.anchorList[i].localPosition = crossRoadPreset.anchorPosList[i];

            roadCross.objCollider.transform.localScale = crossRoadPreset.colliderTransformScale;

            roadCross.currentroadCrossGroundPreset = crossRoadPreset.roadTypeCreatedByDefault;
            roadCross.anchorDistWhenNewPointCreated = crossRoadPreset.anchorDistWhenNewPointCreated;
            #endregion
        }

        void UpdateTerrainSection()
        {
            #region
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(m_showTerrainBorderParams, new GUIContent(""), GUILayout.Width(20));
            EditorGUILayout.LabelField("Update Terrain: ", EditorStyles.boldLabel);

            EditorGUILayout.EndHorizontal();

            if (m_showTerrainBorderParams.boolValue)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Border Width: ", GUILayout.Width(120));
                EditorGUILayout.PropertyField(m_borderWidth, new GUIContent(""));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Border Size: ", GUILayout.Width(120));
                EditorGUILayout.PropertyField(m_borderSize, new GUIContent(""));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Border Slope Size: ", GUILayout.Width(120));
                EditorGUILayout.PropertyField(m_borderSlopeSize, new GUIContent(""));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Cover Offset: ", GUILayout.Width(120));
                EditorGUILayout.PropertyField(m_coverOffset, new GUIContent(""));
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Road from start to end", GUILayout.Height(30)))
                UpdateTerrain(true);
            if (GUILayout.Button("Only last segment", GUILayout.Height(30)))
                UpdateTerrain();
            #endregion
        }

        void OnSceneGUI()
        {
            #region
            serializedObject.Update();

            PositionChangeCheck();
            Move();
            CheckInput();

            serializedObject.ApplyModifiedProperties();
            #endregion
        }

        void Move()
        {
            #region
            RoadCross myScript = (RoadCross)target;
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            Vector3 pointPos = myScript.transform.position;
            float size = HandleUtility.GetHandleSize(pointPos);
            Vector3 newTargetPosition = Vector3.zero;

            Vector3 newGizmoPos = Vector3.zero;

            if (showGizmo)
            {
                Handles.color = Color.red;
#if UNITY_2022_OR_NEWER
            newTargetPosition = Handles.FreeMoveHandle(pointPos, size * .2f, Vector3.zero, Handles.SphereHandleCap);
#else
                var fmh_215_70_638326399978762595 = Quaternion.identity; newTargetPosition = Handles.FreeMoveHandle(pointPos, size * .2f, Vector3.zero, Handles.SphereHandleCap);
#endif
            }
            else
                newTargetPosition = Handles.DoPositionHandle(pointPos, Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                if (newTargetPosition != pointPos)
                {
                    // Move Center Control Point
                    Undo.RegisterFullObjectHierarchyUndo(myScript.gameObject, "Cross");
                    myScript.transform.position = newTargetPosition;
                }
            }

            serializedObject.ApplyModifiedProperties();
            #endregion
        }
        void PositionChangeCheck()
        {
            #region
            RoadCross myScript = (RoadCross)target;

            if (lastCrossRoadPosition != myScript.transform.position || lastCrossRoadRotation != myScript.transform.rotation)
                UpdateFirstAndLastPointPosition(myScript);

            lastCrossRoadPosition = myScript.transform.position;
            lastCrossRoadRotation = myScript.transform.rotation;
            #endregion
        }
        void UpdateFirstAndLastPointPosition(RoadCross myScript)
        {
            #region
            isProcessDone = false;
            for (var i = 0; i < myScript.roadList.Count; i++)
            {
                if (myScript.roadList[i].road)
                    Undo.RegisterFullObjectHierarchyUndo(myScript.roadList[i].road.gameObject, "road");

                if (myScript.roadList[i].road && myScript.roadList[i].roadStartPoint == RoadCross.Direction.End)
                {
                    int howMany = myScript.roadList[i].road.pointsList.Count;
                    // Point
                    myScript.roadList[i].road.pointsList[howMany - 1].points = myScript.anchorList[i].position - myScript.roadList[i].road.transform.position;
                    // Tangent
                    myScript.roadList[i].road.pointsList[howMany - 2].points = myScript.anchorList[i].position - myScript.roadList[i].road.transform.position
                        + myScript.anchorList[i].forward * myScript.roadList[i].road.lastAnchorDist;
                }

                if (myScript.roadList[i].road && myScript.roadList[i].roadStartPoint == RoadCross.Direction.Start)
                {
                    // Point
                    myScript.roadList[i].road.pointsList[0].points = myScript.anchorList[i].position - myScript.roadList[i].road.transform.position;
                    // Tangent
                    myScript.roadList[i].road.pointsList[1].points = myScript.anchorList[i].position - myScript.roadList[i].road.transform.position
                        + myScript.anchorList[i].forward * myScript.roadList[i].road.lastAnchorDist;
                }

            }
            isProcessDone = true;
            #endregion
        }
        void UpdateRoadsPosition()
        {
            #region
            RoadCross myScript = (RoadCross)target;

            UpdateFirstAndLastPointPosition(myScript);

            while (!isProcessDone) { }

            for (var i = 0; i < myScript.roadList.Count; i++)
            {
                if (myScript.roadList[i].road)
                {
                    Bezier bezier = myScript.roadList[i].road;
                    RoadMeshGen.UpdateRoadFromCrossRoadEditor(bezier, true, myScript.roadSubdivisionWhenGenerated);
                }
            }
            UpdateCrossRoadBorder();
            #endregion
        }
        void UpdateTerrain(bool updateRoadFromStarToEnd = false)
        {
            #region
            RoadCross myScript = (RoadCross)target;

            UpdateRoadsPosition();

            for (var i = 0; i < myScript.roadList.Count; i++)
                if (myScript.roadList[i].road)
                    RoadMeshGen.SetSelectionColliders(myScript, i, updateRoadFromStarToEnd);

            for (var i = 0; i < myScript.roadList.Count; i++)
            {
                if (myScript.roadList[i].road)
                {
                    Bezier bezier = myScript.roadList[i].road;

                    RoadMeshGen.ReturnTotalCurveDistance(bezier);
                    while (!RoadMeshGen.isProcessDone) { }

                    RoadMeshGen.RoadBorderMesh(0, 0, "Right", bezier.selectBorderTrans[0], bezier, bezier.distVecList.Count, bezier.selectStart, bezier.selectStop);
                    RoadMeshGen.RoadBorderMesh(0, 0, "Left", bezier.selectBorderTrans[1], bezier, bezier.distVecList.Count, bezier.selectStart, bezier.selectStop);
                    RoadMeshGen.RoadBorderMesh(0, 0, "RightSlope", bezier.selectBorderTrans[2], bezier, bezier.distVecList.Count, bezier.selectStart, bezier.selectStop);
                    RoadMeshGen.RoadBorderMesh(0, 0, "LeftSlope", bezier.selectBorderTrans[3], bezier, bezier.distVecList.Count, bezier.selectStart, bezier.selectStop);

                    RoadMeshGen.RoadBorderMesh(0, 0, "SelectionRoadSection", bezier.selectBorderTrans[4], bezier, bezier.distVecList.Count, bezier.selectStart, bezier.selectStop);
                }
            }

            for (var i = 0; i < myScript.roadList.Count; i++)
                if (myScript.roadList[i].road)
                    TerrainUpdater.UpdateTerrainList(myScript.roadList[i].road, myScript.roadList[i].road.distVecList.Count);

            for (var i = 0; i < myScript.roadList.Count; i++)
                if (myScript.roadList[i].road)
                    TerrainUpdater.UpdateHeight(myScript.roadList[i].road.gameObject.GetComponent<TerrainModif>(), true, myScript);

            TerrainUpdater.UpdateTerrainProcessDoneFeedback();
            #endregion
        }
        void CheckInput()
        {
            #region
            // Update 
            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.U)
                UpdateRoadsPosition();

            // Show Gizmo 
            showGizmo = Event.current.shift ? false : true;
            #endregion
        }
        void CreateNewRoadSection()
        {
            #region
            RoadCross myScript = (RoadCross)target;
            EditorGUILayout.LabelField("");

            EditorGUILayout.LabelField("Create New Road: ", EditorStyles.boldLabel);

            List<string> groundPrefabList = new List<string>();
            for (var i = 0; i < myScript.roadData.crossRoadGroundPrefabList.Count; i++)
                groundPrefabList.Add(myScript.roadData.crossRoadGroundPrefabList[i].name);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Select road type:", GUILayout.Width(100));
            m_currentroadCrossGroundPreset.intValue = EditorGUILayout.Popup(m_currentroadCrossGroundPreset.intValue, groundPrefabList.ToArray(), GUILayout.MinWidth(70));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("");

            Rect lastRect = GUILayoutUtility.GetLastRect();
            EditorGUI.DrawRect(new Rect(20, lastRect.y + 19, 19, 19), Color.blue);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(20));
            if (myScript.roadList[0].road)
                EditorGUILayout.LabelField("A Road is already connected", GUILayout.MinWidth(100));
            else if (GUILayout.Button("Create New Road Section"))
                NewSection(0, 2);
            EditorGUILayout.EndHorizontal();

            lastRect = GUILayoutUtility.GetLastRect();
            EditorGUI.DrawRect(new Rect(20, lastRect.y + 19, 19, 19), Color.yellow);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(20));
            if (myScript.roadList[1].road)
                EditorGUILayout.LabelField("A Road is already connected", GUILayout.MinWidth(100));
            else if (GUILayout.Button("Create New Road Section"))
                NewSection(1, 3);
            EditorGUILayout.EndHorizontal();

            lastRect = GUILayoutUtility.GetLastRect();
            EditorGUI.DrawRect(new Rect(20, lastRect.y + 19, 19, 19), Color.green);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(20));
            if (myScript.roadList[2].road)
                EditorGUILayout.LabelField("A Road is already connected", GUILayout.MinWidth(100));
            else if (GUILayout.Button("Create New Road Section"))
                NewSection(2, 0);
            EditorGUILayout.EndHorizontal();

            lastRect = GUILayoutUtility.GetLastRect();
            EditorGUI.DrawRect(new Rect(20, lastRect.y + 19, 19, 19), Color.magenta);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(20));

            if (myScript.roadList[3].road)
                EditorGUILayout.LabelField("A Road is already connected", GUILayout.MinWidth(100));
            else if (GUILayout.Button("Create New Road Section"))
                NewSection(3, 1);
            EditorGUILayout.EndHorizontal();

            #endregion
        }
        void NewSection(int anchorPos, int anchorDir)
        {
            #region
            RoadCross myScript = (RoadCross)target;

            GameObject roadObj = (GameObject)PrefabUtility.InstantiatePrefab(myScript.roadData.roadPrefabList[m_currentroadCrossGroundPreset.intValue] as GameObject);
            Undo.RegisterCreatedObjectUndo(roadObj, "roadObj");



            roadObj.transform.SetAsLastSibling();
            roadObj.name = "Road_" + myScript.roadData.iD;

            Bezier bezier = roadObj.GetComponent<Bezier>();

            bezier.roadID = myScript.roadData.iD;

            SerializedObject serializedObject2 = new UnityEditor.SerializedObject(myScript.roadData);
            SerializedProperty m_iD = serializedObject2.FindProperty("iD");
            serializedObject2.Update();
            m_iD.intValue++;
            serializedObject2.ApplyModifiedProperties();

            for (var i = 0; i < 4; i++) bezier.pointsList.Add(new PointDescription(Vector3.zero, Quaternion.identity));// m_pointsList.InsertArrayElementAtIndex(0);

            Vector3 dir = (myScript.anchorList[anchorPos].position - myScript.anchorList[anchorDir].position).normalized;

            roadObj.transform.position = myScript.anchorList[anchorPos].position;

            for (var i = 0; i < 4; i++)
                bezier.pointsList[i].points = myScript.anchorList[anchorPos].position - roadObj.transform.position + dir * myScript.anchorDistWhenNewPointCreated * i;

            myScript.roadList[anchorPos].road = bezier;
            myScript.roadList[anchorPos].roadStartPoint = RoadCross.Direction.Start;

            bezier.crossRoadpoint = myScript.transform;
            if (anchorPos == 0) bezier.crossDirection = Bezier.CrossDirection.Blue;
            if (anchorPos == 1) bezier.crossDirection = Bezier.CrossDirection.Yellow;
            if (anchorPos == 2) bezier.crossDirection = Bezier.CrossDirection.Green;
            if (anchorPos == 3) bezier.crossDirection = Bezier.CrossDirection.Majenta;

            while (bezier.pointsList.Count != 4) { }

            PrefabUtility.RecordPrefabInstancePropertyModifications(myScript);

            RoadMeshGen.UpdateRoadFromCrossRoadEditor(bezier, true);

            UpdateCrossRoadBorder(roadObj);
            #endregion
        }

        void UpdateCrossRoadBorder(GameObject selectedObjectInHierarchy = null)
        {
            #region
            RoadCross roadCross = (RoadCross)target;

            for (var i = 0; i < roadCross.crossRoadBorders.Count; i++)
            {
                roadCross.crossRoadBorders[i].GetComponent<MeshFilter>().sharedMesh = null;
                roadCross.crossRoadBorders[i].GetComponent<MeshCollider>().sharedMesh = null;
            }

            if (roadCross.roadList[0].road == null)
                RoadMeshGen.UpdateCrossBorderMesh(roadCross, 0, 1, 2, 3);
            while (!RoadMeshGen.isProcessDone) { }
            if (roadCross.roadList[1].road == null)
                RoadMeshGen.UpdateCrossBorderMesh(roadCross, 1, 2, 3, 0);
            while (!RoadMeshGen.isProcessDone) { }
            if (roadCross.roadList[2].road == null)
                RoadMeshGen.UpdateCrossBorderMesh(roadCross, 2, 3, 0, 1);
            while (!RoadMeshGen.isProcessDone) { }
            if (roadCross.roadList[3].road == null)
                RoadMeshGen.UpdateCrossBorderMesh(roadCross, 3, 0, 1, 2);
            while (!RoadMeshGen.isProcessDone) { }

            if (roadCross.roadList[0].road == null)
                RoadMeshGen.UpdateCrossBorderSlopeMesh(roadCross, 4, 5, 6, 7);
            while (!RoadMeshGen.isProcessDone) { }
            if (roadCross.roadList[1].road == null)
                RoadMeshGen.UpdateCrossBorderSlopeMesh(roadCross, 5, 6, 7, 4);
            while (!RoadMeshGen.isProcessDone) { }
            if (roadCross.roadList[2].road == null)
                RoadMeshGen.UpdateCrossBorderSlopeMesh(roadCross, 6, 7, 4, 5);
            while (!RoadMeshGen.isProcessDone) { }
            if (roadCross.roadList[3].road == null)
                RoadMeshGen.UpdateCrossBorderSlopeMesh(roadCross, 7, 4, 5, 6);

            while (!RoadMeshGen.isProcessDone) { }

            if (selectedObjectInHierarchy)
            {
                Selection.activeGameObject = selectedObjectInHierarchy;
                SceneView.lastActiveSceneView.Focus();
            }
            #endregion
        }
    }
}

#endif

