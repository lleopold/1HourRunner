// Description: RoadCreation: Methods call during road Creation
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HP.Generics
{
    public class RoadCreation
    {
        public static Vector3 GetPointPosition(int id0, int id1, int id2, int id3, float t, List<PointDescription> points)
        {
            #region
            Vector3 p0 = SListPoint(id0, points); Vector3 p1 = SListPoint(id1, points);
            Vector3 p2 = SListPoint(id2, points); Vector3 p3 = SListPoint(id3, points);
            return (1f - t) * (1f - t) * (1f - t) * p0 + 3f * (1f - t) * (1f - t) * t * p1 + 3f * (1f - t) * t * t * p2 + t * t * t * p3;
            #endregion
        }

        // Velocity
        public static Vector3 GetVelocity(int id0, int id1, int id2, int id3, float t, List<PointDescription> points)
        {
            #region
            Vector3 p0 = SListPoint(id0,points); Vector3 p1 = SListPoint(id1, points);
            Vector3 p2 = SListPoint(id2, points); Vector3 p3 = SListPoint(id3, points);
            return
                3f * (1f - t) * (1f - t) * (p1 - p0) +
                6f * (1f - t) * t * (p2 - p1) +
                3f * t * t * (p3 - p2);
            #endregion
        }

        public static Vector3 SListPoint(int index, List<PointDescription> points)
        {
            #region
            int value = (index + points.Count) % points.Count;
            return points[value].points;
            #endregion
        }
    }

    [System.Serializable]
    public class PointDescription
    {
        #region
        public Vector3 points;
        public Quaternion rotation = Quaternion.identity;

        public PointDescription(Vector3 _Point,Quaternion _Rotation)
        {
            points = _Point;
            rotation = _Rotation;
        }
        #endregion
    }

}
