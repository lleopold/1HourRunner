// Description: RoadData : ScriptableObject
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    [CreateAssetMenu(fileName = "RoadData", menuName = "HP/RoadData")]
    public class RoadData : ScriptableObject
    {
        public bool                     MoreOptions;
        public bool                     HelpBox;
        public int                      currentelectedDatas = 0;

        public GameObject               crossRoadprefab;

        public int                      iD = 0;

        public float                    groundOffset = .03f;

        public bool                     isGizmosDisplayed = true;

        public int                      currentPrefabSelected = 0;
        public bool                     isRoadPrefabShown = true;
        public List<GameObject>         roadPrefabList = new List<GameObject>();
       
        public List<GameObject>         crossRoadGroundPrefabList = new List<GameObject>();

        // Copy a part of a curve
        public List<PointDescription>   pointsList = new List<PointDescription>();
        public Vector3                  curvePosRef = new Vector3();
        public int                      howManyPointToCopy = 0;

        public int                      currentProcedualGD = 0;
    }
}

