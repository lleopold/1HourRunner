#if (UNITY_EDITOR)
using UnityEngine;
using UnityEditor;

using System.Collections.Generic;

namespace HP.Generics
{
    [CustomEditor(typeof(RoadBumpPathGen))]
    public class RoadBumpPathGenEditor : Editor
    {
        private bool isProcessDone  = true;

        int         howManyIteration        = 0;
        int         howManyPoints           = 0;
        Vector3[]   vertices;
        Vector2[]   uvsIndex;
        int[]       triangles;

        void OnEnable()
        {
            #region
            #endregion
        }

        public override void OnInspectorGUI()
        {
            #region
            DrawDefaultInspector();

            serializedObject.Update();

            if (GUILayout.Button("Update Bump Path", GUILayout.Height(30)))
                GenerateBumpPath();

            serializedObject.ApplyModifiedProperties();
            #endregion
        }

        void GenerateBumpPath()
        {
            #region
            RoadBumpPathGen roadBumpPathGen = (RoadBumpPathGen)target;
            UpdatePathInfoUsingBezierPath();
            while (!isProcessDone) { }

            CreateExtrudePathPosList();
            while (!isProcessDone) { }

            InitializeMeshArrays();
            while (!isProcessDone) { }

            CreateTheMesh(roadBumpPathGen, vertices, uvsIndex, triangles);
            while (!isProcessDone) { }

            TerrainUpdater.UpdateBumpPathHeight(roadBumpPathGen.transform.parent.GetComponent<TerrainModif>(), true);
            while (!TerrainUpdater.isProcessDone) { }
            #endregion
        }

        void InitializeMeshArrays()
        {
            #region
            isProcessDone = false;
            RoadBumpPathGen roadBumpPathGen = (RoadBumpPathGen)target;

            howManyIteration = roadBumpPathGen.extrudePathPosList.Count - 1;

            howManyPoints = roadBumpPathGen.shapePosList.Count;

            vertices = new Vector3[howManyPoints * howManyIteration];
            uvsIndex = new Vector2[howManyPoints * howManyIteration];

            triangles = new int[6 * (howManyPoints - 1) + 6 * (howManyPoints) * (howManyIteration - 1)];

            vertices = ReturnVerticesArray(roadBumpPathGen, vertices, howManyIteration, howManyPoints);
            uvsIndex = ReturnUvsIndexArray(roadBumpPathGen, uvsIndex, vertices, howManyIteration, howManyPoints);
            triangles = ReturnTrianglesArray(roadBumpPathGen, triangles, howManyIteration, howManyPoints);

            isProcessDone = true;
            #endregion
        }

        void CreateExtrudePathPosList()
        {
            #region
            isProcessDone = false;

            RoadBumpPathGen roadBumpPathGen = (RoadBumpPathGen)target;
            Undo.RegisterFullObjectHierarchyUndo(roadBumpPathGen, "mesh");
            roadBumpPathGen.transform.localPosition = Vector3.zero;
            roadBumpPathGen.transform.rotation = Quaternion.identity;

            roadBumpPathGen.extrudePathPosList.Clear();
            int counter = 0;
            for (var i = 0; i < roadBumpPathGen.distVecListPlusOffsetFinal.Count; i++)
            {
                roadBumpPathGen.extrudePathPosList.Add(roadBumpPathGen.distVecListPlusOffsetFinal[i].spotPos + roadBumpPathGen.transform.position);
                counter++;
            }

            isProcessDone = true;
            #endregion
        }

