// Description: InstantiateObjectUsingBezierCurve: Allows to instantiate objects along a path (Bezier curve).
// MeshGen script uses distVecListPlusOffsetFinal list to generate its path.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    public class InstantiateObjectUsingBezierCurve : MonoBehaviour
    {
        public bool                     seeInspector = true;
        public bool                     moreOptions = false;
        public bool                     advanced = false;

        public List<GameObject>         objsList = new List<GameObject>();

        public int                      percentageProba = 20;

        [System.Serializable]
        public class RandomizedObjectParams
        {
            public int proba = 1;
            public GameObject obj;
        }
        public List<RandomizedObjectParams> objsRandomList = new List<RandomizedObjectParams>();

        [Space]

        public Vector3                  objExtraOffset = Vector3.zero;
        public Vector3                  objExtraRotation = Vector3.zero;

        [HideInInspector]
        public List<PointDescription>   pointsList = new List<PointDescription>();

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
        public float                totalDistance = 0;
        [HideInInspector]
        public List<SubPoint>       distVecList = new List<SubPoint>();
        public float                distanceBetweenDistVec = .5f;

       
        public float                distVecOffset = 6f;
        [HideInInspector]
        public List<SubPoint>       distVecListPlusOffsetFinal = new List<SubPoint>();
        public int                  interval = 10;
        [HideInInspector]
        public int                  startPathPos = 0;
        [HideInInspector]
        public int                  endPathPos = 0;
        

        #if (UNITY_EDITOR)
        public RoadMeshGen.RoadStyle roadStyle = RoadMeshGen.RoadStyle.Wire;
        #endif

        public bool                 showGizmo = false;
        [HideInInspector]
        public List<Terrain>        terrList = new List<Terrain>();
           
        public enum PrefabRotation { Vertical, FollowPathRotation,LookAtNextPrefab};
        public PrefabRotation       prefabRotation = PrefabRotation.Vertical;

        public bool                 createFolderInside = false;
        public GameObject           grpThatContainInstantiateObjects;
        void OnDrawGizmosSelected()
        {
            #region
            if (showGizmo)
            {
                for (var i = 0; i < distVecList.Count; i++)
                {
                    Vector3 point = distVecList[i].spotPos + transform.position;
                    Gizmos.DrawSphere(point, .15f);
                }

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
