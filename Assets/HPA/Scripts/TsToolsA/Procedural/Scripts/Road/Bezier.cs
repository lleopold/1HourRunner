// Description: Bezier: Manage bezier curve and road parameters.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HP.Generics
{
    public class Bezier : MonoBehaviour
    {
        
        public bool                     seeInspector = false;
        
        public bool                     seeCustomEditor = false;
        [HideInInspector]
        public int                      toolbarIndex = 0;

        public RoadData                 roadData;
        public int                      roadID = 0;

        public List<PointDescription>   pointsList = new List<PointDescription>();

        public bool                     linkControlPoints = true;
        public bool                     loop = false;

        public float                    tileSize = 1;

        public float                    roadSize = 8;

        [System.Serializable]
        public class SubPoint
        {
            public Vector3              spotPos;
            public int                  firstSpot;
            public float                distanceFromSpot;
            public float                segLength;

            public SubPoint(Vector3 sp,int fS,float dFS)
            {
                spotPos = sp;
                firstSpot = fS;
                distanceFromSpot = dFS;
            }
        }

        [HideInInspector]
        public float                    totalDistance = 0;

        public List<SubPoint>           distVecList = new List<SubPoint>();

        public Transform                crossRoadpoint;
        public Transform                crossRoadpointIn;

        public enum CrossDirection
        {
            Blue, Yellow, Green, Majenta
        }

        public CrossDirection           crossDirection;
        public CrossDirection           crossDirectionIn;

        public float                    lastAnchorDist = 30;
        public float                    anchorDistWhenNewPointCreated = 10;

        public List<Color>              colorList = new List<Color>();
        public bool                     isChangeDone = true;

        public int                      smoothRotSteps = 15;

        [System.Serializable]
        public class BorderInfo
        {
            [HideInInspector]
            public float     borderLeftStart = 0f;
            public float    borderLeftStop = .5f;
            public float    borderLeftSlopeSize = 1f;

            [HideInInspector]
            public float    borderRightStart = 0f;
            public float    borderRightStop = .5f;
            public float    borderRightSlopeSize = 1f;

            public bool     bShowRoad = true;
            public bool     bShowBorder = true;
            public bool     bShowSlope = true;
        }

        public bool         autoDisableColliderBorders = true;

        public bool         bSelection = true;
        public int          selectStart = 0;
        public int          selectStop = 20;
        public BorderInfo   selectBorderInfo = new BorderInfo();
        public List<Transform> selectBorderTrans = new List<Transform>();

        [HideInInspector]
        public int          hotControlID = 0;
        [HideInInspector]
        public int          closestPoint = 0;


        public List<int>    closestPointList = new List<int>();

        [HideInInspector]
        public int          indexCreatorCrossRoad = 0;
        [HideInInspector]
        public int          indexCrossRoadTab = 0;
        [HideInInspector]
        public int          indexConnectionCrossRoadTab = 0;

        [HideInInspector]
        public int          indexCrossRoadGroundPrefab = 0;


        public bool         isRoadMeshUpdated = true;

        public int          vert = 0;

        public int          roadSubdivisionWhenGenerated = 3;

        public void Reset()
        {
            #region
            loop = false;
            tileSize = 20;
            roadSize = 12;
            totalDistance = 10;
            crossRoadpoint = null;
            crossRoadpointIn = null;
            lastAnchorDist = 30;
            smoothRotSteps = 15;
            GetComponent<MeshFilter>().sharedMesh = null;
            GetComponent<MeshCollider>().sharedMesh = null;

            for(var i = 0;i< 5; i++)
                selectBorderTrans.Add(transform.GetChild(1).GetChild(i).transform);

            for (var i = 0; i < selectBorderTrans.Count; i++)
                selectBorderTrans[i].GetComponent<MeshFilter>().sharedMesh = null;
            for (var i = 0; i < selectBorderTrans.Count; i++)
                selectBorderTrans[i].GetComponent<MeshCollider>().sharedMesh = null;
            #endregion
        }

        void OnDrawGizmosSelected()
        {
            #region
            DisplayRoadBorders();
            #endregion

            #region Display distVecList
            /*
            for (var i = 0; i< distVecList.Count; i++)
                Gizmos.DrawSphere(distVecList[i].spotPos + transform.position,.25f);
            */
            #endregion

            //Gizmos.color = Color.green;
            //Gizmos.DrawSphere(GetComponent<MeshFilter>().mesh.vertices[vert] + transform.position, .25f);

        }

        void DisplayRoadBorders()
        {
            #region
            if (roadData.isGizmosDisplayed)
            {
                int counterCrossRoad = 0;
                int counterCrossRoadOut = smoothRotSteps;
                for (var i = 0; i < distVecList.Count - 2; i++)
                {
                    int firstSpot = distVecList[i].firstSpot;
                    Vector3 roadUpDir = Vector3.up;

                    if (crossRoadpoint && i < smoothRotSteps)
                    {
                        counterCrossRoadOut--;
                        float ratio = ((float)counterCrossRoadOut / smoothRotSteps);

                        roadUpDir = Vector3.up * (1 - ratio) + crossRoadpoint.up * ratio;
                    }
                    if (crossRoadpointIn && (distVecList.Count - smoothRotSteps) < i)
                    {
                        counterCrossRoad++;
                        float ratio = ((float)counterCrossRoad / smoothRotSteps);

                        roadUpDir = Vector3.up * (1 - ratio) + crossRoadpointIn.up * ratio;
                    }

                    Vector3 point = RoadCreation.GetPointPosition(firstSpot, firstSpot + 1, firstSpot + 2, firstSpot + 3, distVecList[i].distanceFromSpot, pointsList);
                    Vector3 endPos = point + 1.5f * 30 * RoadCreation.GetVelocity(firstSpot, firstSpot + 1, firstSpot + 2, firstSpot + 3, distVecList[i].distanceFromSpot, pointsList).normalized;
                    Vector3 dir = point - endPos;



                    int secondSpot = distVecList[i + 1].firstSpot;
                    Vector3 point2 = RoadCreation.GetPointPosition(secondSpot, secondSpot + 1, secondSpot + 2, secondSpot + 3, distVecList[i + 1].distanceFromSpot, pointsList);
                    Vector3 endPos2 = point2 + 1.5f * 30 * RoadCreation.GetVelocity(secondSpot, secondSpot + 1, secondSpot + 2, secondSpot + 3, distVecList[i + 1].distanceFromSpot, pointsList).normalized;
                    Vector3 dir2 = point2 - endPos2;


                    Vector3 tmpPosLeftSelection = Vector3.zero;
                    Vector3 tmpPosLeftSlopSelection = Vector3.zero;
                    Vector3 tmpPosRightSelection = Vector3.zero;
                    Vector3 tmpPosRightSlopSelection = Vector3.zero;

                    for (var j = 0; j < 6; j++)
                    {
                        float dist = -selectBorderInfo.borderLeftStart; // Border Left Start
                        if (j == 1) dist = -selectBorderInfo.borderLeftStop; // Border Left Stop

                        else if (j == 2) dist = -selectBorderInfo.borderLeftStop - selectBorderInfo.borderLeftSlopeSize; // Left Slope


                        else if (j == 3) dist = selectBorderInfo.borderRightStart; // Border Right Start
                        else if (j == 4) dist = selectBorderInfo.borderRightStop; // Border Right Stop

                        else if (j == 5) dist = selectBorderInfo.borderRightStop + selectBorderInfo.borderRightSlopeSize; // Right Slope


                        Vector3 up = Vector3.zero;
                        Vector3 up2 = Vector3.zero;
                        if (j == 0 || j == 1 || j == 2)
                        {
                            up = (dist - .5f) * roadSize * Vector3.Cross(dir, roadUpDir).normalized;
                            up2 = (dist - .5f) * roadSize * Vector3.Cross(dir2, roadUpDir).normalized;
                        }
                        else if (j == 3 || j == 4 || j == 5)
                        {
                            up = (dist + .5f) * roadSize * Vector3.Cross(dir, roadUpDir).normalized;
                            up2 = (dist + .5f) * roadSize * Vector3.Cross(dir2, roadUpDir).normalized;
                        }

                        // A selection is done
                        if (bSelection)
                        {
                            if (i > 1)
                            {
                                if (selectStop > distVecList.Count - 1)
                                    selectStop = distVecList.Count - 1;

                                if (j == 1) dist = -selectBorderInfo.borderLeftStop; // Left stop
                                else if (j == 4) dist = selectBorderInfo.borderRightStop; // Right stop

                                if (j == 2) dist = -selectBorderInfo.borderLeftStop - selectBorderInfo.borderLeftSlopeSize; // Left Slope
                                else if (j == 5) dist = selectBorderInfo.borderRightStop + selectBorderInfo.borderRightSlopeSize; // Right Slope

                                if (j == 4 || j == 1)
                                {

                                    if (j == 1)
                                    {
                                        up = (dist - .5f) * roadSize * Vector3.Cross(dir, roadUpDir).normalized;
                                        up2 = (dist - .5f) * roadSize * Vector3.Cross(dir2, roadUpDir).normalized;
                                        tmpPosLeftSelection = up;
                                    }
                                    if (j == 4)
                                    {
                                        up = (dist + .5f) * roadSize * Vector3.Cross(dir, roadUpDir).normalized;
                                        up2 = (dist + .5f) * roadSize * Vector3.Cross(dir2, roadUpDir).normalized;
                                        tmpPosRightSelection = up;
                                    }
                                }
                                else if (j == 5 || j == 2)
                                {

                                    if (j == 2)
                                    {
                                        up = (dist - .5f) * roadSize * Vector3.Cross(dir, roadUpDir).normalized;
                                        up2 = (dist - .5f) * roadSize * Vector3.Cross(dir2, roadUpDir).normalized;
                                        tmpPosLeftSlopSelection = up;
                                    }
                                    if (j == 5)
                                    {
                                        up = (dist + .5f) * roadSize * Vector3.Cross(dir, roadUpDir).normalized;
                                        up2 = (dist + .5f) * roadSize * Vector3.Cross(dir2, roadUpDir).normalized;
                                        tmpPosRightSlopSelection = up;
                                    }
                                }

                                if (i >= selectStart && i < selectStop - 1)
                                {
                                    if (j == 4)
                                    {
                                        if (!isChangeDone) Gizmos.color = colorList[0];
                                        else Gizmos.color = colorList[2];
                                    }

                                    if (j == 5)
                                    {
                                        if (!isChangeDone) Gizmos.color = colorList[1];
                                        else Gizmos.color = colorList[3];
                                    }
                                    Gizmos.DrawLine(point + up + transform.position, point2 + up2 + transform.position);
                                }

                                if (i >= selectStart && i < selectStop)
                                {
                                    if (j == 4)
                                    {
                                        if (!isChangeDone) Gizmos.color = colorList[0];
                                        else Gizmos.color = colorList[2];
                                    }

                                    if (j == 5)
                                    {
                                        if (!isChangeDone) Gizmos.color = colorList[1];
                                        else Gizmos.color = colorList[3];
                                    }


                                    if (i == selectStart || i == selectStop - 1)
                                    {
                                        if (j == 5)
                                        {
                                            Gizmos.DrawLine(point + tmpPosLeftSelection + transform.position, point + tmpPosLeftSlopSelection + transform.position);
                                            Gizmos.DrawLine(point + tmpPosRightSelection + transform.position, point + tmpPosRightSlopSelection + transform.position);
                                        }
                                        if (j == 4)
                                        {
                                            Gizmos.DrawLine(point - .5f * roadSize * Vector3.Cross(dir, roadUpDir).normalized + transform.position, point + tmpPosLeftSelection + transform.position);
                                            Gizmos.DrawLine(point + .5f * roadSize * Vector3.Cross(dir, roadUpDir).normalized + transform.position, point + tmpPosRightSelection + transform.position);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            #endregion
        }
    }
}