using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    public class WireGroup : MonoBehaviour
    {
        [HideInInspector]
        public bool                     seeInspector = false;
        public bool                     moreOptions = false;

        public List<Wire>               listWire = new List<Wire>();

        public float                    offsetForward = 2f;
        public float                    offsetDown = .5f;

        public float                    precision = .75f;

        #if (UNITY_EDITOR)
        public RoadMeshGen.RoadStyle    roadStyle = RoadMeshGen.RoadStyle.Wire;
        #endif
    }

}