        Vector3[] ReturnVerticesArray(RoadBumpPathGen roadBumpPathGen,Vector3[] vertices, int howManyIteration,int howManyPoints)
        {
            #region
            for (var j = 0; j < howManyIteration; j++)
            {
                Vector3 leftDir = Vector3.zero;
                Vector3 forwardDir = Vector3.zero;
                Vector3 upDir = Vector3.zero;

                Vector3 knownedDir = (roadBumpPathGen.extrudePathPosList[j + 1] - roadBumpPathGen.extrudePathPosList[j]).normalized;
                Vector3 up = Vector3.up;

                leftDir = Vector3.Cross(knownedDir, up).normalized;


                for (var i = 0; i < howManyPoints; i++)
                {
                    Vector3 pos = Vector3.zero;

                    if (roadBumpPathGen.extrudePathPosList.Count > 0)
                    {
                        // First line
                        if (j == 0)
                        {
                            pos = roadBumpPathGen.extrudePathPosList[j] - leftDir * roadBumpPathGen.shapePosList[i].x + up * 0 - roadBumpPathGen.transform.position;
                        }
                        // Last line
                        else if (j == howManyIteration - 1)
                        {
                            pos = roadBumpPathGen.extrudePathPosList[j] - leftDir * roadBumpPathGen.shapePosList[i].x + up * 0 - roadBumpPathGen.transform.position;
                        }
                        else
                        {
                            float randomYPos = 0;
                            if (i != 0 && i != 3 && i != 4 && i != 7 && i != 8 && i != 11)
                                randomYPos = UnityEngine.Random.Range(0, .05f);

                            pos = roadBumpPathGen.extrudePathPosList[j] - leftDir * roadBumpPathGen.shapePosList[i].x + up * (roadBumpPathGen.shapePosList[i].y + randomYPos) - roadBumpPathGen.transform.position;
                        }

                        vertices[i + j * howManyPoints] = pos;
                    }  
                }
            }
            return vertices;
            #endregion
        }

        Vector2[] ReturnUvsIndexArray(RoadBumpPathGen roadBumpPathGen, Vector2[] uvsIndex, Vector3[] vertices, int howManyIteration, int howManyPoints)
        {
            #region
            float totalwidth = 0;

            for (var i = 0; i < howManyPoints-1; i++)
                totalwidth += Vector3.Distance(roadBumpPathGen.shapePosList[i + 1], roadBumpPathGen.shapePosList[i]);

            float posY = 0;
          
            for (var j = 0; j < howManyIteration; j++)
            {
                float posX = 0;

                for (var i = 0; i < howManyPoints; i++)
                {
                    if(i > 0)
                    {
                        float distRatioX = Vector3.Distance(roadBumpPathGen.shapePosList[i], roadBumpPathGen.shapePosList[i-1]) / totalwidth;
                        posX += distRatioX;
                    }

                    if (j > 0)
                    {
                        float distRatioY = Vector3.Distance(vertices[i + (j - 1) * howManyPoints], vertices[i + j * howManyPoints]) / 3;
                        posY = uvsIndex[i + (j-1) * howManyPoints].x + distRatioY;
                    }

                    uvsIndex[i + j * howManyPoints] = new Vector2(posY, posX);
                }
            }

            return uvsIndex;
            #endregion
        }

        int[] ReturnTrianglesArray(RoadBumpPathGen roadBumpPathGen, int[] triangles, int howManyIteration, int howManyPoints)
        {
            #region
            int[] triOrder = new int[6] { 0, howManyPoints, 1, 1, howManyPoints, howManyPoints + 1 };

            for (var k = 0; k < howManyIteration - 1; k++)
            {
                for (var j = 0; j < howManyPoints - 1; j++)
                {
                    for (var i = 0; i < triOrder.Length; i++)
                    {
                        triangles[i + (j * 6) + (howManyPoints) * 6 * k] = triOrder[i] + j + (howManyPoints) * k;
                    }
                }
            }

            return triangles;
            #endregion
        }

        void CreateTheMesh(RoadBumpPathGen roadBumpPathGen,Vector3[] vertices, Vector2[] uvsIndex,int[] triangles)
        {
            #region
            isProcessDone = false;
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvsIndex;
            mesh.RecalculateNormals();
            Unwrapping.GenerateSecondaryUVSet(mesh);
            Undo.RegisterFullObjectHierarchyUndo(roadBumpPathGen, "mesh");

            roadBumpPathGen.GetComponent<MeshFilter>().sharedMesh = mesh;

            if(roadBumpPathGen.GetComponent<MeshCollider>()) roadBumpPathGen.GetComponent<MeshCollider>().sharedMesh = mesh;

            isProcessDone = true;
            #endregion
        }

