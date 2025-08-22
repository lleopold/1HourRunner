// Description: RoadCross: Use to create Cross road
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    public class RoadCross : MonoBehaviour
    {
        [HideInInspector]
        public bool                 seeInspector = false;
        public RoadData             roadData;

        public int                  roadID = 0;

        public List<Transform>      anchorList = new List<Transform>();

        public enum Direction { Start, End};
        [System.Serializable]
        public class RoadList
        {
            public Bezier       road;
            public Direction    roadStartPoint = Direction.Start;
        }

        public List<RoadList>       roadList = new List<RoadList>();

        public List<Transform>      crossRoadBorders = new List<Transform>();

        public float                borderWidth = 6;
        public float                borderSize = 2;
        public float                borderSlopeSize = 6;
        public float                coverOffset = 3;

        public GameObject           grpDecal;
        public GameObject           objCollider;
        [HideInInspector]
        public int                  currentroadCrossGroundPreset;

        [HideInInspector]
        public int                  indexRoadSection = 0;
        [HideInInspector]
        public bool                 showTerrainBorderParams = false;
        [HideInInspector]
        public int                  indexCrossRoadGroundPrefab = 0;

        public float                anchorDistWhenNewPointCreated = 10;

        public int                  roadSubdivisionWhenGenerated = 3;
        void OnDrawGizmos()
        {
            #region
            for (var i = 0; i < anchorList.Count; i++)
            {
                if (i == 0) Gizmos.color = Color.blue;
                if (i == 1) Gizmos.color = Color.yellow;
                if (i == 2) Gizmos.color = Color.green;
                if (i == 3) Gizmos.color = Color.magenta;

                if (anchorList[i] != null)
                    Gizmos.DrawSphere(anchorList[i].position, .3f);
            }
            #endregion
        }
    }
}
