// Description: RoadMeshGen: Methods used to generate road mesh and road colliders.
#if (UNITY_EDITOR)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HP.Generics
{
    public class RoadMeshGen
    {
        public static bool isProcessDone = true;
        public enum RoadStyle { Wire, Fence, Prefab };
        public enum PlaneFace { Front, Back };

       // public static int subdivision = 6;

        public static void RoadMesh(
            int firstPointUpdate = -1, 
            int lastPointUpdate = -1, 
            Bezier myScript = null, 
            int howManyPoints = 0,
            RoadStyle roadStyle = RoadStyle.Wire,
            PlaneFace planeFace = PlaneFace.Front,
            bool isMeshColliderApplied = true,
            int subdivision = 6)
        {
            #region
            isProcessDone = false;
            while (myScript.distVecList.Count != howManyPoints) { }


            // Find First Point
            int firstPointGizmoList = 0;
            for (var j = 0; j < myScript.distVecList.Count; j++)
            {
                if (myScript.distVecList[firstPointGizmoList].firstSpot == firstPointUpdate * 3)
                    break;
                firstPointGizmoList++;
            }

            // Find last point
            int lastPointGizmoList = 0;
            for (var j = firstPointGizmoList; j < myScript.distVecList.Count + firstPointGizmoList; j++)
            {
                if (myScript.distVecList[lastPointGizmoList].firstSpot == lastPointUpdate * 3)
                    break;
                lastPointGizmoList++;
            }

            // Create arrays for the mesh generation
            Vector3[]   vertices    = new Vector3[subdivision * howManyPoints * 2];
            Vector2[]   uvsIndex    = new Vector2[subdivision * howManyPoints * 2];
            int[]       triangles   = new int[6 * (subdivision-1) * howManyPoints];

           
            CopyMeshFilter(myScript, vertices, uvsIndex, triangles);
            while (!isProcessDone) { }

            GenerateVertices(vertices, myScript, firstPointGizmoList, lastPointGizmoList, roadStyle, planeFace, subdivision);
            while (!isProcessDone) { }

            GenerateUvs(vertices, uvsIndex,myScript, firstPointGizmoList,lastPointGizmoList,howManyPoints,planeFace, subdivision);
            while (!isProcessDone) { }

            GenerateTriangles(triangles, firstPointGizmoList, lastPointGizmoList, subdivision);
            while (!isProcessDone) { }

            GenerateMesh(vertices, uvsIndex,  triangles, myScript,isMeshColliderApplied);
            isProcessDone = true;
            #endregion
        }

        public static void CopyMeshFilter(Bezier myScript, Vector3[] vertices, Vector2[] uvsIndex, int[] triangles)
        {
            #region
            isProcessDone = false;
            // Copy a part of the existing mesh if needed
            if (myScript.GetComponent<MeshFilter>().sharedMesh != null)
            {
                myScript.GetComponent<MeshFilter>().sharedMesh.vertices.CopyTo(vertices, 0);
                myScript.GetComponent<MeshFilter>().sharedMesh.uv.CopyTo(uvsIndex, 0);
                myScript.GetComponent<MeshFilter>().sharedMesh.triangles.CopyTo(triangles, 0);
            }
            isProcessDone = true;
            #endregion
        }

        public static void GenerateVertices( Vector3[] vertices, Bezier myScript,  int firstPointGizmoList, int lastPointGizmoList, RoadStyle roadStyle, PlaneFace planeFace, int subdivision)
        {
            #region
            isProcessDone = false;
            int id = firstPointGizmoList;
            int counter = 0;
            float counterCrossRoad = 0;
            float counterCrossRoadOut = myScript.smoothRotSteps;

            for (var j = firstPointGizmoList; j < lastPointGizmoList; j++)
            {
                for (var i = 0; i < subdivision; i++)
                {
                    int cID = id;
                    int firstPos = myScript.distVecList[cID].firstSpot;
                    float distanceFromSpot = myScript.distVecList[cID].distanceFromSpot;

                    // First point
                    if (id == firstPointGizmoList)
                        distanceFromSpot = 0;

                    //Last point case
                    if (cID == vertices.Length / 4 - 1)
                        distanceFromSpot = 1;

                    Vector3 point = RoadCreation.GetPointPosition(firstPos, firstPos + 1, firstPos + 2, firstPos + 3, distanceFromSpot, myScript.pointsList) + myScript.transform.position;

                    if(j == lastPointGizmoList -1)
                        point = myScript.pointsList[myScript.pointsList.Count - 1].points + myScript.transform.position;

                    // Draw Direction
                    Vector3 endPos = point + 1.5f * 30 * RoadCreation.GetVelocity(firstPos, firstPos + 1, firstPos + 2, firstPos + 3, distanceFromSpot, myScript.pointsList).normalized;

                    // Draw perpendicular
                    Vector3 dir = point - endPos;

                    Vector3 roadUpDir = Vector3.up;

                    if (myScript.crossRoadpoint &&
                         cID <= myScript.smoothRotSteps)
                    {
                        if (i == 0)
                            counterCrossRoadOut--;
                        float ratio = (counterCrossRoadOut / myScript.smoothRotSteps);

                        roadUpDir = Vector3.up * (1 - ratio) + myScript.crossRoadpoint.up * ratio;
                    }

                    if (myScript.crossRoadpointIn &&
                        cID > vertices.Length / 4 - 1 - myScript.smoothRotSteps)
                    {
                        if (i == 0)
                            counterCrossRoad++;
                        float ratio = (counterCrossRoad / myScript.smoothRotSteps);

                        roadUpDir = Vector3.up * (1 - ratio) + myScript.crossRoadpointIn.up * ratio;
                    }

                    Vector3 roadDirection = Vector3.zero;
                    switch (roadStyle)
                    {
                        case RoadMeshGen.RoadStyle.Wire:
                            myScript.transform.rotation = Quaternion.identity;
                            roadDirection = .5f * myScript.roadSize * Vector3.Cross(dir, roadUpDir).normalized;
                            break;
                        case RoadMeshGen.RoadStyle.Fence:
                            int faceDir = 1;
                            if (planeFace == PlaneFace.Back) faceDir = -1;
                            myScript.transform.rotation = Quaternion.identity;
                            roadDirection = .5f * myScript.roadSize * Vector3.up * faceDir;
                            break;
                    }


                    float ratio2 = (float)i / (float)(subdivision - 1);
                    vertices[i + j * subdivision] = point - roadDirection + 2 * roadDirection * ratio2 - myScript.transform.position;
                    counter++;
                }
                id += 1;
            }
            //Debug.Log(vertices.Length + " : " + counter);
            isProcessDone = true;
            #endregion
        }

        public static void GenerateUvs(Vector3[] vertices,Vector2[] uvsIndex,  Bezier myScript, int firstPointGizmoList,  int lastPointGizmoList,  int howManyPoints,PlaneFace planeFace,int subdivision = 6)
        {
            #region
            isProcessDone = false;

            int id = firstPointGizmoList;
            int counter = 0;
            Vector3 lastPosUv = Vector3.zero;
            float tmpUvPos = 0;

            for (var j = firstPointGizmoList; j < lastPointGizmoList; j++)
            {
                for (var i = 0; i < subdivision; i++)
                {
                    int cID = id;
                    int firstPos = myScript.distVecList[cID].firstSpot;
                    float distanceFromSpot = myScript.distVecList[cID].distanceFromSpot;

                    // First point
                    if (id == firstPointGizmoList)
                        distanceFromSpot = 0;

                    //Last point case
                    if (cID == vertices.Length / 4 - 1)
                        distanceFromSpot = 1;

                    Vector3 point = RoadCreation.GetPointPosition(firstPos, firstPos + 1, firstPos + 2, firstPos + 3, distanceFromSpot, myScript.pointsList) + myScript.transform.position;

                    if (j == firstPointGizmoList)
                        lastPosUv = point;

                    float uvPosition;// = (float)(j) / (howManyPoints - 1);

                    float distUV = Vector3.Distance(point, lastPosUv);
                    float scaledValue = distUV / myScript.tileSize;

                    float newUVPos = tmpUvPos + scaledValue;


                    if (i == 0)
                        uvPosition = newUVPos;
                    else
                        uvPosition = tmpUvPos;

                    if (planeFace == PlaneFace.Front)
                    {
                        float ratio = (float)i / (float)(subdivision - 1);
                        uvsIndex[i + j * subdivision] = new Vector2(ratio, uvPosition);
                    }
                    else if (planeFace == PlaneFace.Back)
                    {
                        float ratio = (float)i / (float)(subdivision - 1);
                        uvsIndex[i + j * subdivision] = new Vector2(-ratio, uvPosition);
                    }

                    tmpUvPos = uvPosition;

                    if (i == (subdivision - 1))
                        lastPosUv = point;

                    counter++;
                }
                id += 1;
            }
            isProcessDone = true;
            #endregion
        }
      
        public static void GenerateTriangles(int[] triangles,int firstPointGizmoList,int lastPointGizmoList,int subdivision = 6)
        {
            #region
            isProcessDone = false;

            if(subdivision == 2)
            {
                for (var j = firstPointGizmoList; j < lastPointGizmoList - 1; j++)
                {
                    triangles[0 + (j * 6)] = 0 + (2 * j);
                    triangles[1 + (j * 6)] = 2 + (2 * j);
                    triangles[2 + (j * 6)] = 1 + (2 * j);
                    triangles[3 + (j * 6)] = 1 + (2 * j);
                    triangles[4 + (j * 6)] = 2 + (2 * j);
                    triangles[5 + (j * 6)] = 3 + (2 * j);
                }
            }
            else
            {
                for (var j = firstPointGizmoList; j < lastPointGizmoList - 1; j++)
                {
                    int[] trianglesArray = new int[12] { 0, subdivision, 1, 1, subdivision, subdivision + 1, 1, subdivision + 1, 2, 2, subdivision + 1, subdivision + 2 };
                    for (var i = 0; i < (subdivision - 2); i++)
                    {
                        for (var k = 0; k < trianglesArray.Length; k++)
                        {
                            triangles[k + 6 * i + (j * 6 * (subdivision - 1))] = trianglesArray[k] + i + (subdivision * j);
                            int ar = trianglesArray[k] + i + (subdivision * j);
                        }
                    }
                }
            }

            
            isProcessDone = true;
            #endregion
        }

        public static void GenerateMesh(Vector3[] vertices, Vector2[] uvsIndex, int[]  triangles,Bezier myScript,bool isMeshColliderApplied)
        {
            #region
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvsIndex;
            mesh.RecalculateNormals();
            Unwrapping.GenerateSecondaryUVSet(mesh);

            Undo.RegisterFullObjectHierarchyUndo(myScript, "mesh");
            myScript.GetComponent<MeshFilter>().sharedMesh = mesh;

            if (isMeshColliderApplied && myScript.GetComponent<MeshCollider>().enabled)
                myScript.GetComponent<MeshCollider>().sharedMesh = mesh;
            else
                myScript.GetComponent<MeshCollider>().sharedMesh = null;
            #endregion
        }

        public static void RoadBorderMesh(
            int firstPointUpdate = -1
            , int lastPointUpdate = -1, 
            string whichBorder = "", 
            Transform borderObj = null, 
            Bezier myScript = null, 
            int howManyPoints = 0,
            int firstGismo = -1,
            int lastGizmo = -1)
        {
            #region
            isProcessDone = false;
            MeshFilter meshFilter = borderObj.GetComponent<MeshFilter>();

            meshFilter.sharedMesh = null;
            borderObj.GetComponent<MeshCollider>().sharedMesh = null;

            int firstPointGizmoList = 0;
            int lastPointGizmoList = 0;

            // Generate full border road
            if (firstGismo == -1)
            {
                for (var j = 0; j < myScript.distVecList.Count; j++)
                {
                    if (myScript.distVecList[firstPointGizmoList].firstSpot == firstPointUpdate * 3)
                    { break; }
                    firstPointGizmoList++;
                }

                for (var j = firstPointGizmoList; j < myScript.distVecList.Count + firstPointGizmoList; j++)
                {
                    if (myScript.distVecList[lastPointGizmoList].firstSpot == lastPointUpdate * 3)
                    { break; }
                    lastPointGizmoList++;
                }
            }
            // Generate Road border selection
            else
            {
                firstPointGizmoList = firstGismo;
                lastPointGizmoList = lastGizmo;
            }

            Vector3[] vertices = new Vector3[2 * howManyPoints * 2];
            int[] triangles = new int[6 * howManyPoints];


            int id = firstPointGizmoList;

            #region Generate Vertices
            int counter = 0;
            float counterCrossRoad = 0;
            float counterCrossRoadOut = myScript.smoothRotSteps;
            for (var j = firstPointGizmoList; j < lastPointGizmoList; j++)
            {
                for (var i = 0; i < 2; i++)
                {
                    int cID = id;

                    int firstPos = myScript.distVecList[cID].firstSpot;

                    float distanceFromSpot = myScript.distVecList[cID].distanceFromSpot;
                    if (j == lastPointGizmoList - 1 && lastPointGizmoList == myScript.distVecList.Count -1 )
                        distanceFromSpot = 1;


                    if (id == firstPointGizmoList && (firstGismo == -1 || firstGismo == 0))
                        distanceFromSpot = 0;


                    //Last point case
                    if (cID == vertices.Length / 4 - 1)
                        distanceFromSpot = 1;

                    Vector3 point = RoadCreation.GetPointPosition(firstPos, firstPos + 1, firstPos + 2, firstPos + 3, distanceFromSpot, myScript.pointsList) + myScript.transform.position;

                    // Draw Direction
                    Vector3 endPos = point + 1.5f * 30 * RoadCreation.GetVelocity(firstPos, firstPos + 1, firstPos + 2, firstPos + 3, distanceFromSpot, myScript.pointsList).normalized;

                    // Draw perpendicular
                    Vector3 dir = point - endPos;

                    Vector3 roadUpDir = Vector3.up;

                    if (myScript.crossRoadpoint &&
                        cID <= myScript.smoothRotSteps)
                    {
                        if (i == 0)
                            counterCrossRoadOut--;
                        float ratio = (counterCrossRoadOut / myScript.smoothRotSteps);

                        roadUpDir = Vector3.up * (1 - ratio) + myScript.crossRoadpoint.up * ratio;
                    }

                    if (myScript.crossRoadpointIn &&
                        cID > vertices.Length / 4 - 1 - myScript.smoothRotSteps)
                    {
                        if (i == 0)
                            counterCrossRoad++;
                        float ratio = (counterCrossRoad / myScript.smoothRotSteps);

                        roadUpDir = Vector3.up * (1 - ratio) + myScript.crossRoadpointIn.up * ratio;
                    }


                    Vector3 up = .5f * myScript.roadSize * Vector3.Cross(dir, roadUpDir).normalized;

                    // Case Right
                    if (whichBorder == "Right")
                    {
                        if (i == 0) vertices[i + j * 2] = point + up - myScript.transform.position;
                        if (i == 1)
                        {
                            up = (myScript.selectBorderInfo.borderRightStop + .5f) * myScript.roadSize * Vector3.Cross(dir, roadUpDir).normalized;
                            vertices[i + j * 2] = point + up - myScript.transform.position;
                        }

                    }

                    // Case Right Slope
                    if (whichBorder == "RightSlope")
                    {

                        if (i == 0)
                        {
                            up = (myScript.selectBorderInfo.borderRightStop + .5f) * myScript.roadSize * Vector3.Cross(dir, roadUpDir).normalized;
                            vertices[i + j * 2] = point + up - myScript.transform.position;
                        }
                        if (i == 1)
                        {
                            up = (myScript.selectBorderInfo.borderRightStop + myScript.selectBorderInfo.borderRightSlopeSize + .5f) * myScript.roadSize * Vector3.Cross(dir, roadUpDir).normalized;
                            vertices[i + j * 2] = point + up - myScript.transform.position;

                            #region Check Terrain Cast
                            RaycastHit[] hits;
                            hits = Physics.RaycastAll(vertices[i + j * 2] + Vector3.up * 500 + myScript.transform.position, -Vector3.up, Mathf.Infinity);

                            for (int k = 0; k < hits.Length; k++)
                            {
                                RaycastHit hit = hits[k];
                                if (hit.transform.GetComponent<Terrain>())
                                {
                                    vertices[i + j * 2] = new Vector3(vertices[i + j * 2].x, hit.point.y, vertices[i + j * 2].z) - Vector3.up * myScript.transform.position.y;
                                }
                            }
                            #endregion
                        }
                    }

                    // Case Left
                    if (whichBorder == "Left")
                    {
                        if (i == 1) vertices[i + j * 2] = point - up - myScript.transform.position;
                        if (i == 0)
                        {
                            up = (myScript.selectBorderInfo.borderLeftStop + .5f) * myScript.roadSize * Vector3.Cross(dir, roadUpDir).normalized;
                            vertices[i + j * 2] = point - up - myScript.transform.position;
                        }
                    }

                    // Case Left Slope
                    if (whichBorder == "LeftSlope")
                    {
                        if (i == 0)
                        {
                            up = (myScript.selectBorderInfo.borderLeftStop + myScript.selectBorderInfo.borderLeftSlopeSize + .5f) * myScript.roadSize * Vector3.Cross(dir, roadUpDir).normalized;
                            vertices[i + j * 2] = point - up - myScript.transform.position;

                            #region Check Terrain Cast
                            RaycastHit[] hits;
                            hits = Physics.RaycastAll(vertices[i + j * 2] + Vector3.up * 500 + myScript.transform.position, -Vector3.up, Mathf.Infinity);

                            for (int k = 0; k < hits.Length; k++)
                            {
                                RaycastHit hit = hits[k];
                                if (hit.transform.GetComponent<Terrain>())
                                {
                                    vertices[i + j * 2] = new Vector3(vertices[i + j * 2].x, hit.point.y, vertices[i + j * 2].z) - Vector3.up * myScript.transform.position.y;
                                }
                            }
                            #endregion
                        }
                        if (i == 1)
                        {
                            up = (myScript.selectBorderInfo.borderLeftStop + .5f) * myScript.roadSize * Vector3.Cross(dir, roadUpDir).normalized;
                            vertices[i + j * 2] = point - up - myScript.transform.position;
                        }
                    }


                    // Case Selection: Road Section
                    if (whichBorder == "SelectionRoadSection")
                    {
                        if (i == 1) vertices[i + j * 2] = point + up - myScript.transform.position;
                        if (i == 0)
                        {
                            vertices[i + j * 2] = point - up - myScript.transform.position;
                        }
                    }

                    counter++;

                }
                id += 1;
            }
            #endregion

            #region Generate Triangles
            int[] triOrder = new int[6] { 0, 2, 1, 1, 2, 3 };
            for (var j = firstPointGizmoList; j < lastPointGizmoList - 1; j++)
            {
                for (var k = 0; k < triOrder.Length; k++)
                    triangles[k + (j * 6)] = triOrder[k] + (2 * j);
            }
            #endregion

            #region Generate Mesh
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            Unwrapping.GenerateSecondaryUVSet(mesh);
            Undo.RegisterFullObjectHierarchyUndo(borderObj.gameObject, "mesh");
            meshFilter.sharedMesh = mesh;

            borderObj.GetComponent<MeshCollider>().sharedMesh = mesh;
            #endregion
            isProcessDone = true;
            #endregion
        }

        public static void UpdateCrossBorderMesh(RoadCross roadCross,int first,int second,int third,int fourth)
        {
            #region
            isProcessDone = false;
            Vector3[] vertices = new Vector3[4];
            int[] triangles = new int[6];

            Vector3 dirLeft = (roadCross.anchorList[second].localPosition - roadCross.anchorList[fourth].localPosition).normalized;
            Vector3 dirForward = (roadCross.anchorList[first].localPosition - roadCross.anchorList[third].localPosition).normalized;

            float offset = Mathf.Abs(roadCross.anchorList[first].localPosition.x);
            if(first == 1 || first == 3) offset = Mathf.Abs(roadCross.anchorList[first].localPosition.z);

            float borderWidth = roadCross.borderWidth + roadCross.coverOffset; 
            float borderSize = roadCross.borderSize;

            // Generate Vertices
            vertices[0] = dirLeft * borderWidth + dirForward * offset;
            vertices[1] = - dirLeft * borderWidth + dirForward * offset;
            vertices[2] = dirLeft * borderWidth + dirForward * (offset + borderSize);
            vertices[3] = - dirLeft * borderWidth + dirForward * (offset+ borderSize);

            // Generate Triangles
            int[] triOrder = new int[6] { 0, 2, 1, 1, 2, 3 };
            for (var k = 0; k < triOrder.Length; k++)
                triangles[k] = triOrder[k];

            // Create the mesh
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            Unwrapping.GenerateSecondaryUVSet(mesh);
            MeshFilter meshFilter = roadCross.crossRoadBorders[first].GetComponent<MeshFilter>();

            meshFilter.sharedMesh = null;
            roadCross.crossRoadBorders[first].GetComponent<MeshCollider>().sharedMesh = null;

            Undo.RegisterFullObjectHierarchyUndo(roadCross.crossRoadBorders[first].gameObject, "mesh");
            meshFilter.sharedMesh = mesh;
            roadCross.crossRoadBorders[first].GetComponent<MeshCollider>().sharedMesh = mesh;

            isProcessDone = true;
            #endregion
        }

        public static void UpdateCrossBorderSlopeMesh(RoadCross roadCross, int first, int second, int third, int fourth)
        {
            #region
            isProcessDone = false;
            Vector3[] vertices = new Vector3[4];
            int[] triangles = new int[6];

            Vector3 dirLeft = (roadCross.anchorList[second-4].localPosition - roadCross.anchorList[fourth-4].localPosition).normalized;
            Vector3 dirForward = (roadCross.anchorList[first-4].localPosition - roadCross.anchorList[third-4].localPosition).normalized;

            float offset = Mathf.Abs(roadCross.anchorList[first-4].localPosition.x);
            if (first == 5 || first == 7) offset = Mathf.Abs(roadCross.anchorList[first-4].localPosition.z);

            float borderWidth = roadCross.borderWidth + roadCross.coverOffset;
            float borderSize = roadCross.borderSize;
            float borderSlopeSize = roadCross.borderSlopeSize;

            // Generate Vertices
            vertices[0] = dirLeft * borderWidth + dirForward * (offset + borderSize);
            vertices[1] = -dirLeft * borderWidth + dirForward * (offset + borderSize);
            vertices[2] = dirLeft * borderWidth + dirForward * (offset + borderSize + borderSlopeSize);
            vertices[3] = -dirLeft * borderWidth + dirForward * (offset + borderSize + borderSlopeSize);

            int counter = 0;
            for (var i = 2; i < 4; i++)
            {
                RaycastHit[] hits;
                hits = Physics.RaycastAll(roadCross.transform.position  +
                    roadCross.transform.forward * vertices[i].z +
                    roadCross.transform.right * vertices[i].x +
                    Vector3.up * 500, -Vector3.up, Mathf.Infinity);

                for (int k = 0; k < hits.Length; k++)
                {
                    RaycastHit hit = hits[k];
                    if (hit.transform.GetComponent<Terrain>())
                    {
                        counter++;
                        vertices[i] = roadCross.transform.InverseTransformPoint(hit.point);
                    }
                }
            }

            // Generate Triangles
            int[] triOrder = new int[6] { 0, 2, 1, 1, 2, 3 };
            for (var k = 0; k < triOrder.Length; k++)
                triangles[k] = triOrder[k];


            // Generate Mesh
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            Unwrapping.GenerateSecondaryUVSet(mesh);
            MeshFilter meshFilter = roadCross.crossRoadBorders[first].GetComponent<MeshFilter>();

            meshFilter.sharedMesh = null;
            roadCross.crossRoadBorders[first].GetComponent<MeshCollider>().sharedMesh = null;

            Undo.RegisterFullObjectHierarchyUndo(roadCross.crossRoadBorders[first].gameObject, "mesh");
            meshFilter.sharedMesh = mesh;
            roadCross.crossRoadBorders[first].GetComponent<MeshCollider>().sharedMesh = mesh;
            isProcessDone = true;
            #endregion
        }

        public static void UpdateRoadFromCrossRoadEditor(Bezier myScript = null,bool updateBorder = false,int subdivision = 6)
        {
            #region
            ReturnTotalCurveDistance(myScript);
            while (!isProcessDone) { }

            myScript.GetComponent<MeshFilter>().sharedMesh = null;

            int pointsListSize = myScript.pointsList.Count;

            RoadMesh(
                0, 
                pointsListSize,
                myScript, 
                myScript.distVecList.Count, 
                RoadStyle.Wire, 
                PlaneFace.Front, 
                true,
                subdivision);


            while (!isProcessDone) { }

            if (updateBorder)
                TerrainUpdater.UpdateTerrainList(myScript, myScript.distVecList.Count);
            #endregion
        }

        public static float ReturnTotalCurveDistance(Bezier myScript = null,float distanceBetweenTwoPoints = 1.5f)
        {
            #region
            isProcessDone = false;
            if (myScript.pointsList.Count > 0)
            {
                Undo.RegisterFullObjectHierarchyUndo(myScript, "Bezier");

                float dist = 0.0f;
                Vector3 lastPos = myScript.pointsList[0].points;

                float multiplier = 1;

                myScript.distVecList.Clear();

                for (int j = 0; j < myScript.pointsList.Count - 1; j++)
                {
                    if (j % 3 == 0)
                    {
                        float currentPosOnCurve = 0;
                        while (currentPosOnCurve < 1)
                        {
                            Vector3 subPoint = RoadCreation.GetPointPosition(j, j + 1, j + 2, j + 3, currentPosOnCurve, myScript.pointsList);

                            dist += Vector3.Distance(lastPos, subPoint);

                            if (dist >= distanceBetweenTwoPoints * multiplier)
                            {
                                myScript.distVecList.Add(new Bezier.SubPoint(subPoint, j, currentPosOnCurve));
                                multiplier++;
                            }

                            lastPos = subPoint;
                            currentPosOnCurve += .005f;
                        }
                    }
                }
                //lastPos = myScript.pointsList[0].points;

                if (dist != myScript.totalDistance)
                    myScript.totalDistance  = dist;
            }
            isProcessDone = true;
            return 0;
            #endregion
        }

        public static float ReturnTotalCurveDistanceForObjectUsingABezierCurve(
            InstantiateObjectUsingBezierCurve OBC = null, 
            List<PointDescription> pointsList = null,
            List<InstantiateObjectUsingBezierCurve.SubPoint> distVecList = null, 
            float distanceBetweenTwoPoints = 1.5f)
        {
            #region
            isProcessDone = false;

            if (pointsList.Count > 0)
            {
                Undo.RegisterFullObjectHierarchyUndo(OBC, "OBC");

                float dist = 0.0f;
                Vector3 lastPos = pointsList[0].points;

                float multiplier = 1;

                distVecList.Clear();

                distVecList.Add(new InstantiateObjectUsingBezierCurve.SubPoint(pointsList[0].points, 0, 0));

                for (int j = 0; j <pointsList.Count - 3; j++)
                {
                    if (j % 3 == 0)
                    {
                        float currentPosOnCurve = 0;
                        while (currentPosOnCurve < 1)
                        {
                            Vector3 subPoint = RoadCreation.GetPointPosition(j, j + 1, j + 2, j + 3, currentPosOnCurve, pointsList);

                            dist += Vector3.Distance(lastPos, subPoint);

                            if (dist >= distanceBetweenTwoPoints * multiplier)
                            {
                                distVecList.Add(new InstantiateObjectUsingBezierCurve.SubPoint(subPoint, j, currentPosOnCurve));
                                multiplier++;
                            }

                            lastPos = subPoint;

                            currentPosOnCurve += .005f;
                        }
                    }
                }

               distVecList.Add(new InstantiateObjectUsingBezierCurve.SubPoint(pointsList[pointsList.Count-1].points, pointsList.Count - 1, 1));

                if (dist != OBC.totalDistance)
                    OBC.totalDistance = dist;
            }
            isProcessDone = true;
            return 0;
            #endregion
        }

        public static float ReturnTotalCurveDistanceForRoadBumpPath(
            RoadBumpPathGen RBPG = null,
            List<PointDescription> pointsList = null,
            List<RoadBumpPathGen.SubPoint> distVecList = null,
            float distanceBetweenTwoPoints = 1.5f)
        {
            #region
            isProcessDone = false;
       
            if (pointsList.Count > 0)
            {
                Undo.RegisterFullObjectHierarchyUndo(RBPG, "RBPG");

                float dist = 0.0f;
                Vector3 lastPos = pointsList[0].points;

                float multiplier = 1;

                distVecList.Clear();

                distVecList.Add(new RoadBumpPathGen.SubPoint(pointsList[0].points, 0, 0));

                for (int j = 0; j < pointsList.Count - 3; j++)
                {
                    if (j % 3 == 0)
                    {
                        float currentPosOnCurve = 0;
                        while (currentPosOnCurve < 1)
                        {
                            Vector3 subPoint = RoadCreation.GetPointPosition(j, j + 1, j + 2, j + 3, currentPosOnCurve, pointsList);

                            dist += Vector3.Distance(lastPos, subPoint);

                            if (dist >= distanceBetweenTwoPoints * multiplier)
                            {
                                distVecList.Add(new RoadBumpPathGen.SubPoint(subPoint, j, currentPosOnCurve));
                                multiplier++;
                            }

                            lastPos = subPoint;

                            currentPosOnCurve += .005f;
                        }
                    }
                }

                distVecList.Add(new RoadBumpPathGen.SubPoint(pointsList[pointsList.Count - 1].points, pointsList.Count - 1, 1));

                if (dist != RBPG.totalDistance)
                    RBPG.totalDistance = dist;
            }
            isProcessDone = true;
            return 0;
            #endregion
        }

        public static void SetSelectionColliders(RoadCross myScript = null,int index = 0,bool updateRoadFromStarToEnd = false)
        {
            #region
            isProcessDone = false;
            int i = index;
            Undo.RegisterFullObjectHierarchyUndo(myScript.roadList[i].road, "Obj");
            Bezier bezier = myScript.roadList[i].road;

            if (updateRoadFromStarToEnd)
            {
                bezier.selectStart = 0;
                bezier.selectStop = myScript.roadList[i].road.distVecList.Count - 1;
            }
            else
            {
                if (myScript.roadList[i].roadStartPoint == RoadCross.Direction.End)
                {
                    bezier.selectStop = myScript.roadList[i].road.distVecList.Count - 1;
                    bool bFound = false;
                    for (var j = myScript.roadList[i].road.distVecList.Count - 2; j > 0; j--)
                    {
                        if (myScript.roadList[i].road.distVecList[j].firstSpot < myScript.roadList[i].road.pointsList.Count - 4)
                        {
                            bezier.selectStart = j;
                            bFound = true;
                            break;
                        }
                    }
                    if (!bFound)
                        bezier.selectStart = 0;
                }
                else if (myScript.roadList[i].roadStartPoint == RoadCross.Direction.Start)
                {
                    bezier.selectStart = 0;

                    bool bFound = false;
                    for (var j = 0; j < myScript.roadList[i].road.distVecList.Count - 2; j++)
                    {
                        if (myScript.roadList[i].road.distVecList[j].firstSpot > 0)
                        {
                            bezier.selectStop = j;
                            bFound = true;
                            break;
                        }
                    }
                    if (!bFound)
                        bezier.selectStop = myScript.roadList[i].road.distVecList.Count - 1;
                }
            }
            isProcessDone = true;
            #endregion
        }
    }
}
#endif
