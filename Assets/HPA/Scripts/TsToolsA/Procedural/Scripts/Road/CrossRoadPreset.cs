// Description: CrossRoadPreset: Attached to crossRoad.
// This script is used to initilize a cross Road after its creation.
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    public class CrossRoadPreset : MonoBehaviour
    {
        public List<Vector3>    anchorPosList = new List<Vector3>();
        public Vector3          colliderTransformScale = Vector3.zero;
        public int              roadTypeCreatedByDefault = 0;
        public float            anchorDistWhenNewPointCreated = 10;
    }
}
