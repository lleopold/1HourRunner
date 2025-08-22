//Description: BezierEditor: Custom Editor
#if (UNITY_EDITOR)
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace HP.Generics
{
    [CustomEditor(typeof(Bezier))]
    public class BezierEditor : Editor
    {
        SerializedProperty SeeInspector;                                            // use to draw default Inspector
        SerializedProperty seeCustomEditor;

        SerializedProperty m_toolbarIndex;
        SerializedProperty m_pointsList;

        SerializedProperty m_linkControlPoints;
        SerializedProperty m_loop;

        SerializedProperty m_totalDistance;
        SerializedProperty m_tileSize;
        SerializedProperty m_roadSize;

        SerializedProperty m_distVecList;

        SerializedProperty m_CrossRoadpoint;
        SerializedProperty m_CrossRoadpointIn;
        SerializedProperty m_lastAnchorDist;
        SerializedProperty m_anchorDistWhenNewPointCreated;
        SerializedProperty m_CrossDirection;
        SerializedProperty m_CrossDirectionIn;

        SerializedProperty m_bSelection;
        SerializedProperty m_selectStart;
        SerializedProperty m_selectStop;

        SerializedProperty m_hotControlID;
        SerializedProperty m_closestPoint;

        SerializedProperty m_IsChangeDone;

        SerializedProperty m_closestPointList;
        SerializedProperty m_indexCreatorCrossRoad;
        SerializedProperty m_indexCrossRoadTab;

        SerializedProperty m_indexConnectionCrossRoadTab;

        SerializedProperty m_selectBorderInfo;

        SerializedProperty m_indexCrossRoadGroundPrefab;

        SerializedProperty m_isRoadMeshUpdated;

        public List<int> crossRoadIDList = new List<int>();
        public List<string> crossRoadNameList = new List<string>();
        public List<RoadCross> crossRoadObjList = new List<RoadCross>();

        public int index = 0;

        string[] toolbarNames = { "Point Manipulation", "Road Selection", "Road Crossing", "Other" };
        string[] anchorBlackAndWhite = { "Start", "End" };
        string[] crossRoadTab = { "Create", "Connect", "Point to Road Crossing" };
        string[] connectCrossRoadTab = { "Blue", "Yellow", "Green", "Magenta" };

        public bool showGizmo = false;
        public bool isCtrlPressed = false;
        public bool isShowTangent = false;

        public bool isProcessDone = false;
        private bool isApplySerializedModification = true;


        void OnEnable()
        {
            #region
            // Setup the SerializedProperties.
            SeeInspector = serializedObject.FindProperty("seeInspector");
            seeCustomEditor = serializedObject.FindProperty("seeCustomEditor");
            m_toolbarIndex = serializedObject.FindProperty("toolbarIndex");
            m_pointsList = serializedObject.FindProperty("pointsList");

            m_linkControlPoints = serializedObject.FindProperty("linkControlPoints");
            m_loop = serializedObject.FindProperty("loop");
            m_totalDistance = serializedObject.FindProperty("totalDistance");
            m_tileSize = serializedObject.FindProperty("tileSize");
            m_roadSize = serializedObject.FindProperty("roadSize");

            m_distVecList = serializedObject.FindProperty("distVecList");

            m_CrossRoadpoint = serializedObject.FindProperty("crossRoadpoint");
            m_CrossRoadpointIn = serializedObject.FindProperty("crossRoadpointIn");
            m_CrossDirection = serializedObject.FindProperty("crossDirection");
            m_CrossDirectionIn = serializedObject.FindProperty("crossDirectionIn");

            m_lastAnchorDist = serializedObject.FindProperty("lastAnchorDist");
            m_anchorDistWhenNewPointCreated = serializedObject.FindProperty("anchorDistWhenNewPointCreated");
            m_bSelection = serializedObject.FindProperty("bSelection");
            m_selectStart = serializedObject.FindProperty("selectStart");
            m_selectStop = serializedObject.FindProperty("selectStop");

            m_hotControlID = serializedObject.FindProperty("hotControlID");
            m_closestPoint = serializedObject.FindProperty("closestPoint");

            m_IsChangeDone = serializedObject.FindProperty("isChangeDone");

            m_closestPointList = serializedObject.FindProperty("closestPointList");

            m_indexCreatorCrossRoad = serializedObject.FindProperty("indexCreatorCrossRoad");
            m_indexCrossRoadTab = serializedObject.FindProperty("indexCrossRoadTab");
            m_indexConnectionCrossRoadTab = serializedObject.FindProperty("indexConnectionCrossRoadTab");

            m_selectBorderInfo = serializedObject.FindProperty("selectBorderInfo");

            m_indexCrossRoadGroundPrefab = serializedObject.FindProperty("indexCrossRoadGroundPrefab");

            m_isRoadMeshUpdated = serializedObject.FindProperty("isRoadMeshUpdated");
            #endregion
        }

        private void OnDisable()
        {
            #region
            Bezier bezier = (Bezier)target;
            if (bezier)
            {
                serializedObject.Update();
                m_closestPointList.ClearArray();
                m_closestPoint.intValue = -1;
                serializedObject.ApplyModifiedProperties();
            }
            #endregion
        }

        public override void OnInspectorGUI()
        {
            #region
            if (SeeInspector.boolValue)
                DrawDefaultInspector();

            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            if (!SeeInspector.boolValue)
            {

                EditorGUILayout.LabelField("Show Inspector: ", GUILayout.Width(100));
                EditorGUILayout.PropertyField(SeeInspector, new GUIContent(""), GUILayout.Width(20));

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Custom Editor: ", GUILayout.Width(100));
                EditorGUILayout.PropertyField(seeCustomEditor, new GUIContent(""), GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndHorizontal();

            if (seeCustomEditor.boolValue)
            {
                InfoHelper();

                m_toolbarIndex.intValue = GUILayout.SelectionGrid(m_toolbarIndex.intValue, toolbarNames, 2, GUILayout.MinWidth(120));

                if (m_toolbarIndex.intValue == 0)
                    HotControl();

                if (m_toolbarIndex.intValue == 1)
                    SelectionSection();

                if (m_toolbarIndex.intValue == 2)
                    AddCrossRoadSection();

                if (m_toolbarIndex.intValue == 3)
                    Other();
            }


            if (isApplySerializedModification)
                serializedObject.ApplyModifiedProperties();
            #endregion
        }

        void DisplayBezierPath()
        {
            #region
            for (var i = 0; i < m_pointsList.arraySize - 1; i += 3)
            {
                Handles.color = Color.red;
                Handles.DrawBezier(SListPoint(i), SListPoint(i + 3), SListPoint(i + 1), SListPoint(i + 2), Color.red, null, 1);
            }
            #endregion
        }

        void DrawCrossRoadConnection()
        {
            #region
            if (crossRoadObjList.Count > 0 && crossRoadObjList[index] && m_pointsList.arraySize > 0)
            {
                Bezier bezier = (Bezier)target;
                Handles.color = Color.white;
                float size = HandleUtility.GetHandleSize(crossRoadObjList[index].transform.position);
                Handles.SphereHandleCap(0, crossRoadObjList[index].transform.position, Quaternion.identity, size * .4f, EventType.Repaint);

                // Create a line to show connection between road and cross
                if (m_indexCreatorCrossRoad.intValue == 0)
                    Handles.DrawLine(
                    crossRoadObjList[index].anchorList[m_indexConnectionCrossRoadTab.intValue].transform.position,
                    m_pointsList.GetArrayElementAtIndex(0).FindPropertyRelative("points").vector3Value + bezier.transform.position, .4f);

                if (m_indexCreatorCrossRoad.intValue == 1)
                    Handles.DrawLine(
                    crossRoadObjList[index].anchorList[m_indexConnectionCrossRoadTab.intValue].transform.position,
                    m_pointsList.GetArrayElementAtIndex(m_pointsList.arraySize - 1).FindPropertyRelative("points").vector3Value + bezier.transform.position, .4f);
            }
            #endregion
        }

        void DrawCurvePointsAndTangents()
        {
            #region
            Bezier bezier = (Bezier)target;
            Handles.color = Color.red;

            for (var i = 0; i < HowManyCurvePoints(); i++)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 pointPos = SListPoint(i);
                float size = HandleUtility.GetHandleSize(pointPos);

                Vector3 newTargetPosition = Vector3.zero;

                Vector3 newGizmoPos = Vector3.zero;

                // Draw Points
                if (i % 3 == 0)
                {
                    // Draw First Point
                    if (m_CrossRoadpoint.objectReferenceValue && i == 0)
                    {
                        if (m_closestPoint.intValue == i)
                            Handles.color = Color.green;
                        else if (IsSpotInTheSelectionSpotsList(i))
                            Handles.color = Color.blue;
                        else
                            Handles.color = Color.white;

#if UNITY_2022_OR_NEWER
                    newTargetPosition = Handles.FreeMoveHandle(pointPos, size * .3f, Vector3.zero, Handles.SphereHandleCap);
#else
                        var fmh_249_78_638326399981295194 = Quaternion.identity; newTargetPosition = Handles.FreeMoveHandle(pointPos, size * .3f, Vector3.zero, Handles.SphereHandleCap);
#endif

                    }
                    // Draw Last points
                    else if (m_CrossRoadpointIn.objectReferenceValue && i == HowManyCurvePoints() - 1)
                    {
                        if (m_closestPoint.intValue == i)
                            Handles.color = Color.green;
                        else if (IsSpotInTheSelectionSpotsList(i))
                            Handles.color = Color.blue;
                        else
                            Handles.color = Color.black;
#if UNITY_2022_OR_NEWER
                    newTargetPosition = Handles.FreeMoveHandle(pointPos, size * .2f, Vector3.zero, Handles.SphereHandleCap);
#else
                        var fmh_265_78_638326399981299291 = Quaternion.identity; newTargetPosition = Handles.FreeMoveHandle(pointPos, size * .2f, Vector3.zero, Handles.SphereHandleCap);
#endif

                    }
                    // Draw other points
                    else
                    {
                        if (m_loop.boolValue && i == m_pointsList.arraySize - 1)
                        {

                        }
                        else if (showGizmo || !showGizmo && isShowTangent)
                        {
                            if (m_closestPoint.intValue == i)
                                Handles.color = Color.green;
                            else if (IsSpotInTheSelectionSpotsList(i))
                                Handles.color = Color.blue;
                            else
                                Handles.color = Color.red;

                            if (i == 0)
                            {
#if UNITY_2022_OR_NEWER
                            newTargetPosition = Handles.FreeMoveHandle(pointPos, size * .3f, Vector3.zero, Handles.SphereHandleCap); 
#else
                                var fmh_290_86_638326399981303465 = Quaternion.identity; newTargetPosition = Handles.FreeMoveHandle(pointPos, size * .3f, Vector3.zero, Handles.SphereHandleCap);
#endif

                            }
                            else
                            {
#if UNITY_2022_OR_NEWER
                            newTargetPosition = Handles.FreeMoveHandle(pointPos, size * .2f, Vector3.zero, Handles.SphereHandleCap); 
#else
                                var fmh_299_86_638326399981307404 = Quaternion.identity; newTargetPosition = Handles.FreeMoveHandle(pointPos, size * .2f, Vector3.zero, Handles.SphereHandleCap);
#endif

                            }
                        }
                        else
                        {
                            if (!isShowTangent)
                                newTargetPosition = Handles.DoPositionHandle(pointPos, Quaternion.identity);
                        }
                    }
                }
                // Draw Tangents
                else
                {
                    // First Tangent
                    if (m_CrossRoadpoint.objectReferenceValue && i == 1)
                    {
                        Handles.color = Color.white;
                        Handles.CubeHandleCap(0, pointPos, Quaternion.identity, size * .1f, EventType.Repaint);
                    }
                    // Last Tangent
                    else if (m_CrossRoadpointIn.objectReferenceValue && i == HowManyCurvePoints() - 2)
                    {
                        Handles.color = Color.black;
                        Handles.CubeHandleCap(0, pointPos, Quaternion.identity, size * .1f, EventType.Repaint);
                    }
                    // Other Tangents
                    else
                    {
                        Handles.color = Color.white;
                        if (showGizmo || !showGizmo && !isShowTangent)
                        {
#if UNITY_2022_OR_NEWER
                         newTargetPosition = Handles.FreeMoveHandle(pointPos, size * .1f, Vector3.zero, Handles.CubeHandleCap); 
#else
                            var fmh_335_82_638326399981311583 = Quaternion.identity; newTargetPosition = Handles.FreeMoveHandle(pointPos, size * .1f, Vector3.zero, Handles.SphereHandleCap);
#endif

                        }
                        else
                        {
                            Vector3 relativePos = Vector3.zero;
                            Quaternion rotation = Quaternion.identity;

                            if (isShowTangent)
                            {
                                if (i % 3 == 2)
                                    relativePos = SListPoint(i + 1) - SListPoint(i);
                                else if (i % 3 == 1)
                                    relativePos = SListPoint(i - 1) - SListPoint(i);

                                rotation = Quaternion.LookRotation(relativePos, Vector3.up);

                                newTargetPosition = Handles.DoPositionHandle(pointPos, rotation);
                            }

                        }
                    }
                }

                // Update Handle position if a point or a tangent is moved
                if (EditorGUI.EndChangeCheck())
                {
                    if (newTargetPosition != pointPos)
                    {
                        if (m_CrossRoadpoint.objectReferenceValue && i == 0 ||
                        m_CrossRoadpointIn.objectReferenceValue && i == HowManyCurvePoints() - 1)
                        {

                        }
                        else
                        {
                            MoveSelectedPoints(i, m_pointsList.GetArrayElementAtIndex(i).FindPropertyRelative("points").vector3Value, newTargetPosition - bezier.transform.position);

                            // Move Center Control Point
                            m_pointsList.GetArrayElementAtIndex(i).FindPropertyRelative("points").vector3Value = newTargetPosition - bezier.transform.position;

                            // A center point is moving. Move Left and Right Control points if needed
                            if (i % 3 == 0)
                            {
                                // Move Left and Right Control points
                                if (i - 1 > 0 && i + 1 < HowManyCurvePoints())
                                {
                                    float distL = Vector3.Distance(SListPoint(i - 1), pointPos);
                                    Vector3 dirL = (SListPoint(i - 1) - pointPos).normalized;
                                    m_pointsList.GetArrayElementAtIndex(i - 1).FindPropertyRelative("points").vector3Value = newTargetPosition + distL * dirL - bezier.transform.position;

                                    float distR = Vector3.Distance(SListPoint(i + 1), pointPos);
                                    Vector3 dirR = (SListPoint(i + 1) - pointPos).normalized;

                                    if (!m_linkControlPoints.boolValue)
                                        m_pointsList.GetArrayElementAtIndex(i + 1).FindPropertyRelative("points").vector3Value = newTargetPosition + distR * dirR - bezier.transform.position;
                                    else
                                        m_pointsList.GetArrayElementAtIndex(i + 1).FindPropertyRelative("points").vector3Value = newTargetPosition - distL * dirL - bezier.transform.position;

                                    if (m_loop.boolValue && m_closestPoint.intValue == 0)
                                    {
                                        if (!m_linkControlPoints.boolValue)
                                        {

                                        }
                                        else
                                        {
                                            Vector3 dir = (newTargetPosition - pointPos).normalized;
                                            float dist = Vector3.Distance(newTargetPosition, pointPos);
                                            m_pointsList.GetArrayElementAtIndex(0).FindPropertyRelative("points").vector3Value += dist * dir/* - bezier.transform.position*/;
                                            m_pointsList.GetArrayElementAtIndex(1).FindPropertyRelative("points").vector3Value += dist * dir/* - bezier.transform.position*/;
                                            Debug.Log("Here 1");
                                        }
                                    }
                                }
                                // Loop: Left and Right Control points (First point or last point)
                                else if (m_loop.boolValue && i == 0)
                                {
                                    if (!m_linkControlPoints.boolValue)
                                    {

                                    }
                                    else
                                    {
                                        Vector3 dir = (newTargetPosition - pointPos).normalized;
                                        float dist = Vector3.Distance(newTargetPosition, pointPos);
                                        m_pointsList.GetArrayElementAtIndex(1).FindPropertyRelative("points").vector3Value += dist * dir/* - bezier.transform.position*/;
                                        m_pointsList.GetArrayElementAtIndex(m_pointsList.arraySize - 1).FindPropertyRelative("points").vector3Value += dist * dir/* - bezier.transform.position*/;
                                        m_pointsList.GetArrayElementAtIndex(m_pointsList.arraySize - 2).FindPropertyRelative("points").vector3Value += dist * dir/* - bezier.transform.position*/;

                                    }
                                }
                                // Move Left Control points
                                else if (i - 1 > 0)
                                {
                                    float distL = Vector3.Distance(SListPoint(i - 1), pointPos);
                                    Vector3 dirL = (SListPoint(i - 1) - pointPos).normalized;
                                    m_pointsList.GetArrayElementAtIndex(i - 1).FindPropertyRelative("points").vector3Value = newTargetPosition + distL * dirL - bezier.transform.position;
                                }
                                // Move Right Control Point
                                else if (i + 1 < HowManyCurvePoints())
                                {
                                    float distR = Vector3.Distance(SListPoint(i + 1), pointPos);
                                    Vector3 dirR = (SListPoint(i + 1) - pointPos).normalized;
                                    m_pointsList.GetArrayElementAtIndex(i + 1).FindPropertyRelative("points").vector3Value = newTargetPosition + distR * dirR - bezier.transform.position;
                                }
                            }

                            //  Link Left and Right movement if m_linkControlPoints = true
                            if (i % 3 != 0 && m_linkControlPoints.boolValue)
                            {
                                // Right Control point
                                if (((i - 1) % 3 == 0 && i - 2 > 0))
                                {
                                    float distR = Vector3.Distance(SListPoint(i - 1), pointPos);
                                    Vector3 dirR = (SListPoint(i - 1) - pointPos).normalized;
                                    m_pointsList.GetArrayElementAtIndex(i - 2).FindPropertyRelative("points").vector3Value = SListPoint(i - 1) + distR * dirR - bezier.transform.position;
                                }

                                // Left Control point
                                if (((i + 1) % 3 == 0 && i + 2 < HowManyCurvePoints()))
                                {
                                    float distL = Vector3.Distance(SListPoint(i + 1), pointPos);
                                    Vector3 dirL = (SListPoint(i + 1) - pointPos).normalized;
                                    m_pointsList.GetArrayElementAtIndex(i + 2).FindPropertyRelative("points").vector3Value = SListPoint(i + 1) + distL * dirL - bezier.transform.position;
                                }

                                // Loop: Right Control point
                                if (m_loop.boolValue && i == 1)
                                {
                                    float distR = Vector3.Distance(SListPoint(i - 1), pointPos);
                                    Vector3 dirR = (SListPoint(i - 1) - pointPos).normalized;
                                    m_pointsList.GetArrayElementAtIndex(Points(i - 3)).FindPropertyRelative("points").vector3Value = SListPoint(i - 1) + distR * dirR - bezier.transform.position;
                                }

                                // Loop: Right Control point
                                if (m_loop.boolValue && i == m_pointsList.arraySize - 2)
                                {
                                    float distL = Vector3.Distance(SListPoint(i + 1), pointPos);
                                    Vector3 dirL = (SListPoint(i + 1) - pointPos).normalized;
                                    m_pointsList.GetArrayElementAtIndex(Points(i + 3)).FindPropertyRelative("points").vector3Value = SListPoint(i + 1) + distL * dirL - bezier.transform.position;
                                }
                            }

                            // Loop (Move first point and last point at the same time)
                            if (m_loop.boolValue)
                            {
                                if (i == 0)
                                    m_pointsList.GetArrayElementAtIndex(m_pointsList.arraySize - 1).FindPropertyRelative("points").vector3Value = SListPoint(i) - bezier.transform.position;
                                if (i == m_pointsList.arraySize - 1)
                                    m_pointsList.GetArrayElementAtIndex(0).FindPropertyRelative("points").vector3Value = SListPoint(m_pointsList.arraySize - 1) - bezier.transform.position;
                            }
                            m_IsChangeDone.boolValue = true;
                        }
                    }
                }
            }
            #endregion
        }

        void OnSceneGUI()
        {
            #region
            CheckInput();
            serializedObject.Update();

            DisplayBezierPath();
            DrawCrossRoadConnection();
            DrawCurvePointsAndTangents();

            serializedObject.ApplyModifiedProperties();
            #endregion
        }

        void ResetBezierCurvePivot()
        {
            #region
            Bezier bezier = (Bezier)target;
            Undo.RegisterFullObjectHierarchyUndo(bezier.gameObject, bezier.name);

            float dist = Vector3.Distance(bezier.transform.position, bezier.transform.position + bezier.pointsList[0].points);
            Vector3 dir = (bezier.transform.position + bezier.pointsList[0].points - bezier.transform.position).normalized;

            Vector3 startPos = bezier.pointsList[0].points;
            Vector3 endPos = bezier.pointsList[3].points;
            Vector3 dirFirstSegment = (endPos - startPos).normalized;
            bezier.transform.position -= dirFirstSegment * 5;

            bezier.transform.position += dist * dir;

            for (var i = 0; i < bezier.pointsList.Count; i++)
                bezier.pointsList[i].points -= dist * dir - dirFirstSegment * 5;

            PrefabUtility.RecordPrefabInstancePropertyModifications(bezier);
            #endregion
        }

        void AddPoint()
        {
            #region
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;
            Vector3 dir = Vector3.zero;
            float dist = 0;
            Vector3 newPos = Vector3.zero;

            Bezier bezier = (Bezier)target;
            Undo.RegisterFullObjectHierarchyUndo(bezier.gameObject, bezier.name);

            if (Physics.Raycast(ray, out hit))
            {
                newPos = hit.point + Vector3.up * bezier.roadData.groundOffset;

                if (m_pointsList.arraySize != 0)
                {
                    dir = (newPos - SListPoint(bezier.pointsList.Count - 1)).normalized;
                    dist = Vector3.Distance(newPos, SListPoint(bezier.pointsList.Count - 1));
                }
            }

            // Create First Segment
            if (m_pointsList.arraySize < 4)
            {
                for (var i = 0; i < 2; i++) m_pointsList.InsertArrayElementAtIndex(0);

                for (var i = 0; i < 2; i++)
                    bezier.pointsList.Add(new PointDescription(newPos - bezier.transform.position, Quaternion.identity));

                if (m_pointsList.arraySize == 4)
                {
                    Vector3 startPos = bezier.pointsList[0].points;
                    Vector3 endPos = bezier.pointsList[3].points;
                    Vector3 dirFirstSegment = (endPos - startPos).normalized;

                    bezier.transform.position = startPos - dirFirstSegment * 5;

                    bezier.pointsList[1].points = startPos + dirFirstSegment * bezier.anchorDistWhenNewPointCreated;
                    bezier.pointsList[2].points = endPos - dirFirstSegment * bezier.anchorDistWhenNewPointCreated;

                    for (var i = 0; i < 4; i++)
                        bezier.pointsList[i].points -= bezier.transform.position;
                }

            }
            // New segment
            else
            {
                // Insert 3 new Points
                for (var i = 0; i < 3; i++)
                    m_pointsList.InsertArrayElementAtIndex(0);

                // use to find first point position
                Vector3 refPos = SListPoint(m_pointsList.arraySize - 1);
                // Choose a position

                float dist2 = Vector3.Distance(SListPoint(m_pointsList.arraySize - 1), SListPoint(m_pointsList.arraySize - 2));
                Vector3 dir2 = (SListPoint(m_pointsList.arraySize - 1) - SListPoint(m_pointsList.arraySize - 2)).normalized;

                Vector3 pointZero = refPos + dir2 * dist2;
                Vector3 pointTwo = refPos + dir * dist;

                Vector3 dir3 = (pointZero - pointTwo).normalized;

                Vector3 pointOne = refPos + dir * dist + dir3 * dist2;

                bezier.pointsList.Add(new PointDescription(pointZero - bezier.transform.position, Quaternion.identity));
                bezier.pointsList.Add(new PointDescription(pointOne - bezier.transform.position, Quaternion.identity));
                bezier.pointsList.Add(new PointDescription(pointTwo - bezier.transform.position, Quaternion.identity));
            }

            if (bezier.pointsList.Count == 4)
                UpdateRoad(HowManyCurvePoints() / 3 - 1, HowManyCurvePoints() / 3, true, false);
            else if (bezier.pointsList.Count > 4)
                UpdateRoad(HowManyCurvePoints() / 3 - 1, HowManyCurvePoints() / 3, false, false);

            PrefabUtility.RecordPrefabInstancePropertyModifications(bezier);
            #endregion
        }

        void DeletePoint(int value)
        {
            #region
            serializedObject.Update();
            int counter = m_pointsList.arraySize - 3;
            // Delete not allowed
            if (m_closestPoint.intValue == -1 || m_closestPoint.intValue == m_pointsList.arraySize - 1 && m_loop.boolValue)
            { }
            // Delete allowed
            else
            {
                if (m_pointsList.arraySize <= 4 && m_closestPoint.intValue == 0)
                {
                    for (var i = 0; i < 2; i++)
                        m_pointsList.DeleteArrayElementAtIndex(0);

                    m_distVecList.ClearArray();

                }
                else if (m_pointsList.arraySize <= 4 && m_closestPoint.intValue == 3)
                {
                    for (var i = 0; i < 2; i++)
                        m_pointsList.DeleteArrayElementAtIndex(m_pointsList.arraySize - 1);

                    m_distVecList.ClearArray();
                }
                else if (m_closestPoint.intValue == 0)
                    for (var i = 0; i < 3; i++)
                        m_pointsList.DeleteArrayElementAtIndex(0);
                else if (m_closestPoint.intValue == m_pointsList.arraySize - 1)
                    for (var i = 0; i < 3; i++)
                        m_pointsList.DeleteArrayElementAtIndex(m_pointsList.arraySize - 1);
                else
                    for (var i = 0; i < 3; i++)
                        m_pointsList.DeleteArrayElementAtIndex(m_closestPoint.intValue - 1);

                m_closestPoint.intValue = -1;
                m_closestPointList.ClearArray();

            }
            serializedObject.ApplyModifiedProperties();
            #endregion
        }

        void SplitSegment(int value)
        {
            #region
            if (m_closestPointList.arraySize < 2)
            {
                if (EditorUtility.DisplayDialog("Wrong Selection", "You must select 2 points on the road.", "Continue")) { }
            }
            else if (m_closestPointList.arraySize == 2)
            {
                if (m_closestPointList.GetArrayElementAtIndex(0).intValue / 3 == m_closestPointList.GetArrayElementAtIndex(1).intValue / 3 + 1
                    ||
                    m_closestPointList.GetArrayElementAtIndex(0).intValue / 3 == m_closestPointList.GetArrayElementAtIndex(1).intValue / 3 - 1)
                {
                    if (m_closestPointList.GetArrayElementAtIndex(0).intValue < m_closestPointList.GetArrayElementAtIndex(1).intValue)
                        value = m_closestPointList.GetArrayElementAtIndex(0).intValue;
                    else
                        value = m_closestPointList.GetArrayElementAtIndex(1).intValue;

                    serializedObject.Update();

                    float dist = Vector3.Distance(SListPoint(value + 1), SListPoint(value + 2));
                    Vector3 dir = (SListPoint(value + 1) - SListPoint(value + 2)).normalized;

                    m_pointsList.InsertArrayElementAtIndex(0);
                    m_pointsList.InsertArrayElementAtIndex(0);
                    m_pointsList.InsertArrayElementAtIndex(0);

                    m_pointsList.MoveArrayElement(0, value + 4);
                    m_pointsList.MoveArrayElement(0, value + 4);
                    m_pointsList.MoveArrayElement(0, value + 4);

                    Bezier bezier = (Bezier)target;

                    m_pointsList.GetArrayElementAtIndex(value + 2).FindPropertyRelative("points").vector3Value = SListPoint(value + 1) - dir * dist * .2f - bezier.transform.position;
                    m_pointsList.GetArrayElementAtIndex(value + 3).FindPropertyRelative("points").vector3Value = SListPoint(value + 1) - dir * dist * .5f - bezier.transform.position;
                    m_pointsList.GetArrayElementAtIndex(value + 4).FindPropertyRelative("points").vector3Value = SListPoint(value + 5) + dir * dist * .2f - bezier.transform.position;
                    m_closestPointList.ClearArray();
                    serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    if (EditorUtility.DisplayDialog("Wrong Selection", "You must choose two points next to each other.", "Continue")) { }
                }
            }
            if (m_closestPointList.arraySize > 2)
            {
                if (EditorUtility.DisplayDialog("Wrong Selection", "You must select only 2 points on the road.", "Continue")) { }
            }
            #endregion
        }

        int HowManyCurvePoints()
        {
            #region
            return m_pointsList.arraySize;
            #endregion
        }

        int Points(int value)
        {
            #region
            return (value + m_pointsList.arraySize) % m_pointsList.arraySize;
            #endregion
        }

        void CheckInput()
        {
            #region
            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.G)
                DeletePoint(FindClosestPoint());

            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.C)
                SplitSegment(FindClosestPoint());

            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.N)
                AddPoint();

            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.U)
            {
                UpdateRoad(0, HowManyCurvePoints() / 3, true, true);
                serializedObject.Update();
                m_IsChangeDone.boolValue = false;
                serializedObject.ApplyModifiedProperties();
            }

            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.M)
                MoveSceneViewCamera();

            // Show Gizmo 
            showGizmo = Event.current.shift ? false : true;

            // Show local Gizmo 
            isCtrlPressed = Event.current.control ? true : false;

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.S)
                isShowTangent = !isShowTangent;

            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.LeftShift)
                isShowTangent = false;

            if (EditorGUIUtility.hotControl != 0 && Event.current.type == EventType.MouseUp)
            {
                serializedObject.Update();
                int selectedPoint = FindClosestPoint();
                if (FindClosestPoint() % 3 == 0)
                {
                    if (isCtrlPressed)
                    {
                        SelectMultipleSpots(selectedPoint);
                    }
                    else
                    {
                        m_hotControlID.intValue = EditorGUIUtility.hotControl;
                        int lastPointSelected = m_closestPoint.intValue;

                        ResetMultipleSpots();

                        m_closestPoint.intValue = FindClosestPoint();

                        if (m_loop.boolValue && m_closestPoint.intValue == 0)
                        {
                            m_closestPointList.InsertArrayElementAtIndex(0);
                            m_closestPointList.GetArrayElementAtIndex(0).intValue = m_pointsList.arraySize - 1;
                        }
                    }
                }
                serializedObject.ApplyModifiedProperties();
            }
            #endregion
        }

        int FindClosestPoint()
        {
            #region
            int closestIndex = -1;
            float currentMinDist = 1000;

            Vector2 screenPos = SceneView.currentDrawingSceneView.camera.ScreenToViewportPoint(Event.current.mousePosition);

            for (var i = 0; i < m_pointsList.arraySize; i++)
            {
                if (i % 3 == 0)
                {
                    Vector3 pos = SceneView.currentDrawingSceneView.camera.WorldToViewportPoint(SListPoint(i));
                    pos = new Vector2(pos.x, 1 - pos.y);

                    float dist = Vector3.Distance(screenPos, pos);

                    if (dist < currentMinDist && dist < .02f)
                    {
                        currentMinDist = dist;
                        closestIndex = i;
                    }
                }
            }
            return closestIndex;
            #endregion
        }

        public Vector3 SListPoint(int index)
        {
            #region
            Bezier bezier = (Bezier)target;
            return m_pointsList.GetArrayElementAtIndex(Points(index)).FindPropertyRelative("points").vector3Value + bezier.transform.position;
            #endregion
        }

        void MoveSceneViewCamera()
        {
            #region
            Bezier bezier = (Bezier)target;
            int closestIndex = FindClosestPoint();
            if (closestIndex != -1)
            {
                Vector3 closestPos = m_pointsList.GetArrayElementAtIndex(closestIndex).FindPropertyRelative("points").vector3Value + bezier.transform.position;
                SceneView.currentDrawingSceneView.LookAt(closestPos);
            }
            #endregion
        }

        void HotControl()
        {
            #region
            if (m_pointsList.arraySize >= 4 && m_closestPoint.intValue < m_pointsList.arraySize && m_closestPoint.intValue != -1)
            {
                EditorGUILayout.LabelField("");
                EditorGUILayout.LabelField("Selected Points: " + m_closestPoint.intValue + " | Hot Control: " + m_hotControlID.intValue/*, GUILayout.Width(20), EditorStyles.boldLabel*/);
                EditorGUILayout.PropertyField(m_pointsList.GetArrayElementAtIndex(m_closestPoint.intValue).FindPropertyRelative("points"), new GUIContent(""));

                if (GUILayout.Button("Flatten tangent points"))
                {
                    if (m_closestPoint.intValue + 1 < m_pointsList.arraySize)
                    {
                        m_pointsList.GetArrayElementAtIndex(m_closestPoint.intValue + 1).FindPropertyRelative("points").vector3Value = new Vector3(
                           m_pointsList.GetArrayElementAtIndex(m_closestPoint.intValue + 1).FindPropertyRelative("points").vector3Value.x,
                           m_pointsList.GetArrayElementAtIndex(m_closestPoint.intValue).FindPropertyRelative("points").vector3Value.y,
                           m_pointsList.GetArrayElementAtIndex(m_closestPoint.intValue + 1).FindPropertyRelative("points").vector3Value.z);
                    }
                    if (m_closestPoint.intValue - 1 > 0)
                    {
                        m_pointsList.GetArrayElementAtIndex(m_closestPoint.intValue - 1).FindPropertyRelative("points").vector3Value = new Vector3(
                           m_pointsList.GetArrayElementAtIndex(m_closestPoint.intValue - 1).FindPropertyRelative("points").vector3Value.x,
                           m_pointsList.GetArrayElementAtIndex(m_closestPoint.intValue).FindPropertyRelative("points").vector3Value.y,
                           m_pointsList.GetArrayElementAtIndex(m_closestPoint.intValue + -1).FindPropertyRelative("points").vector3Value.z);
                    }
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("<"))
                    SetSegmentStraight(m_closestPoint.intValue, -1);

                EditorGUILayout.LabelField("Segment Straight", GUILayout.Width(100));
                if (GUILayout.Button(">"))
                    SetSegmentStraight(m_closestPoint.intValue, 1);

                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Set next segment straight"))
                    SetSegmentStraightUsingLastSegment(m_closestPoint.intValue, -1);

                GUILayout.Label("");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Force Selected Points:", GUILayout.Width(140));
                if (GUILayout.Button("X"))
                    ForceSelectedPointsPosition(m_closestPoint.intValue, "X_Axis");
                if (GUILayout.Button("Y"))
                    ForceSelectedPointsPosition(m_closestPoint.intValue, "Y_Axis");
                if (GUILayout.Button("Z"))
                    ForceSelectedPointsPosition(m_closestPoint.intValue, "Z_Axis");
                EditorGUILayout.EndHorizontal();

                GUILayout.Label("");
                if (GUILayout.Button("Delete selected point"))
                    DeletePoint(0);
            }
            else
            {
                EditorGUILayout.HelpBox("No Point is selected", MessageType.Info);
            }

            if ((m_pointsList.arraySize > 3))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Split Point:", GUILayout.Width(70));
                if (GUILayout.Button("First Point"))
                    SplitFirstPoint();
                if (GUILayout.Button("Last Point"))
                    SplitLastPoint();
                EditorGUILayout.EndHorizontal();
            }
            #endregion
        }

        void SelectionSection()
        {
            #region
            EditorGUILayout.LabelField("");

            DisplaySelectionBorderSize();

            EditorGUILayout.LabelField("");

            int minRef = 0;
            int maxref = m_distVecList.arraySize - 1;

            EditorGUI.BeginChangeCheck();

            if (!m_bSelection.boolValue)
            { }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Selection:", EditorStyles.boldLabel, GUILayout.Width(60));

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select All", GUILayout.MinWidth(100)))
                {
                    m_selectStart.intValue = minRef;
                    m_selectStop.intValue = maxref;
                }
                if (GUILayout.Button("No Selection", GUILayout.MinWidth(100)))
                {
                    m_selectStart.intValue = minRef;
                    m_selectStop.intValue = minRef;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Min: ", GUILayout.Width(50));
                m_selectStart.intValue = EditorGUILayout.IntSlider(m_selectStart.intValue, minRef, maxref);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Max: ", GUILayout.Width(50));
                m_selectStop.intValue = EditorGUILayout.IntSlider(m_selectStop.intValue, minRef, maxref);
                EditorGUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    if (m_selectStart.intValue > m_selectStop.intValue)
                        m_selectStart.intValue = m_selectStop.intValue;
                    if (m_selectStop.intValue < m_selectStart.intValue)
                        m_selectStop.intValue = m_selectStart.intValue;
                }

                EditorGUILayout.LabelField("");

                if (m_selectStop.intValue == 0)
                    EditorGUILayout.HelpBox("Selection is empty", MessageType.Warning);
                if (m_selectStop.intValue != 0)
                    if (GUILayout.Button("Update Terrain Height", GUILayout.MinWidth(100), GUILayout.Height(40)))
                        UpdateTerrainHeight(true);
            }
            #endregion
        }

        void AddCrossRoadSection()
        {
            #region
            EditorGUILayout.LabelField("");

            m_indexCrossRoadTab.intValue = GUILayout.SelectionGrid(m_indexCrossRoadTab.intValue, crossRoadTab, 3);

            if (m_indexCrossRoadTab.intValue == 0)
                SectionCreateCrossRoad();
            else if (m_indexCrossRoadTab.intValue == 1)
                SectionConnectRoadToCrossRoad();
            else if (m_indexCrossRoadTab.intValue == 2)
                InstantiateCrossRoadAfterSplitingRoad();
            #endregion
        }

        void UpdateRoadPositionToCrossRoadPosition(string roadConnectionPoint, int crossRoadConnectionPoint, RoadCross crossRoad)
        {
            #region
            Bezier bezier = (Bezier)target;
            if (roadConnectionPoint == "FirstPoint")
            {
                // Point
                bezier.pointsList[0].points = crossRoad.anchorList[crossRoadConnectionPoint].position - bezier.transform.position;
                // Tangent
                bezier.pointsList[1].points = crossRoad.anchorList[crossRoadConnectionPoint].position - bezier.transform.position
                    + crossRoad.anchorList[crossRoadConnectionPoint].forward * crossRoad.roadList[crossRoadConnectionPoint].road.lastAnchorDist;
            }

            if (roadConnectionPoint == "LastPoint")
            {
                int howMany = m_pointsList.arraySize;
                // Point
                bezier.pointsList[howMany - 1].points = crossRoad.anchorList[crossRoadConnectionPoint].position - bezier.transform.position;

                // Tangent
                bezier.pointsList[howMany - 2].points = crossRoad.anchorList[crossRoadConnectionPoint].position - bezier.transform.position
                    + crossRoad.anchorList[crossRoadConnectionPoint].forward * crossRoad.roadList[crossRoadConnectionPoint].road.lastAnchorDist;
            }
            UpdateRoad(0, HowManyCurvePoints() / 3, true, true);
            #endregion
        }

        void UpdateRoad(int firstPointUpdate, int lastPointUpdate, bool updateBorder, bool isRoadMeshRemoved, Bezier specificBezier = null, GameObject crossRoad = null)
        {
            #region
            isProcessDone = false;
            Bezier myScript = (Bezier)target;
            if (specificBezier)
                myScript = specificBezier;

            RoadMeshGen.ReturnTotalCurveDistance(myScript);
            while (!RoadMeshGen.isProcessDone) { }

            if (myScript.isRoadMeshUpdated)
            {
                if (isRoadMeshRemoved)
                {
                    myScript.GetComponent<MeshFilter>().sharedMesh = null;
                    myScript.GetComponent<MeshCollider>().sharedMesh = null;
                }

                int howManyPoints = 0;
                if (specificBezier)
                {
                    if (specificBezier.totalDistance == 0)
                        specificBezier.totalDistance = 40;
                    howManyPoints = specificBezier.distVecList.Count;
                }
                else
                {
                    serializedObject.Update();

                    if (m_totalDistance.floatValue == 0)
                        m_totalDistance.floatValue = 40;

                    serializedObject.ApplyModifiedProperties();

                    howManyPoints = m_distVecList.arraySize;
                }

                RoadMeshGen.RoadMesh(
                    firstPointUpdate,
                    lastPointUpdate,
                    myScript,
                    howManyPoints,
                    RoadMeshGen.RoadStyle.Wire,
                    RoadMeshGen.PlaneFace.Front,
                    true,
                    myScript.roadSubdivisionWhenGenerated);

                while (!RoadMeshGen.isProcessDone) { }
            }

            isProcessDone = true;
            if (crossRoad) Selection.activeGameObject = crossRoad;
            #endregion
        }

        void UpdateTerrainHeight(bool UpdateSelection = false)
        {
            #region
            Bezier bezier = (Bezier)target;

            UpdateRoad(0, m_distVecList.arraySize / 3, false, true);

            TerrainModif terrainModif = bezier.GetComponent<TerrainModif>();

            TerrainUpdater.UpdateTerrainList(bezier, bezier.distVecList.Count);
            while (!TerrainUpdater.isProcessDone) { }

            TerrainUpdater.UpdateColliderUsedForSelection(UpdateSelection, bezier);
            while (!TerrainUpdater.isProcessDone) { }

            TerrainUpdater.UpdateHeight(terrainModif, UpdateSelection);
            while (!TerrainUpdater.isProcessDone) { }

            TerrainUpdater.UpdateTerrainProcessDoneFeedback();
            #endregion
        }

        void Other()
        {
            #region
            EditorGUILayout.LabelField("Textures: ", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tile Size: ", GUILayout.Width(150));
            EditorGUILayout.PropertyField(m_tileSize, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Road Size: ", GUILayout.Width(150));
            EditorGUILayout.PropertyField(m_roadSize, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("");

            EditorGUILayout.LabelField("Tangentes: ", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Cross Road Tangent Size: ", GUILayout.Width(150));
            EditorGUILayout.PropertyField(m_lastAnchorDist, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Default Tangent Size: ", GUILayout.Width(150));
            EditorGUILayout.PropertyField(m_anchorDistWhenNewPointCreated, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Refresh"))
            {
                Bezier bezier = (Bezier)target;

                Undo.RegisterFullObjectHierarchyUndo(bezier.gameObject, bezier.name);
                int howMany = bezier.pointsList.Count;
                // Tangent
                Vector3 dir = (bezier.pointsList[howMany - 2].points - bezier.pointsList[howMany - 1].points).normalized;

                bezier.pointsList[howMany - 2].points = bezier.pointsList[howMany - 1].points
                  + dir * bezier.lastAnchorDist;

                // Tangent
                dir = (bezier.pointsList[1].points - bezier.pointsList[0].points).normalized;

                bezier.pointsList[1].points = bezier.pointsList[0].points
                  + dir * bezier.lastAnchorDist;
                SceneView.RepaintAll();

                UpdateRoad(0, HowManyCurvePoints() / 3, true, true);
            }
            EditorGUILayout.LabelField("");

            EditorGUILayout.LabelField("Road Mesh: ", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Update Road Mesh: ", GUILayout.Width(150));
            EditorGUILayout.PropertyField(m_isRoadMeshUpdated, new GUIContent(""));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Loop Road: ", GUILayout.Width(150));
            string sLoop = "Is Looped";
            if (!m_loop.boolValue) sLoop = "Is Not Loop";
            if (GUILayout.Button(sLoop, GUILayout.MinWidth(50)))
            {
                m_loop.boolValue = !m_loop.boolValue;
                if (m_loop.boolValue)
                {
                    Bezier bezier = (Bezier)target;
                    m_pointsList.GetArrayElementAtIndex(m_pointsList.arraySize - 1).FindPropertyRelative("points").vector3Value = SListPoint(0) - bezier.transform.position;

                    float dist = Vector3.Distance(SListPoint(0), SListPoint(1));
                    Vector3 dir = (SListPoint(1) - SListPoint(0)).normalized;

                    m_pointsList.GetArrayElementAtIndex(m_pointsList.arraySize - 2).FindPropertyRelative("points").vector3Value = SListPoint(0) - dir * dist - bezier.transform.position;
                    serializedObject.ApplyModifiedProperties();
                    UpdateRoad(0, HowManyCurvePoints() / 3, true, true);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("");

            if (m_pointsList.arraySize >= 4)
            {
                if (GUILayout.Button("ResetBezierCurvePivot"))
                {
                    ResetBezierCurvePivot();
                }
            }
            #endregion
        }

        void InfoHelper()
        {
            #region
            EditorGUILayout.HelpBox("Don't forget to enable Gizmo in scene view", MessageType.Warning);

            EditorGUILayout.HelpBox("Shortcuts: " +
                "\n" +
                "\n" + "Add Point: N | Update Road: U" +
                "\n" +
                "\n" + "Display Transform Gizmo: Left Shift (Spot or Tangent) | Press Left Shift + S to switch between spot and tangent." +
                "\n" +
                "\n" + "Focus: M | Split: C | Delete Point: G" +
                "\n" +
                "\n" + "Get Hot Control in the Inspector: Left Click on spot"
                , MessageType.None);

            #endregion
        }

        void SetSegmentStraight(int spotID, int dir)
        {
            #region
            if (dir == -1 && spotID > 0)
            {
                Vector3 startPos = m_pointsList.GetArrayElementAtIndex(spotID).FindPropertyRelative("points").vector3Value;
                Vector3 endPos = m_pointsList.GetArrayElementAtIndex(spotID - 3).FindPropertyRelative("points").vector3Value;
                Vector3 dirFirstSegment = (endPos - startPos).normalized;

                if (spotID < m_pointsList.arraySize - 1)
                    m_pointsList.GetArrayElementAtIndex(spotID + 1).FindPropertyRelative("points").vector3Value = startPos - dirFirstSegment * 10;

                m_pointsList.GetArrayElementAtIndex(spotID - 1).FindPropertyRelative("points").vector3Value = startPos + dirFirstSegment * 10;
                m_pointsList.GetArrayElementAtIndex(spotID - 2).FindPropertyRelative("points").vector3Value = endPos - dirFirstSegment * 10;

                if (spotID > 3)
                    m_pointsList.GetArrayElementAtIndex(spotID - 4).FindPropertyRelative("points").vector3Value = endPos + dirFirstSegment * 10;
            }

            if (dir == 1 && spotID < m_pointsList.arraySize - 1)
            {
                Vector3 startPos = m_pointsList.GetArrayElementAtIndex(spotID).FindPropertyRelative("points").vector3Value;
                Vector3 endPos = m_pointsList.GetArrayElementAtIndex(spotID + 3).FindPropertyRelative("points").vector3Value;
                Vector3 dirFirstSegment = (endPos - startPos).normalized;

                if (spotID - 1 > 0)
                    m_pointsList.GetArrayElementAtIndex(spotID - 1).FindPropertyRelative("points").vector3Value = startPos - dirFirstSegment * 10;

                m_pointsList.GetArrayElementAtIndex(spotID + 1).FindPropertyRelative("points").vector3Value = startPos + dirFirstSegment * 10;
                m_pointsList.GetArrayElementAtIndex(spotID + 2).FindPropertyRelative("points").vector3Value = endPos - dirFirstSegment * 10;

                if (spotID + 4 < m_pointsList.arraySize)
                    m_pointsList.GetArrayElementAtIndex(spotID + 4).FindPropertyRelative("points").vector3Value = endPos + dirFirstSegment * 10;
            }
            #endregion
        }

        void SetSegmentStraightUsingLastSegment(int spotID, int dir)
        {
            #region
            if (spotID < m_pointsList.arraySize - 1)
            {
                Vector3 startPos = m_pointsList.GetArrayElementAtIndex(spotID).FindPropertyRelative("points").vector3Value;
                Vector3 endPos = m_pointsList.GetArrayElementAtIndex(spotID + 3).FindPropertyRelative("points").vector3Value;
                Vector3 previousPos = m_pointsList.GetArrayElementAtIndex(spotID - 3).FindPropertyRelative("points").vector3Value;

                float distFromStartToEnd = Vector3.Distance(endPos, startPos);

                Vector3 dirFirstSegment = (startPos - previousPos).normalized;

                if (spotID - 1 > 0)
                    m_pointsList.GetArrayElementAtIndex(spotID - 1).FindPropertyRelative("points").vector3Value = startPos - dirFirstSegment * 10;

                m_pointsList.GetArrayElementAtIndex(spotID + 2).FindPropertyRelative("points").vector3Value = startPos + dirFirstSegment * distFromStartToEnd - 10 * dirFirstSegment;
                m_pointsList.GetArrayElementAtIndex(spotID + 3).FindPropertyRelative("points").vector3Value = startPos + dirFirstSegment * distFromStartToEnd;

                m_pointsList.GetArrayElementAtIndex(spotID + 1).FindPropertyRelative("points").vector3Value = startPos + dirFirstSegment * 10;

                if (spotID + 4 < m_pointsList.arraySize)
                    m_pointsList.GetArrayElementAtIndex(spotID + 4).FindPropertyRelative("points").vector3Value = startPos + dirFirstSegment * distFromStartToEnd + 10 * dirFirstSegment;
            }
            #endregion
        }

        void SelectMultipleSpots(int newSpot)
        {
            #region
            bool alreadyInTheList = false;
            int alreadyInTheListID = -1;
            if (m_closestPointList.arraySize == 0)
            {
                m_closestPointList.InsertArrayElementAtIndex(0);
                m_closestPointList.GetArrayElementAtIndex(0).intValue = m_closestPoint.intValue;

                /* if (m_loop.boolValue)
                 {
                     //m_closestPointList.InsertArrayElementAtIndex(0);
                     m_closestPointList.GetArrayElementAtIndex(0).intValue = m_pointsList.arraySize-1;
                 }*/
            }

            for (var i = 0; i < m_closestPointList.arraySize; i++)
            {
                if (m_closestPointList.GetArrayElementAtIndex(i).intValue == newSpot)
                {
                    alreadyInTheList = true;
                    alreadyInTheListID = i;
                    break;
                }
            }

            if (!alreadyInTheList)
            {
                m_closestPointList.InsertArrayElementAtIndex(0);
                m_closestPointList.GetArrayElementAtIndex(0).intValue = newSpot;
            }
            else
            {
                if (m_closestPoint.intValue != newSpot)
                    m_closestPointList.DeleteArrayElementAtIndex(alreadyInTheListID);
            }
            #endregion
        }

        void ResetMultipleSpots()
        {
            #region
            m_closestPointList.ClearArray();
            #endregion
        }

        bool IsSpotInTheSelectionSpotsList(int newSpot)
        {
            #region
            for (var i = 0; i < m_closestPointList.arraySize; i++)
            {
                if (m_closestPointList.GetArrayElementAtIndex(i).intValue == newSpot)
                {
                    return true;
                }
            }
            return false;
            #endregion
        }

        void MoveSelectedPoints(int spot, Vector3 lastPos, Vector3 newPos)
        {
            #region
            serializedObject.Update();
            Vector3 dir = (newPos - lastPos).normalized;
            float dist = Vector3.Distance(newPos, lastPos);

            for (var i = 0; i < m_closestPointList.arraySize; i++)
            {
                if (spot != m_closestPointList.GetArrayElementAtIndex(i).intValue)
                {
                    int spotID = m_closestPointList.GetArrayElementAtIndex(i).intValue;

                    if (spotID - 1 > 0)
                        m_pointsList.GetArrayElementAtIndex(spotID - 1).FindPropertyRelative("points").vector3Value += dir * dist;

                    m_pointsList.GetArrayElementAtIndex(spotID).FindPropertyRelative("points").vector3Value += dir * dist;

                    if (spotID + 1 < m_pointsList.arraySize - 1)
                        m_pointsList.GetArrayElementAtIndex(spotID + 1).FindPropertyRelative("points").vector3Value += dir * dist;

                }
            }
            serializedObject.ApplyModifiedProperties();
            #endregion
        }

        void ForceSelectedPointsPosition(int spot, string axis)
        {
            #region
            serializedObject.Update();

            Vector3 spotPos = m_pointsList.GetArrayElementAtIndex(spot).FindPropertyRelative("points").vector3Value;

            for (var i = 0; i < m_closestPointList.arraySize; i++)
            {
                if (spot != m_closestPointList.GetArrayElementAtIndex(i).intValue)
                {
                    int spotID = m_closestPointList.GetArrayElementAtIndex(i).intValue;

                    if (axis == "X_Axis")
                    {
                        if (spotID - 1 > 0)
                            m_pointsList.GetArrayElementAtIndex(spotID - 1).FindPropertyRelative("points").vector3Value = new Vector3(
                                spotPos.x,
                                m_pointsList.GetArrayElementAtIndex(spotID - 1).FindPropertyRelative("points").vector3Value.y,
                                m_pointsList.GetArrayElementAtIndex(spotID - 1).FindPropertyRelative("points").vector3Value.z);

                        m_pointsList.GetArrayElementAtIndex(spotID).FindPropertyRelative("points").vector3Value = new Vector3(
                                spotPos.x,
                                m_pointsList.GetArrayElementAtIndex(spotID).FindPropertyRelative("points").vector3Value.y,

                                m_pointsList.GetArrayElementAtIndex(spotID).FindPropertyRelative("points").vector3Value.z);

                        if (spotID + 1 < m_pointsList.arraySize - 1)
                            m_pointsList.GetArrayElementAtIndex(spotID + 1).FindPropertyRelative("points").vector3Value = new Vector3(
                                spotPos.x,
                                m_pointsList.GetArrayElementAtIndex(spotID + 1).FindPropertyRelative("points").vector3Value.y,
                                m_pointsList.GetArrayElementAtIndex(spotID + 1).FindPropertyRelative("points").vector3Value.z);
                    }
                    if (axis == "Y_Axis")
                    {
                        if (spotID - 1 > 0)
                            m_pointsList.GetArrayElementAtIndex(spotID - 1).FindPropertyRelative("points").vector3Value = new Vector3(
                                m_pointsList.GetArrayElementAtIndex(spotID - 1).FindPropertyRelative("points").vector3Value.x,
                                spotPos.y,
                                m_pointsList.GetArrayElementAtIndex(spotID - 1).FindPropertyRelative("points").vector3Value.z);

                        m_pointsList.GetArrayElementAtIndex(spotID).FindPropertyRelative("points").vector3Value = new Vector3(
                                m_pointsList.GetArrayElementAtIndex(spotID).FindPropertyRelative("points").vector3Value.x,
                                spotPos.y,
                                m_pointsList.GetArrayElementAtIndex(spotID).FindPropertyRelative("points").vector3Value.z);

                        if (spotID + 1 < m_pointsList.arraySize - 1)
                            m_pointsList.GetArrayElementAtIndex(spotID + 1).FindPropertyRelative("points").vector3Value = new Vector3(
                                m_pointsList.GetArrayElementAtIndex(spotID + 1).FindPropertyRelative("points").vector3Value.x,
                                spotPos.y,
                                m_pointsList.GetArrayElementAtIndex(spotID + 1).FindPropertyRelative("points").vector3Value.z);
                    }
                    if (axis == "Z_Axis")
                    {
                        if (spotID - 1 > 0)
                            m_pointsList.GetArrayElementAtIndex(spotID - 1).FindPropertyRelative("points").vector3Value = new Vector3(
                                m_pointsList.GetArrayElementAtIndex(spotID - 1).FindPropertyRelative("points").vector3Value.x,
                                m_pointsList.GetArrayElementAtIndex(spotID - 1).FindPropertyRelative("points").vector3Value.y,
                                 spotPos.z);

                        m_pointsList.GetArrayElementAtIndex(spotID).FindPropertyRelative("points").vector3Value = new Vector3(
                                m_pointsList.GetArrayElementAtIndex(spotID).FindPropertyRelative("points").vector3Value.x,
                                m_pointsList.GetArrayElementAtIndex(spotID).FindPropertyRelative("points").vector3Value.y,
                                 spotPos.z);

                        if (spotID + 1 < m_pointsList.arraySize - 1)
                            m_pointsList.GetArrayElementAtIndex(spotID + 1).FindPropertyRelative("points").vector3Value = new Vector3(
                                m_pointsList.GetArrayElementAtIndex(spotID + 1).FindPropertyRelative("points").vector3Value.x,
                                m_pointsList.GetArrayElementAtIndex(spotID + 1).FindPropertyRelative("points").vector3Value.y,
                                 spotPos.z);
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
            #endregion
        }

        void SectionCreateCrossRoad()
        {
            #region
            EditorGUILayout.LabelField("Create Cross Road:", EditorStyles.boldLabel, GUILayout.Width(120));
            Bezier myScript = (Bezier)target;
            if (m_indexCreatorCrossRoad.intValue == 0 && m_CrossRoadpoint.objectReferenceValue)
                EditorGUILayout.HelpBox("The road is already connected to a cross Road (White Spot)", MessageType.Warning);
            if (m_indexCreatorCrossRoad.intValue == 1 && m_CrossRoadpointIn.objectReferenceValue)
                EditorGUILayout.HelpBox("The road is already connected to a cross Road (Black Spot)", MessageType.Warning);

            EditorGUILayout.HelpBox("Note: To visualize the connection in Scene view, there are 3 circles next to black or white spot.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            Color oldColor = GUI.backgroundColor;
            if (m_indexCreatorCrossRoad.intValue == 0)
                GUI.backgroundColor = Color.white;
            if (m_indexCreatorCrossRoad.intValue == 1)
                GUI.backgroundColor = Color.black;
            if (GUILayout.Button("", GUILayout.Width(20)))
            { }
            GUI.backgroundColor = oldColor;

            m_indexCreatorCrossRoad.intValue = EditorGUILayout.Popup(m_indexCreatorCrossRoad.intValue, anchorBlackAndWhite, GUILayout.MinWidth(40));

            List<string> groundPrefabList = new List<string>();
            for (var i = 0; i < myScript.roadData.crossRoadGroundPrefabList.Count; i++)
                groundPrefabList.Add(myScript.roadData.crossRoadGroundPrefabList[i].name);

            m_indexCrossRoadGroundPrefab.intValue = EditorGUILayout.Popup(m_indexCrossRoadGroundPrefab.intValue, groundPrefabList.ToArray(), GUILayout.MinWidth(40));

            if (m_indexCreatorCrossRoad.intValue == 0)
            {
                if (!m_CrossRoadpoint.objectReferenceValue)
                {
                    if (GUILayout.Button("Create", GUILayout.MinWidth(50)))
                        InstantiateCrossRoad(1, 0, 1, 1, m_CrossRoadpoint, RoadCross.Direction.Start);
                }
                else
                {
                    if (GUILayout.Button("Remove Connection", GUILayout.MinWidth(120)))
                        RemoveCrossRoadConnection(m_CrossRoadpoint);
                }
            }
            if (m_indexCreatorCrossRoad.intValue == 1)
            {

                if (!m_CrossRoadpointIn.objectReferenceValue)
                {
                    if (GUILayout.Button("Create", GUILayout.MinWidth(50)))
                        InstantiateCrossRoad(myScript.pointsList.Count - 1, myScript.pointsList.Count - 2, 3, 1, m_CrossRoadpointIn, RoadCross.Direction.End);
                }
                else
                {
                    if (GUILayout.Button("Remove Connection", GUILayout.MinWidth(120)))
                        RemoveCrossRoadConnection(m_CrossRoadpointIn);
                }
            }
            EditorGUILayout.EndHorizontal();
            #endregion
        }

        void DisplayButtonWithSpecificColor(Color nColor)
        {
            #region
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = nColor;
            if (GUILayout.Button("", GUILayout.Width(20)))
            { }
            GUI.backgroundColor = oldColor;
            #endregion
        }

        void ConnectRoadToCrossRoad(string roadConnectionPoint, int crossRoadConnectionPoint, RoadCross crossRoad, Bezier specificBezier = null)
        {
            #region
            isProcessDone = false;
            // Move road to cross road position
            Bezier bezier = (Bezier)target;
            if (specificBezier) bezier = specificBezier;
            Undo.RegisterFullObjectHierarchyUndo(bezier, bezier.name);
            Undo.RegisterFullObjectHierarchyUndo(crossRoad, crossRoad.name);

            SerializedObject serializedObject2 = new UnityEditor.SerializedObject(crossRoad);
            SerializedProperty m_RoadList = serializedObject2.FindProperty("roadList");
            serializedObject2.Update();

            SerializedObject serializedObject3 = new UnityEditor.SerializedObject(bezier);
            SerializedProperty m_CrossRoadpointBezier = serializedObject3.FindProperty("crossRoadpoint");
            SerializedProperty m_CrossDirectionBezier = serializedObject3.FindProperty("crossDirection");
            SerializedProperty m_CrossRoadpointInBezier = serializedObject3.FindProperty("crossRoadpointIn");
            SerializedProperty m_CrossDirectionInBezier = serializedObject3.FindProperty("crossDirectionIn");
            serializedObject3.Update();

            if (roadConnectionPoint == "FirstPoint")
            {
                m_RoadList.GetArrayElementAtIndex(crossRoadConnectionPoint).FindPropertyRelative("roadStartPoint").enumValueIndex = 0;
                m_CrossRoadpointBezier.objectReferenceValue = crossRoad.transform;
                m_CrossDirectionBezier.enumValueIndex = crossRoadConnectionPoint;
            }

            if (roadConnectionPoint == "LastPoint")
            {
                m_RoadList.GetArrayElementAtIndex(crossRoadConnectionPoint).FindPropertyRelative("roadStartPoint").enumValueIndex = 1;
                m_CrossRoadpointInBezier.objectReferenceValue = crossRoad.transform;
                m_CrossDirectionInBezier.enumValueIndex = crossRoadConnectionPoint;
            }

            m_RoadList.GetArrayElementAtIndex(crossRoadConnectionPoint).FindPropertyRelative("road").objectReferenceValue = (Bezier)bezier;

            serializedObject3.ApplyModifiedProperties();
            serializedObject2.ApplyModifiedProperties();

            serializedObject.ApplyModifiedProperties();

            while (m_CrossRoadpointInBezier.objectReferenceValue != bezier.crossRoadpointIn) { }
            while (m_CrossRoadpointBezier.objectReferenceValue != bezier.crossRoadpoint) { }

            if (!specificBezier)
                UpdateRoadPositionToCrossRoadPosition(roadConnectionPoint, crossRoadConnectionPoint, crossRoad);

            isProcessDone = true;
            #endregion
        }

        void SectionConnectRoadToCrossRoad()
        {
            #region
            EditorGUILayout.LabelField("Connect to Cross Road:", EditorStyles.boldLabel, GUILayout.Width(150));
            Bezier myScript = (Bezier)target;
            if (m_indexCreatorCrossRoad.intValue == 0 && m_CrossRoadpoint.objectReferenceValue)
                EditorGUILayout.HelpBox("The road is already connected to a cross Road (White Spot)", MessageType.Warning);
            if (m_indexCreatorCrossRoad.intValue == 1 && m_CrossRoadpointIn.objectReferenceValue)
                EditorGUILayout.HelpBox("The road is already connected to a cross Road (Black Spot)", MessageType.Warning);

            EditorGUILayout.HelpBox("Note: To visualize the connection in Scene view, there are 3 circles next to black or white spot.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (m_indexCreatorCrossRoad.intValue == 0)
                DisplayButtonWithSpecificColor(Color.white);
            if (m_indexCreatorCrossRoad.intValue == 1)
                DisplayButtonWithSpecificColor(Color.black);

            m_indexCreatorCrossRoad.intValue = EditorGUILayout.Popup(m_indexCreatorCrossRoad.intValue, anchorBlackAndWhite, GUILayout.MinWidth(40));

            if (m_indexCreatorCrossRoad.intValue == 0 && m_CrossRoadpoint.objectReferenceValue)
                if (GUILayout.Button("Remove Connection", GUILayout.MinWidth(120)))
                    RemoveCrossRoadConnection(m_CrossRoadpoint);

            if (m_indexCreatorCrossRoad.intValue == 1 && m_CrossRoadpointIn.objectReferenceValue)
                if (GUILayout.Button("Remove Connection", GUILayout.MinWidth(120)))
                    RemoveCrossRoadConnection(m_CrossRoadpointIn);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("");

            if (crossRoadIDList.Count == 0)
            {
                if (GUILayout.Button("Cross Road List", GUILayout.MinWidth(40)))
                {
                    RoadCross[] roadCross = FindObjectsOfType(typeof(RoadCross)) as RoadCross[];
                    foreach (RoadCross obj in roadCross)
                    {
                        crossRoadIDList.Add(obj.roadID);
                        crossRoadNameList.Add(obj.name);
                        crossRoadObjList.Add(obj);
                    }

                    EditorWindow view = EditorWindow.GetWindow<SceneView>();
                    view.Repaint();

                }
            }
            else
            {
                if (crossRoadObjList[index])
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Select Cross Road:", GUILayout.Width(110));
                    EditorGUI.BeginChangeCheck();

                    index = EditorGUILayout.Popup(index, crossRoadNameList.ToArray(), GUILayout.MinWidth(40));

                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorWindow view = EditorWindow.GetWindow<SceneView>();
                        view.Repaint();
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Select Connection:", GUILayout.Width(110));
                    if (m_indexConnectionCrossRoadTab.intValue == 0) DisplayButtonWithSpecificColor(Color.blue);
                    else if (m_indexConnectionCrossRoadTab.intValue == 1) DisplayButtonWithSpecificColor(Color.yellow);
                    else if (m_indexConnectionCrossRoadTab.intValue == 2) DisplayButtonWithSpecificColor(Color.green);
                    else if (m_indexConnectionCrossRoadTab.intValue == 3) DisplayButtonWithSpecificColor(Color.magenta);

                    m_indexConnectionCrossRoadTab.intValue = EditorGUILayout.Popup(m_indexConnectionCrossRoadTab.intValue, connectCrossRoadTab, GUILayout.MinWidth(40));
                    EditorGUILayout.EndHorizontal();

                    SerializedObject serializedObject2 = new UnityEditor.SerializedObject(crossRoadObjList[index]);
                    SerializedProperty m_RoadList = serializedObject2.FindProperty("roadList");

                    if (m_RoadList.GetArrayElementAtIndex(m_indexConnectionCrossRoadTab.intValue).FindPropertyRelative("road").objectReferenceValue == null)
                    {
                        if (GUILayout.Button("Connect", GUILayout.MinWidth(50)))
                        {
                            if (m_indexCreatorCrossRoad.intValue == 0 && !m_CrossRoadpoint.objectReferenceValue)
                                ConnectRoadToCrossRoad("FirstPoint", m_indexConnectionCrossRoadTab.intValue, crossRoadObjList[index]);
                            if (m_indexCreatorCrossRoad.intValue == 1 && !m_CrossRoadpointIn.objectReferenceValue)
                                ConnectRoadToCrossRoad("LastPoint", m_indexConnectionCrossRoadTab.intValue, crossRoadObjList[index]);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(connectCrossRoadTab[m_indexConnectionCrossRoadTab.intValue] + " connection is already connected to an other road." +
                            "\n" + "To use this connection you must remove it in the Cross Road itself." +
                            "\n" + "-Select the Road" +
                            "\n" + "-Remove the connection in the Inspector", MessageType.Warning);
                        if (GUILayout.Button("Select Cross Road in the Hierarchy", GUILayout.MinWidth(50)))
                            Selection.activeGameObject = crossRoadObjList[index].roadList[m_indexConnectionCrossRoadTab.intValue].road.gameObject;
                    }
                }
                else
                {
                    crossRoadIDList.Clear();
                }
            }
            #endregion
        }

        void InstantiateCrossRoad(int firstPosIndex, int secondPosIndex, int anchorindex, float invert, SerializedProperty m_ConnecTo, RoadCross.Direction roadDir)
        {
            #region
            Bezier myScript = (Bezier)target;
            GameObject crossRoadObj = (GameObject)PrefabUtility.InstantiatePrefab(myScript.roadData.crossRoadprefab as GameObject);

            crossRoadObj.name = "Cross_" + myScript.roadData.iD;

            Undo.RegisterCreatedObjectUndo(crossRoadObj, "crossRoadObj");

            RoadCross roadCross = crossRoadObj.GetComponent<RoadCross>();
            roadCross.transform.SetAsLastSibling();

            // Load Ground
            roadCross.grpDecal.transform.GetChild(0).gameObject.SetActive(false);
            GameObject crossRoadGround = (GameObject)PrefabUtility.InstantiatePrefab(myScript.roadData.crossRoadGroundPrefabList[m_indexCrossRoadGroundPrefab.intValue] as GameObject, roadCross.grpDecal.transform.GetChild(1));
            Undo.RegisterCreatedObjectUndo(crossRoadGround, "crossRoadGround");
            // Setup ground and anchors
            CrossRoadPreset crossRoadPreset = roadCross.grpDecal.transform.GetChild(1).GetChild(0).GetComponent<CrossRoadPreset>();
            for (var i = 0; i < roadCross.anchorList.Count; i++)
                roadCross.anchorList[i].localPosition = crossRoadPreset.anchorPosList[i];

            roadCross.objCollider.transform.localScale = crossRoadPreset.colliderTransformScale;

            roadCross.currentroadCrossGroundPreset = crossRoadPreset.roadTypeCreatedByDefault;
            roadCross.anchorDistWhenNewPointCreated = crossRoadPreset.anchorDistWhenNewPointCreated;

            roadCross.roadID = myScript.roadData.iD;

            SerializedObject serializedObject2 = new UnityEditor.SerializedObject(myScript.roadData);
            SerializedProperty m_iD = serializedObject2.FindProperty("iD");
            serializedObject2.Update();
            m_iD.intValue++;
            serializedObject2.ApplyModifiedProperties();

            Vector3 dir = (myScript.pointsList[firstPosIndex].points - myScript.pointsList[secondPosIndex].points).normalized;

            float dis = Vector3.Distance(roadCross.anchorList[anchorindex].position, crossRoadObj.transform.position);
            float dis2 = Vector3.Distance(myScript.pointsList[firstPosIndex].points, myScript.pointsList[secondPosIndex].points);

            if (m_CrossRoadpoint == m_ConnecTo) crossRoadObj.transform.position = myScript.pointsList[firstPosIndex].points - dir * dis2 - dir * dis + myScript.transform.position;
            if (m_CrossRoadpointIn == m_ConnecTo) crossRoadObj.transform.position = myScript.pointsList[firstPosIndex].points + dir * dis + myScript.transform.position;

            crossRoadObj.transform.LookAt(myScript.pointsList[firstPosIndex].points + dir * dis * 1.2f + myScript.transform.position);

            // Connect the road
            roadCross.roadList[anchorindex].road = myScript;
            roadCross.roadList[anchorindex].roadStartPoint = roadDir;

            m_ConnecTo.objectReferenceValue = roadCross.transform;

            if (m_CrossRoadpoint == m_ConnecTo) m_CrossDirection.enumValueIndex = anchorindex;
            if (m_CrossRoadpointIn == m_ConnecTo) m_CrossDirectionIn.enumValueIndex = anchorindex;

            serializedObject.ApplyModifiedProperties();
            UpdateRoad(0, HowManyCurvePoints() / 3, true, true, null, crossRoadObj);
            #endregion
        }

        void RemoveCrossRoadConnection(SerializedProperty m_DisconnectTo)
        {
            #region
            Bezier bezier = (Bezier)target;
            Transform crossRoad = (Transform)m_DisconnectTo.objectReferenceValue;
            SerializedObject serializedObject2 = new UnityEditor.SerializedObject(crossRoad.GetComponent<RoadCross>());
            SerializedProperty m_RoadList = serializedObject2.FindProperty("roadList");
            serializedObject2.Update();

            for (var i = 0; i < m_RoadList.arraySize; i++)
            {
                if ((Bezier)m_RoadList.GetArrayElementAtIndex(i).FindPropertyRelative("road").objectReferenceValue == bezier)
                {
                    m_RoadList.GetArrayElementAtIndex(i).FindPropertyRelative("road").objectReferenceValue = null;
                    break;
                }
            }

            serializedObject2.ApplyModifiedProperties();

            m_DisconnectTo.objectReferenceValue = null;
            #endregion
        }

        void DisplaySelectionBorderSize()
        {
            #region
            EditorGUILayout.LabelField("Border Size: ", EditorStyles.boldLabel, GUILayout.Width(100));

            SerializedProperty m_borderLeftStop = m_selectBorderInfo.FindPropertyRelative("borderLeftStop");
            SerializedProperty m_borderLeftSlopeSize = m_selectBorderInfo.FindPropertyRelative("borderLeftSlopeSize");
            SerializedProperty m_borderRightStop = m_selectBorderInfo.FindPropertyRelative("borderRightStop");
            SerializedProperty m_borderRightSlopeSize = m_selectBorderInfo.FindPropertyRelative("borderRightSlopeSize");

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("|Left Slope", GUILayout.MinWidth(70));
            EditorGUILayout.LabelField("|Left", GUILayout.MinWidth(50));
            EditorGUILayout.LabelField("|Right", GUILayout.MinWidth(50));
            EditorGUILayout.LabelField("|Right Slope", GUILayout.MinWidth(70));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(m_borderLeftSlopeSize, new GUIContent(""), GUILayout.MinWidth(70));
            EditorGUILayout.PropertyField(m_borderLeftStop, new GUIContent(""), GUILayout.MinWidth(50));
            EditorGUILayout.PropertyField(m_borderRightStop, new GUIContent(""), GUILayout.MinWidth(50));
            EditorGUILayout.PropertyField(m_borderRightSlopeSize, new GUIContent(""), GUILayout.MinWidth(70));
            EditorGUILayout.EndHorizontal();
            #endregion
        }

        void InstantiateCrossRoadAfterSplitingRoad()
        {
            #region
            EditorGUILayout.LabelField("Replace a point with a road crossing. ", EditorStyles.boldLabel);

            if (m_closestPoint.intValue == -1)
            {
                EditorGUILayout.HelpBox("You must select a point on the road to split the road into two parts.", MessageType.Warning);
            }
            else if (m_closestPoint.intValue == 0 || m_closestPoint.intValue == m_pointsList.arraySize - 1)
            {
                EditorGUILayout.HelpBox("You must select a point on the road to split the road into two parts.", MessageType.Warning);
                EditorGUILayout.HelpBox("It is not possible to split the road using the first point or the last point of the road.", MessageType.Error);
            }
            else if (GUILayout.Button("Split And create road crossing", GUILayout.Height(30)))
            {
                Bezier myScript = (Bezier)target;

                Undo.RegisterFullObjectHierarchyUndo(myScript.gameObject, myScript.name);

                GameObject crossRoadObj = (GameObject)PrefabUtility.InstantiatePrefab(myScript.roadData.crossRoadprefab as GameObject);

                crossRoadObj.name = "Cross_" + myScript.roadData.iD;

                Undo.RegisterCreatedObjectUndo(crossRoadObj, "crossRoadObj");

                RoadCross roadCross = crossRoadObj.GetComponent<RoadCross>();

                roadCross.roadID = myScript.roadData.iD;

                roadCross.grpDecal.transform.GetChild(0).gameObject.SetActive(false);

                roadCross.transform.SetAsLastSibling();

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

                SerializedObject serializedObject2 = new UnityEditor.SerializedObject(myScript.roadData);
                SerializedProperty m_iD = serializedObject2.FindProperty("iD");
                serializedObject2.Update();
                m_iD.intValue++;
                serializedObject2.ApplyModifiedProperties();


                roadCross.transform.position = myScript.pointsList[m_closestPoint.intValue].points + myScript.transform.position;

                Vector3 dir = (myScript.pointsList[m_closestPoint.intValue - 3].points - myScript.pointsList[m_closestPoint.intValue].points).normalized;
                crossRoadObj.transform.LookAt(myScript.pointsList[m_closestPoint.intValue].points + myScript.transform.position - dir * 1.2f);


                Object prefabRoot = PrefabUtility.GetCorrespondingObjectFromSource(Selection.activeGameObject);
                GameObject roadPart01 = (GameObject)PrefabUtility.InstantiatePrefab(prefabRoot as GameObject);

                roadPart01.transform.SetAsLastSibling();

                PrefabUtility.SetPropertyModifications(roadPart01, PrefabUtility.GetPropertyModifications(Selection.activeGameObject));

                roadPart01.name = "RoadPart01_" + myScript.roadData.iD;

                Undo.RegisterCreatedObjectUndo(roadPart01, "RoadPart01_");

                Bezier bezierPart01 = roadPart01.GetComponent<Bezier>();
                int pointListSize = bezierPart01.pointsList.Count;
                bezierPart01.pointsList.RemoveRange(m_closestPoint.intValue + 1, pointListSize - m_closestPoint.intValue - 1);
                pointListSize = bezierPart01.pointsList.Count;

                bezierPart01.pointsList[pointListSize - 1].points = roadCross.anchorList[3].position - myScript.transform.position;
                bezierPart01.pointsList[pointListSize - 2].points = roadCross.anchorList[3].position - myScript.transform.position
                     + roadCross.anchorList[3].forward * m_lastAnchorDist.floatValue;


                // COnnect the Previous Start CrossRoad if needed
                if (bezierPart01.crossRoadpoint)
                {
                    int whichAnchor = 0;
                    if (bezierPart01.crossDirection == Bezier.CrossDirection.Blue) whichAnchor = 0;
                    if (bezierPart01.crossDirection == Bezier.CrossDirection.Yellow) whichAnchor = 1;
                    if (bezierPart01.crossDirection == Bezier.CrossDirection.Green) whichAnchor = 2;
                    if (bezierPart01.crossDirection == Bezier.CrossDirection.Majenta) whichAnchor = 3;

                    bezierPart01.crossRoadpoint.GetComponent<RoadCross>().roadList[whichAnchor].road = bezierPart01;
                    bezierPart01.crossRoadpoint.GetComponent<RoadCross>().roadList[whichAnchor].roadStartPoint = RoadCross.Direction.Start;
                }


                UpdateRoad(0, HowManyCurvePoints() / 3, true, true, bezierPart01);

                while (!isProcessDone) { }

                ConnectRoadToCrossRoad("LastPoint", 3, roadCross, bezierPart01);

                while (!isProcessDone) { }

                GameObject roadPart02 = (GameObject)PrefabUtility.InstantiatePrefab(prefabRoot as GameObject);

                roadPart02.transform.SetAsLastSibling();

                PrefabUtility.SetPropertyModifications(roadPart02, PrefabUtility.GetPropertyModifications(Selection.activeGameObject));

                roadPart02.name = "RoadPart02_" + myScript.roadData.iD;

                Undo.RegisterCreatedObjectUndo(roadPart02, "RoadPart02_");

                Bezier bezierPart02 = roadPart02.GetComponent<Bezier>();
                int pointListSize2 = bezierPart02.pointsList.Count;
                bezierPart02.pointsList.RemoveRange(0, m_closestPoint.intValue);

                bezierPart02.pointsList[0].points = roadCross.anchorList[1].position - myScript.transform.position;
                bezierPart02.pointsList[1].points = roadCross.anchorList[1].position - myScript.transform.position
                     + roadCross.anchorList[1].forward * m_lastAnchorDist.floatValue;


                // Change RoadPart 02 Pivot
                Vector3 dirPart01ToAnchor1 = (roadCross.anchorList[1].position - bezierPart02.transform.position).normalized;
                float distPart01ToAnchor1 = Vector3.Distance(roadCross.anchorList[1].position, bezierPart02.transform.position);

                for (var i = 0; i < bezierPart02.pointsList.Count; i++)
                    bezierPart02.pointsList[i].points -= dirPart01ToAnchor1 * distPart01ToAnchor1;

                bezierPart02.transform.position = roadCross.anchorList[1].position;

                // Connect the Previous Start CrossRoad if needed
                if (bezierPart02.crossRoadpointIn)
                {
                    int whichAnchor = 0;
                    if (bezierPart02.crossDirectionIn == Bezier.CrossDirection.Blue) whichAnchor = 0;
                    if (bezierPart02.crossDirectionIn == Bezier.CrossDirection.Yellow) whichAnchor = 1;
                    if (bezierPart02.crossDirectionIn == Bezier.CrossDirection.Green) whichAnchor = 2;
                    if (bezierPart02.crossDirectionIn == Bezier.CrossDirection.Majenta) whichAnchor = 3;

                    bezierPart02.crossRoadpointIn.GetComponent<RoadCross>().roadList[whichAnchor].road = bezierPart02;
                    bezierPart02.crossRoadpointIn.GetComponent<RoadCross>().roadList[whichAnchor].roadStartPoint = RoadCross.Direction.End;
                }

                UpdateRoad(0, HowManyCurvePoints() / 3, true, true, bezierPart02);

                while (!isProcessDone) { }

                ConnectRoadToCrossRoad("FirstPoint", 1, roadCross, bezierPart02);

                while (!isProcessDone) { }

                Undo.DestroyObjectImmediate(myScript.gameObject);
                Selection.activeGameObject = roadCross.gameObject;

                isApplySerializedModification = false;
            }
            #endregion
        }

        void SplitFirstPoint()
        {
            #region 
            m_closestPointList.ClearArray();
            m_closestPointList.InsertArrayElementAtIndex(0);
            m_closestPointList.InsertArrayElementAtIndex(0);
            m_closestPointList.GetArrayElementAtIndex(0).intValue = 0;
            m_closestPointList.GetArrayElementAtIndex(0).intValue = 3;
            SplitSegment(0);
            #endregion
        }

        void SplitLastPoint()
        {
            #region 
            m_closestPointList.ClearArray();
            m_closestPointList.InsertArrayElementAtIndex(0);
            m_closestPointList.InsertArrayElementAtIndex(0);
            m_closestPointList.GetArrayElementAtIndex(0).intValue = m_pointsList.arraySize - 4;
            m_closestPointList.GetArrayElementAtIndex(1).intValue = m_pointsList.arraySize - 1;
            SplitSegment(0);
            #endregion
        }
    }
}

#endif

