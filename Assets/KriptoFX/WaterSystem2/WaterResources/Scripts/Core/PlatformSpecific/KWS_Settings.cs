using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace KWS
{
    public static class KWS_Settings
    {
#if !KWS_URP && !KWS_HDRP
        public static readonly int MaskStencilValue = 32; //builtin 32, urp 8, hdrp 128
#endif

#if KWS_URP
        public static readonly int MaskStencilValue = 2;
#endif

#if KWS_HDRP
        public static readonly int MaskStencilValue = 128;
#endif

        public static class Water
        {
            public static readonly int DefaultWaterQueue     = 3000;
            public static readonly int UnderwaterQueueOffset = +1;
            public static readonly int WaterLayer            = 4; //water layer bit mask
            public static readonly int LightLayer            = 0; //light layer bit mask used in urp/hdrp

            public static readonly int MaxRefractionDispersion = 5;


            public static readonly Dictionary<WaterQualityLevelSettings.WaterMeshQualityEnum, int[]> QuadTreeChunkQuailityLevelsInfinite = new Dictionary<WaterQualityLevelSettings.WaterMeshQualityEnum, int[]>()
            {
                { WaterQualityLevelSettings.WaterMeshQualityEnum.Ultra, new[] { 4, 6, 8, 12, 16, 20 } },
                { WaterQualityLevelSettings.WaterMeshQualityEnum.High, new[] { 2, 4, 6, 8, 12, 16 } },
                { WaterQualityLevelSettings.WaterMeshQualityEnum.Medium, new[] { 1, 2, 4, 6, 8, 12 } },
                { WaterQualityLevelSettings.WaterMeshQualityEnum.Low, new[] { 1, 2, 3, 4, 6, 8 } },
                { WaterQualityLevelSettings.WaterMeshQualityEnum.VeryLow, new[] { 1, 2, 3, 4, 5, 6 } }
            };

            public static readonly Dictionary<WaterQualityLevelSettings.WaterMeshQualityEnum, int[]> QuadTreeChunkQuailityLevelsFinite = new Dictionary<WaterQualityLevelSettings.WaterMeshQualityEnum, int[]>()
            {
                { WaterQualityLevelSettings.WaterMeshQualityEnum.Ultra, new[] { 2, 4, 6, 8, 10, 12 } },
                { WaterQualityLevelSettings.WaterMeshQualityEnum.High, new[] { 2, 4, 5, 6, 8, 10 } },
                { WaterQualityLevelSettings.WaterMeshQualityEnum.Medium, new[] { 1, 2, 3, 4, 6, 8 } },
                { WaterQualityLevelSettings.WaterMeshQualityEnum.Low, new[] { 1, 2, 3, 4, 5, 6 } },
                { WaterQualityLevelSettings.WaterMeshQualityEnum.VeryLow, new[] { 1, 1, 2, 3, 4, 5 } }
            };

            public static readonly float[] QuadTreeChunkLodRelativeToWind        = { 0.5f, 0.75f, 1f, 1.5f, 2f, 2.5f };
            public static readonly int     QuadTreeChunkLodOffsetForDynamicWaves = 5;

            public static readonly bool IsPostfxRequireDepthWriting = true;
        }


        public static class ResourcesPaths
        {
            public const string WaterSettingsProfileAssetName = "WaterSettings";

            public static readonly string KWS_FluidsFoamTex                  = "Textures/FluidsFoamTex";
            public static readonly string KWS_PerlinNoise                    = "Textures/KWS_PerlinNoise";
            public static readonly string KWS_OceanFoamTex                   = "Textures/OceanFoamTex";
            public static readonly string KWS_SplashTex0                     = "Textures/WaterSplash_0";
            public static readonly string KWS_WaterDrops                     = "Textures/WaterDrops";
            public static readonly string KWS_WaterDropsMask                 = "Textures/WaterDropsMask";
            public static readonly string KWS_WaterDynamicWavesFlowMapNormal = "Textures/WaterDynamicWavesFlowMapNormal";
            public static readonly string KWS_RainMap                        = "Textures/RainMap";
            
            public static readonly string KWS_BakedSplashParticles = "Prefabs/BakedSplashParticleSystem";
            
            public static readonly string KWS_DefaultVideoLoading = "Textures/KWS_DefaultVideoLoading";
        }

        public static class ShaderPaths
        {
            public static readonly string KWS_WaterDefines = @"Resources/Common/KWS_WaterDefines.cginc";

#if !KWS_HDRP && !KWS_URP
            public static readonly string KWS_PlatformSpecificHelpers = @"Resources/PlatformSpecific/KWS_PlatformSpecificHelpers_Builtin.cginc";
#endif

#if KWS_URP
                public static readonly string KWS_PlatformSpecificHelpers = @"Resources/PlatformSpecific/KWS_PlatformSpecificHelpers_URP.cginc";
#endif

#if KWS_HDRP
                public static readonly string KWS_PlatformSpecificHelpers = @"Resources/PlatformSpecific/KWS_PlatformSpecificHelpers_HDRP.cginc";
#endif
        }

        public static class FFT
        {
            public static readonly float MaxWindSpeed      = 50;
            public static readonly float MaxWavesAreaScale = 4;

            public static readonly Vector4   FftDomainSizes        = new Vector4(5,  20,  100, 600);
            public static readonly Vector4   FftDomainVisibleArea = new Vector4(40, 160, 800, 4800);
            public static readonly Vector4   FftDomainHeightScales = new Vector4(0.5f, 0.5f, 0.6f, 0.9f);
            
        }

        public static class Caustic
        {
            public static readonly float CausticDecalHeight = 5000;
            public static readonly float MaxCausticDepth    = 10;
        }

        public static class SurfaceDepth
        {
            public static readonly float MaxSurfaceDepthMeters = 50;
        }


        public static class VolumetricLighting
        {
            public static readonly float AbsorbtionOverrideMultiplier = 1;
            public static readonly int   MaxIterations                = 8;
        }

        public static class Reflection
        {
            public static readonly float MaxSunStrength         = 3;
            public static readonly float MaxSkyLodAtFarDistance = 1.5f;

#if KWS_HDRP
                public static readonly bool IsCloudRenderingAvailable = true;
                public static readonly bool IsVolumetricsAndFogAvailable = true;
#else
            public static readonly bool IsCloudRenderingAvailable    = false;
            public static readonly bool IsVolumetricsAndFogAvailable = false;
#endif

            public static readonly float AnisotropicReflectionsCurvePower = 3.0f;
        }

        public static class Refraction
        {
            public static readonly bool IsRefractionDownsampleAvailable = true;
        }

        public static class DynamicWaves
        {
            public const           int     MaxDynamicWavesMapLods      = 8;
          //  public static readonly int     DynamicWavesMapTexSize      = 512;
            public static readonly float[] DynamicWavesMapLodDistances = new float[MaxDynamicWavesMapLods] { 50, 100, 200, 400, 800, 1600, 3200, 6400};

            public static readonly float[] DynamicWavesMapLodDistancesInverted = DynamicWavesMapLodDistances.Select(x => 1.0f / x).ToArray();
            
            public static readonly float   UpdateDynamicWavesMapEveryMeters    = 10;
        }

        public static class Mesh
        {
            public static readonly int SplineRiverMinVertexCount = 5;
            public static readonly int SplineRiverMaxVertexCount = 25;

            public static readonly float MaxTesselationFactorInfinite = 12;
            public static readonly float MaxTesselationFactorFinite   = 5;
            public static readonly float MaxTesselationFactorRiver    = 5;
            public static readonly float MaxTesselationFactorCustom   = 15;

            public static readonly int TesselationInfiniteMeshChunksSize = 2;
            public static readonly int TesselationFiniteMeshChunksSize   = 2;

            public static readonly float MaxInfiniteOceanDepth = 5000;


            public static readonly float QuadtreeInfiniteOceanMinDistance        = 10.0f;
            public static readonly float QuadtreeFiniteOceanMinDistance          = 20.0f;
            public static readonly float UpdateQuadtreeEveryMetersForward        = 5f;
            public static readonly float UpdateQuadtreeEveryMetersBackward       = 1.0f;
            public static readonly float QuadtreeRotationThresholdForUpdate      = 0.005f;
            public static readonly float QuadTreeAmplitudeDisplacementMultiplier = 1.25f;

            public static readonly Vector3 MinFiniteSize = new Vector3(0.25f, 0.25f, 0.25f);
        }
    }
}