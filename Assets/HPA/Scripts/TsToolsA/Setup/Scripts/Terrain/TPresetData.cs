using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    [CreateAssetMenu(fileName = "TPresetData", menuName = "HP/TPresetData")]
    public class TPresetData : ScriptableObject
    {
        public List<float>                  heightmapsList = new List<float>();
        public List<float>                  alphamapsList = new List<float>();

        public Vector2                      selectionSize = Vector2.zero;

        public Vector2                      selectionSizeAlphamap = Vector2.zero;

        public GameObject                   prefab;

        [System.Serializable]
        public class TerrainLayerParams
        {
            public List<float> layerTextureList = new List<float>();  
        }

        public List<TerrainLayerParams>     terrainLayer = new List<TerrainLayerParams>();

        [System.Serializable]
        public class TerrainDetailParams
        {
            public List<int>    layerDetailIntensityList = new List<int>();
        }

        public List<TerrainDetailParams>    terrainDetail = new List<TerrainDetailParams>();

        public int                          howManyFadeStepHeight = 12;
        public int                          howManyFadeStepLayertexture = 12;
    }

}