        void OnSceneGUI()
        {
            #region
            DisplayHandles();
            #endregion
        }

        void DisplayHandles()
        {
            #region
            RoadBumpPathGen roadBumpPathGen = (RoadBumpPathGen)target;

            if (roadBumpPathGen.extrudePathPosList.Count > 1)
            {
                float size = HandleUtility.GetHandleSize(roadBumpPathGen.extrudePathPosList[0]);
               
                Vector3 newTargetPosition = Vector3.zero;

                Vector3 leftDir     = Vector3.zero;
                Vector3 forwardDir  = Vector3.zero;
                Vector3 upDir       = Vector3.zero;

                Vector3 knownedDir  = (roadBumpPathGen.extrudePathPosList[1] - roadBumpPathGen.extrudePathPosList[0]).normalized;
                Vector3 up          = Vector3.up;

                leftDir = Vector3.Cross(knownedDir, up).normalized;
                upDir   = Vector3.Cross(leftDir, knownedDir).normalized;

                for (var i = 0; i < roadBumpPathGen.shapePosList.Count; i++)
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 pointPos = roadBumpPathGen.extrudePathPosList[0] + roadBumpPathGen.shapePosList[i];

                    #if UNITY_2022_OR_NEWER
                    newTargetPosition = Handles.FreeMoveHandle(roadBumpPathGen.extrudePathPosList[0] -leftDir * roadBumpPathGen.shapePosList[i].x + up * roadBumpPathGen.shapePosList[i].y, size * .1f, Vector3.zero, Handles.SphereHandleCap);
                    #else
                    var fmh_266_190_638326399978401254 = Quaternion.identity; newTargetPosition = Handles.FreeMoveHandle(roadBumpPathGen.extrudePathPosList[0] - leftDir * roadBumpPathGen.shapePosList[i].x + up * roadBumpPathGen.shapePosList[i].y, size * .1f, Vector3.zero, Handles.SphereHandleCap);
                    #endif

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (newTargetPosition != pointPos)
                        {
                            Undo.RegisterFullObjectHierarchyUndo(roadBumpPathGen, roadBumpPathGen.name);
                            roadBumpPathGen.shapePosList[i] = newTargetPosition - roadBumpPathGen.extrudePathPosList[0];
                        }
                    }
                }
            }
            #endregion
        }

        void UpdatePathInfoUsingBezierPath()
        {
            #region
            isProcessDone = false;
            RoadBumpPathGen roadBumpPathGen = (RoadBumpPathGen)target;

            float newDistVecListUsingBezierPointsList = RoadMeshGen.ReturnTotalCurveDistanceForRoadBumpPath(roadBumpPathGen, roadBumpPathGen.transform.parent.GetComponent<Bezier>().pointsList, roadBumpPathGen.distVecList, roadBumpPathGen.distanceBetweenDistVec);
            while (!RoadMeshGen.isProcessDone) { }

            List<RoadBumpPathGen.SubPoint> distVecListPlusOffset = new List<RoadBumpPathGen.SubPoint>();
            distVecListPlusOffset.Clear();

            for (var i = 0; i < roadBumpPathGen.distVecList.Count - 1; i++)
            {
                int index = i;
                if (i >= roadBumpPathGen.distVecList.Count - 2)
                    index = i - 1;

                Vector3 newPos = roadBumpPathGen.distVecList[i].spotPos;

                distVecListPlusOffset.Add(new RoadBumpPathGen.SubPoint(newPos, 0, 0));
            }

            List<PointDescription> pointsList = new List<PointDescription>();

            for (var i = 0; i < distVecListPlusOffset.Count; i++)
            {
                pointsList.Add(new PointDescription(distVecListPlusOffset[i].spotPos, Quaternion.identity));
            }

            roadBumpPathGen.totalDistance = RoadMeshGen.ReturnTotalCurveDistanceForRoadBumpPath(roadBumpPathGen, pointsList, roadBumpPathGen.distVecListPlusOffsetFinal, roadBumpPathGen.distanceBetweenDistVec);
            while (!RoadMeshGen.isProcessDone) { }

            isProcessDone = true;
            #endregion
        }
    }
}
#endif

