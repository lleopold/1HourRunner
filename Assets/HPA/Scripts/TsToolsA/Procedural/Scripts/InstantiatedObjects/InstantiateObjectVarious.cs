// Description: InstantiateObjectVarious:
// Methods used by InstantiateObjectUsingBezierCurveEditor and MeshGenEditor
#if (UNITY_EDITOR)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HP.Generics
{
    public class InstantiateObjectVarious
    {
        public static bool isProcessDone = true;
        public static void UpdateSpawnPoints(InstantiateObjectUsingBezierCurve OBC)
        {
            #region
            isProcessDone = false;

            OBC.totalDistance = RoadMeshGen.ReturnTotalCurveDistanceForObjectUsingABezierCurve(OBC, OBC.GetComponent<Bezier>().pointsList, OBC.distVecList, OBC.distanceBetweenDistVec);
            while (!RoadMeshGen.isProcessDone) { }

            GenerateAListOfSpawnedPoints(OBC);

            while (isProcessDone) { }
            #endregion
        }

        public static void GenerateAListOfSpawnedPoints(InstantiateObjectUsingBezierCurve OBC)
        {
            #region
            isProcessDone = true;

            List<InstantiateObjectUsingBezierCurve.SubPoint> distVecListPlusOffset = new List<InstantiateObjectUsingBezierCurve.SubPoint>();
            distVecListPlusOffset.Clear();

            for (var i = 0; i < OBC.distVecList.Count - 1; i++)
            {
                Vector3 newPos = Vector3.zero;

                int index = i;
                if (i >= OBC.distVecList.Count - 2)
                    index = i - 1;

                Vector3 dir = (OBC.distVecList[index + 1].spotPos - OBC.distVecList[index].spotPos).normalized;
                Vector3 left = OBC.distVecOffset * Vector3.Cross(dir, Vector3.up).normalized;


                if (i == OBC.distVecList.Count - 1)
                    newPos = OBC.GetComponent<Bezier>().pointsList[15].points + left;
                else
                    newPos = OBC.distVecList[i].spotPos + left;

                distVecListPlusOffset.Add(new InstantiateObjectUsingBezierCurve.SubPoint(newPos, 0, 0));
            }

            OBC.pointsList.Clear();
            for (var i = 0; i < distVecListPlusOffset.Count; i++)
            {
                OBC.pointsList.Add(new PointDescription(distVecListPlusOffset[i].spotPos, Quaternion.identity));
            }

            OBC.totalDistance = RoadMeshGen.ReturnTotalCurveDistanceForObjectUsingABezierCurve(OBC, OBC.pointsList, OBC.distVecListPlusOffsetFinal, OBC.distanceBetweenDistVec);

            while (!RoadMeshGen.isProcessDone) { }

            isProcessDone = false;
            #endregion
        }
    }
}
#endif
