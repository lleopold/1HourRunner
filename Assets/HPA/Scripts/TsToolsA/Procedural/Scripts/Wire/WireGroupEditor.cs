#if (UNITY_EDITOR)
using UnityEngine;
using UnityEditor;

using System.Collections.Generic;

namespace HP.Generics
{
    [CustomEditor(typeof(WireGroup))]
    public class WireGroupEditor : Editor
    {
        SerializedProperty SeeInspector;                                            // use to draw default Inspector
        SerializedProperty _moreOptions;
        void OnEnable()
        {
            #region
            // Setup the SerializedProperties.
            SeeInspector = serializedObject.FindProperty("seeInspector");
            _moreOptions = serializedObject.FindProperty("moreOptions");
            #endregion
        }

        public override void OnInspectorGUI()
        {
            #region
           // if (SeeInspector.boolValue)
                DrawDefaultInspector();

            serializedObject.Update();

            /* EditorGUILayout.BeginHorizontal();
             EditorGUILayout.LabelField("Show Inspector: ", GUILayout.Width(100));
             EditorGUILayout.PropertyField(SeeInspector, new GUIContent(""), GUILayout.Width(20));
             EditorGUILayout.LabelField("More Options: ", GUILayout.Width(100));
             EditorGUILayout.PropertyField(_moreOptions, new GUIContent(""), GUILayout.Width(20));

             EditorGUILayout.EndHorizontal();
            */
            EditorGUILayout.LabelField("");

            if (GUILayout.Button("Update Group", GUILayout.Height(30)))
                UpdateWireList();

            if(_moreOptions.boolValue)
            {
                EditorGUILayout.LabelField("");
                if (GUILayout.Button("Update List + Update Wires", GUILayout.Height(30)))
                    UpdateList();
            }

            serializedObject.ApplyModifiedProperties();
            #endregion
        }

        void UpdateWireList()
        {
            #region
            WireGroup wireGroup = (WireGroup)target;

            Undo.RegisterFullObjectHierarchyUndo(wireGroup, wireGroup.name);

            for (var i = wireGroup.listWire.Count-1; i >= 0; i--)
                if (!wireGroup.listWire[i])
                    wireGroup.listWire.RemoveAt(i);

            for (var i = 0; i < wireGroup.listWire.Count-1; i++)
            {
                for (var j = 0; j < wireGroup.listWire[i].GetComponent<Wire>().wireList.Count; j++)
                {
                    wireGroup.listWire[i].GetComponent<Wire>().wireList[j].SetActive(true);


                    Bezier bezier = wireGroup.listWire[i].GetComponent<Wire>().wireList[j].GetComponent<Bezier>();

                    Undo.RegisterFullObjectHierarchyUndo(bezier, bezier.name);

                    Transform wirePivotPosStart = wireGroup.listWire[i].GetComponent<Wire>().wireList[j].transform;
                    Transform wirePivotPosEnd = wireGroup.listWire[i + 1].GetComponent<Wire>().wireList[j].transform;

                    Vector3 dir = Vector3.zero;
                    float distanceTangent = 0;
                    Vector3 down = Vector3.zero;

                    switch (wireGroup.roadStyle)
                    {
                        case RoadMeshGen.RoadStyle.Wire:
                            dir = (wirePivotPosEnd.position - wirePivotPosStart.position).normalized;
                            distanceTangent = wireGroup.offsetForward; // = 2;
                            down = Vector3.up * wireGroup.offsetDown; //.5f;
                            break;

                        case RoadMeshGen.RoadStyle.Fence:
                            dir = (wirePivotPosEnd.position - wirePivotPosStart.position).normalized;
                            distanceTangent = wireGroup.offsetForward; // = 1;
                            down = Vector3.up * wireGroup.offsetDown; // 0;
                            break;
                    }

                    bezier.pointsList[0].points = wirePivotPosStart.position - bezier.transform.position;
                    bezier.pointsList[1].points = wirePivotPosStart.position + dir * distanceTangent - down - bezier.transform.position;
                    bezier.pointsList[2].points = wirePivotPosEnd.position - dir * distanceTangent - down - bezier.transform.position;
                    bezier.pointsList[3].points = wirePivotPosEnd.position - bezier.transform.position;

                    RoadMeshGen.ReturnTotalCurveDistance(bezier, wireGroup.precision);

                    while (!RoadMeshGen.isProcessDone) { }

                    bezier.GetComponent<MeshFilter>().sharedMesh = null;

                    int pointsListSize = bezier.pointsList.Count;

                    switch (wireGroup.roadStyle)
                    {
                        case RoadMeshGen.RoadStyle.Wire:
                            RoadMeshGen.RoadMesh(0, pointsListSize, bezier, bezier.distVecList.Count, wireGroup.roadStyle, RoadMeshGen.PlaneFace.Front,false,3);
                            break;

                        case RoadMeshGen.RoadStyle.Fence:
                            if(j%2 == 0)
                                RoadMeshGen.RoadMesh(0, pointsListSize, bezier, bezier.distVecList.Count, wireGroup.roadStyle,RoadMeshGen.PlaneFace.Front,true,3);
                            else
                                RoadMeshGen.RoadMesh(0, pointsListSize, bezier, bezier.distVecList.Count, wireGroup.roadStyle, RoadMeshGen.PlaneFace.Back, true, 3);
                            break;
                    }

                    while (!RoadMeshGen.isProcessDone) { }
                }
            }


            List<GameObject> tmpWireList = wireGroup.listWire[wireGroup.listWire.Count - 1].GetComponent<Wire>().wireList;
            for (var j = 0; j < tmpWireList.Count; j++)
            {
                tmpWireList[j].SetActive(false);
            }

            #endregion
        }

        void UpdateList()
        {
            #region
            WireGroup wireGroup = (WireGroup)target;

            Wire[] allWires = wireGroup.gameObject.GetComponentsInChildren<Wire>();

            Undo.RegisterFullObjectHierarchyUndo(wireGroup, wireGroup.name);

            wireGroup.listWire.Clear();

            for (var i = 0; i < allWires.Length; i++)
                wireGroup.listWire.Add(allWires[i]);

            while (wireGroup.listWire.Count < allWires.Length) { }

            UpdateWireList(); 
            #endregion
        }
    }
}
#endif

