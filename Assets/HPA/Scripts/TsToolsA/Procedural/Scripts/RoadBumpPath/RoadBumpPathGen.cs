// Description: RoadBumpPathGen: use to create terrain border on dust road
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace HP.Generics{
    public class RoadBumpPathGen : MonoBehaviour
    {
        public List<Vector3>            shapePosList = new List<Vector3>();

        [HideInInspector]
        public List<Vector3>            extrudePathPosList = new List<Vector3>();

        [System.Serializable]
        public class SubPoint
        {
            public Vector3  spotPos;
            public int      firstSpot;
            public float    distanceFromSpot;
            public float    segLength;

            public SubPoint(Vector3 sp, int fS, float dFS)
            {
                spotPos = sp;
                firstSpot = fS;
                distanceFromSpot = dFS;
            }
        }

        [HideInInspector]
        public float                    totalDistance = 0;
        [HideInInspector]
        public List<SubPoint>           distVecList = new List<SubPoint>();
        public float                    distanceBetweenDistVec = .5f;

        [HideInInspector]
        public List<SubPoint>           distVecListPlusOffsetFinal = new List<SubPoint>();

        public bool                     showGizmos = false;

        void OnDrawGizmosSelected()
        {
            #region
            if (showGizmos)
            {
                for (var i = 0; i < distVecListPlusOffsetFinal.Count; i++)
                {
                    Vector3 point = distVecListPlusOffsetFinal[i].spotPos + transform.position;
                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(point, .15f);
                }
            }
            #endregion
        }
    }
}
