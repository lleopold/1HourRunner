#if (UNITY_EDITOR)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HP.Generics
{
    public class TerrainUpdater
    {
        public static bool isProcessDone = true;
        public static void UpdateTerrainList(Bezier myScript = null, int howManyPoints = 0)
        {
            #region
            isProcessDone = false;
            TerrainModif terrainModif = myScript.GetComponent<TerrainModif>();
            terrainModif.terrList.Clear();

            int progress = 0;
            while (progress != myScript.distVecList.Count - 2)
            {
                int i = progress;
                double startTime = EditorApplication.timeSinceStartup;
                while (EditorApplication.timeSinceStartup < startTime + .001f) { }
                EditorUtility.DisplayProgressBar("Terrain List", "Terrain Found: " + terrainModif.terrList.Count, float.Parse(progress.ToString()) / myScript.distVecList.Count);

                float distanceFromSpot = myScript.distVecList[i].distanceFromSpot;
                int firstSpot = myScript.distVecList[i].firstSpot;
                Vector3 roadUpDir = Vector3.up;

                Vector3 point = RoadCreation.GetPointPosition(firstSpot, firstSpot + 1, firstSpot + 2, firstSpot + 3, distanceFromSpot, myScript.pointsList) + myScript.transform.position;
                Vector3 endPos = point + 1.5f * 30 * RoadCreation.GetVelocity(firstSpot, firstSpot + 1, firstSpot + 2, firstSpot + 3, distanceFromSpot, myScript.pointsList).normalized;
                Vector3 dir = point - endPos;

                for (var j = 0; j < 4; j++)
                {
                    float dist = .5f;
                    if (j == 1) dist = 1f;
                    else if (j == 2) dist = -.5f;
                    else if (j == 3) dist = -1f;

                    Vector3 up = dist * myScript.roadSize * Vector3.Cross(dir, roadUpDir).normalized;

                    #region Check Terrain Cast
                    RaycastHit[] hits;
                    hits = Physics.RaycastAll(point + up + Vector3.up * 500, -Vector3.up, Mathf.Infinity);

                    for (int k = 0; k < hits.Length; k++)
                    {
                        RaycastHit hit = hits[k];
                        if (hit.transform.GetComponent<Terrain>())
                        {
                            bool alreadyExist = false;
                            for (int l = 0; l < terrainModif.terrList.Count; l++)
                            {
                                if (terrainModif.terrList[l] == hit.transform.GetComponent<Terrain>())
                                {
                                    alreadyExist = true;
                                    break;
                                }
                            }

                            if (!alreadyExist)
                                terrainModif.terrList.Add(hit.transform.GetComponent<Terrain>());
                        }
                    }
                    #endregion
                }
                progress++;
            }
            EditorUtility.ClearProgressBar();

            isProcessDone = true;
            #endregion
        }

        public static void UpdateHeight(TerrainModif myScript, bool UpdateSelection = false, RoadCross roadCross = null)
        {
            #region
            isProcessDone = false;
            int progress = 0;
            while (progress != myScript.terrList.Count)
            {
                int m = progress;
                float progressPercentage = float.Parse(progress.ToString()) / myScript.terrList.Count;
                EditorUtility.DisplayProgressBar("Update terrain Height", "Process: " + Mathf.RoundToInt(progressPercentage*100) + "%", progressPercentage);
                Undo.RegisterCompleteObjectUndo(myScript.terrList[m].terrainData, myScript.terrList[m].gameObject.name);

                if (myScript.terrList[m])
                {
                    int reso = myScript.terrList[m].terrainData.heightmapResolution;

                    // Find terrain resolution 
                    int xRes = myScript.terrList[m].terrainData.heightmapResolution;
                    int yRes = myScript.terrList[m].terrainData.heightmapResolution;
                    // Load terrain heightmap
                    float[,] heights = myScript.terrList[m].terrainData.GetHeights(0, 0, xRes, yRes);

                    int ctr = 0;
                    for (var i = 0; i < reso; i++)
                    {
                        for (var j = 0; j < reso; j++)
                        {
                            float xCompletion = myScript.terrList[m].terrainData.size.x * j / (reso - 1);
                            float yCompletion = myScript.terrList[m].terrainData.size.z * i / (reso - 1);

                            Vector3 pos = new Vector3(myScript.terrList[m].transform.position.x + xCompletion, 0, myScript.terrList[m].transform.position.z + yCompletion);
                            Vector3 offsetRayStartPos = Vector3.up * (myScript.terrList[m].terrainData.size.y + 1000);

                            RaycastHit[] hits;
                            hits = Physics.RaycastAll(pos + offsetRayStartPos, -Vector3.up, Mathf.Infinity);

                            for (int k = 0; k < hits.Length; k++)
                            {
                                RaycastHit hit = hits[k];
                                if (hit.transform.GetComponent<RoadTag>() && !UpdateSelection)
                                {
                                    if (IsPartOfTheRoad(myScript.GetComponent<Bezier>(), hit.transform))
                                    {
                                        // Road (Offset to prevent the terrain to pass through the road)
                                        float offstH = myScript.roadOffsetHeight;
                                        // Border (Offset to prevent the road to levitate)
                                        if (hit.transform.GetComponent<RoadTag>().ID == 1)
                                            offstH = 0.0f;

                                        float heightInTerrain = (hit.point.y - offstH) / myScript.terrList[m].terrainData.size.y;

                                        heights[i, j] = Mathf.Clamp01(heightInTerrain);
                                        ctr++;
                                    }
                                }
                                else if (hit.transform.GetComponent<RoadTag>() && UpdateSelection)
                                {
                                    if (IsPartOfTheRoad(myScript.GetComponent<Bezier>(), hit.transform) ||
                                        IsPartOfACrossRoad(hit.transform.GetComponent<RoadTag>().ID,roadCross,hit.transform))
                                    {
                                        float offstH = myScript.roadOffsetHeight;

                                        if (hit.transform.GetComponent<RoadTag>().ID == 2 
                                            || hit.transform.GetComponent<RoadTag>().ID == 3
                                             || hit.transform.GetComponent<RoadTag>().ID == 4
                                             || hit.transform.GetComponent<RoadTag>().ID == 5)
                                        {
                                            if (hit.transform.GetComponent<RoadTag>().ID == 2 || hit.transform.GetComponent<RoadTag>().ID == 4)
                                                offstH = 0.0f;

                                            float heightInTerrain = (hit.point.y - offstH) / myScript.terrList[m].terrainData.size.y;

                                            heights[i, j] = Mathf.Clamp01(heightInTerrain);
                                        }
                                        ctr++;
                                    }
                                }
                            }
                        }
                    }
                    // Update terrain heightmap
                    myScript.terrList[m].terrainData.SetHeights(0, 0, heights);
                }
                progress++;
            }
            EditorUtility.ClearProgressBar();

            isProcessDone = true;
            #endregion
        }

        public static bool IsPartOfTheRoad(Bezier refRoad, Transform hitTransform)
        {
            #region
            for (var n = 0; n < refRoad.selectBorderTrans.Count; n++)
            {
                if (refRoad.selectBorderTrans[n] == hitTransform)
                    return true;
            }
            return false;
            #endregion
        }

        public static bool IsPartOfACrossRoad(int ID, RoadCross roadCross, Transform hitTransform)
        {
            #region
            if (ID == 5 && roadCross != null && roadCross == hitTransform.parent.GetComponent<RoadCross>())
                return true;

            if (ID == 2 && roadCross != null && roadCross == hitTransform.parent.parent.GetComponent<RoadCross>())
                return true;

            if (ID == 4 && roadCross != null && roadCross == hitTransform.parent.parent.GetComponent<RoadCross>())
                return true;


            return false;
            #endregion
        }

        public static void UpdateBumpPathHeight(TerrainModif myScript, bool UpdateSelection = false, RoadCross roadCross = null)
        {
            #region
            isProcessDone = false;
            int progress = 0;
            while (progress != myScript.terrList.Count)
            {
                int m = progress;
                float progressPercentage = float.Parse(progress.ToString()) / myScript.terrList.Count;
                EditorUtility.DisplayProgressBar("Update terrain Height Tag = 6", "Process: " + Mathf.RoundToInt(progressPercentage * 100) + "%", progressPercentage);
                Undo.RegisterCompleteObjectUndo(myScript.terrList[m].terrainData, myScript.terrList[m].gameObject.name);

                if (myScript.terrList[m])
                {
                    int reso = myScript.terrList[m].terrainData.heightmapResolution;

                    // Find terrain resolution 
                    int xRes = myScript.terrList[m].terrainData.heightmapResolution;
                    int yRes = myScript.terrList[m].terrainData.heightmapResolution;
                    // Load terrain heightmap
                    float[,] heights = myScript.terrList[m].terrainData.GetHeights(0, 0, xRes, yRes);

                    int ctr = 0;
                    for (var i = 0; i < reso; i++)
                    {
                        for (var j = 0; j < reso; j++)
                        {
                            float xCompletion = myScript.terrList[m].terrainData.size.x * j / (reso - 1);
                            float yCompletion = myScript.terrList[m].terrainData.size.z * i / (reso - 1);

                            Vector3 pos = new Vector3(myScript.terrList[m].transform.position.x + xCompletion, 0, myScript.terrList[m].transform.position.z + yCompletion);
                            Vector3 offsetRayStartPos = Vector3.up * (myScript.terrList[m].terrainData.size.y + 1000);

                            RaycastHit[] hits;
                            hits = Physics.RaycastAll(pos + offsetRayStartPos, -Vector3.up, Mathf.Infinity);

                            for (int k = 0; k < hits.Length; k++)
                            {
                                RaycastHit hit = hits[k];
                                if (hit.transform.GetComponent<RoadTag>() && UpdateSelection)
                                {
                                    if (hit.transform.GetComponent<RoadTag>().ID == 6)
                                    {
                                        float heightInTerrain = hit.point.y  / myScript.terrList[m].terrainData.size.y;

                                        heights[i, j] = Mathf.Clamp01(heightInTerrain);
                                        ctr++;
                                    }
                                }
                            }
                        }
                    }

                    // Update terrain heightmap
                    myScript.terrList[m].terrainData.SetHeights(0, 0, heights);
                }

                progress++;
            }
            EditorUtility.ClearProgressBar();

            isProcessDone = true;
            #endregion
        }

        public static void UpdateColliderUsedForSelection(bool UpdateSelection = false, Bezier bezier = null)
        {
            #region
            isProcessDone = false;

            if (UpdateSelection)
            {
                Undo.RegisterFullObjectHierarchyUndo(bezier, "Obj");

                RoadMeshGen.RoadBorderMesh(0, 0, "Right", bezier.selectBorderTrans[0], bezier, bezier.distVecList.Count, bezier.selectStart, bezier.selectStop);
                RoadMeshGen.RoadBorderMesh(0, 0, "Left", bezier.selectBorderTrans[1], bezier, bezier.distVecList.Count, bezier.selectStart, bezier.selectStop);
                RoadMeshGen.RoadBorderMesh(0, 0, "RightSlope", bezier.selectBorderTrans[2], bezier, bezier.distVecList.Count, bezier.selectStart, bezier.selectStop);
                RoadMeshGen.RoadBorderMesh(0, 0, "LeftSlope", bezier.selectBorderTrans[3], bezier, bezier.distVecList.Count, bezier.selectStart, bezier.selectStop);

                RoadMeshGen.RoadBorderMesh(0, 0, "SelectionRoadSection", bezier.selectBorderTrans[4], bezier, bezier.distVecList.Count, bezier.selectStart, bezier.selectStop);

            }
            else
            {
                bezier.selectStart = 0;
                bezier.selectStop = bezier.distVecList.Count - 1;
            }

            isProcessDone = true;
            #endregion
        }

        public static void UpdateTerrainProcessDoneFeedback()
        {
            #region
            int progress = 0;
            while (progress != 10)
            {
                int i = progress;
                double startTime = EditorApplication.timeSinceStartup;
                while (EditorApplication.timeSinceStartup < startTime + .1f) { }
                EditorUtility.DisplayProgressBar("Process Done:", "Terrains are updated.", (float)progress / 10);
                progress++;
            }
            EditorUtility.ClearProgressBar();
            #endregion
        }
    }
}
#endif