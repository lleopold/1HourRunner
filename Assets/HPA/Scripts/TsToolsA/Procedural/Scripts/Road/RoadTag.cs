// Description: RoadTag: Use as a tag to determine the road type when mesh road is created.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    public class RoadTag : MonoBehaviour
    {
        public int ID = 0;
        public RoadType roadType = RoadType.Asphalt;
    }
    public enum RoadType { 
        Default, 
        Asphalt, 
        Dust, 
        Dirt, 
        Ice, 
        Mud, 
        Sand, 
        Gravel, 
        Rock, 
        Grass, 
        RT10 };
}
