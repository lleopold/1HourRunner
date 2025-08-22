// Description: TerrainStarter: Use to setup terrains
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HP.Generics
{
    public class TerrainStarter : MonoBehaviour
    {
        [HideInInspector]
        public bool                 seeInspector = false;
        public RoadData             roadData;
        public List<Terrain>        terrList = new List<Terrain>();
        public List<Texture2D>      noisetexList = new List<Texture2D>();
        public Texture2D            noiseOffsetHeight;
        public float                heightOffset = .001f;

        [System.Serializable]
        public class SpawnObjParams
        {
            public bool         show = true;
            public string       name = "";
            public GameObject   objToSpawn;
            public int          probabilityToSpawn = 100;
            public Vector3      offsetPosition;
            public Vector3      randRotation;
            public float        randomScaleMin;
            public float        randomScaleMax;
            public int          safeZone = 5;
            public float        safeHeight = .005f;
        }
        public List<SpawnObjParams> spawnObjList = new List<SpawnObjParams>();

        public int                  gridSize = 10;

        [HideInInspector]
        public int                  editorTabIndex = 0;

        [System.Serializable]
        public class DetailParams
        {
            public bool         isShown = false;
            public string       name;
            public int          probability = 0;
            public float        threshold = 1;
            public int          detailIntensity = 1;

            public Texture2D    detailTexture;
            public GameObject   detailObject;
            public float        minWidth = 1;
            public float        maxWidth = 2;
            public float        minHeight = 1;
            public float        maxHeight = 2;
            public float        noiseSpread = .1f;
            public float        holeEdgePadding = 0;
            public Color        healthyColor = Color.white;
            public Color        dryColor = Color.white;
            public DetailRenderMode detailRenderModeGrass = DetailRenderMode.Grass;
            public DetailRenderMode detailRenderModeObj = DetailRenderMode.VertexLit;

            public DetailParams(Color _healthyColor, Color _dryColor)
            {
                healthyColor = _healthyColor;
                dryColor = _dryColor;
            }
        }

        [System.Serializable]
        public class LayerParams
        {
            public string               name = "";
            public TerrainLayer         terrainLayer;
            public List<DetailParams>   detailList = new List<DetailParams>();
        }
        public List<LayerParams>    layerParamsList = new List<LayerParams>();
        public List<DetailParams>   detailListRef = new List<DetailParams>();

        [System.Serializable]
        public class TerrainSettings
        {
            public float windSpeed = 0;
            public float windSize = 0;
            public float windBending = 0;
            public Color windGrassTint = Color.white;

            public float detailObjectDistance = 150;

            public float terrainWidth = 300;
            public float terrainLength = 300;
            public float terrainHeight = 250;
            public int detailResolutionPatch = 64;
            public int detailResolution = 512;

            public int heightmapResolution = 513;
            public int controlTextureResolution = 512;
            public int baseTextureResolution = 1024;

            public float scaleInLightmap = 0.01f;

            
            public int basemapDistance = 400;
            public float heightmapPixelError = 5f;
            public float treeBillboardDistance = 500f;
            public float treeCrossFadeLength = 5f;

        }
        public TerrainSettings terrainSettings = new TerrainSettings();

        [System.Serializable]
        public class TreeSettings
        {
            public bool isShown = false;
            public string name;
            public GameObject objTree;
            public float bendFactor = 0;
        }
        public List<TreeSettings> treeSettings = new List<TreeSettings>();
    }
}
