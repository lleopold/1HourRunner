using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static KWS.KWS_ShaderConstants;

namespace KWS
{

    internal class OceanRenderPass : WaterPass
    {   

        internal static MeshQuadTree _meshQuadTree = new MeshQuadTree();
        
        private GameObject _infiniteOceanShadowCullingAnchor;
        private Transform  _infiniteOceanShadowCullingAnchorTransform;
        Material           shadowFixInfiniteOceanMaterial;
        Mesh               shadowFixInfiniteOceanMesh;

        internal static ComputeBuffer _defaultDrawMeshBuffer;

        public OceanRenderPass()
        {
            SetDefaultBuffer();
        }

        internal static void SetDefaultBuffer()
        {
            KWS_CoreUtils.SetFallbackBuffer<MeshQuadTree.GPUChunkInstance>(ref _defaultDrawMeshBuffer, KWS_ShaderConstants.StructuredBuffers.InstancedMeshData);
        }

        public override void ExecuteBeforeCameraRendering(Camera cam, ScriptableRenderContext context)
        {
           
            
#if KWS_DEBUG
            if (KWS_Ocean.Instance != null && (KWS_Ocean.Instance.DebugQuadtree) && cam.cameraType != CameraType.Game) return;
#endif
            if (!WaterSystem.ForceDisableWaterRendering)
            {
                UpdateQuadTree(cam);
            }
          
            UpdateOceanParams(cam);
            SetGlobalOceanWaterShaderParams();
            
        }
        
        public override void Release()
        {
            _meshQuadTree.Release();
            _defaultDrawMeshBuffer?.Release();
            _defaultDrawMeshBuffer = null;
            
            KW_Extensions.SafeDestroy(shadowFixInfiniteOceanMaterial, shadowFixInfiniteOceanMesh, _infiniteOceanShadowCullingAnchor);
            shadowFixInfiniteOceanMaterial = null;
            shadowFixInfiniteOceanMesh = null;
            _infiniteOceanShadowCullingAnchor = null;
           
            
#if KWS_DEBUG
            DebugHelpers.Release();
#endif
        }
        
        private static void UpdateQuadTree(Camera cam)
        {
            var waterInstance = WaterSystem.Instance;
            if(waterInstance == null) return;

            if (_meshQuadTree.IsInitialized == false || waterInstance.RequireReinitializeMesh)
            {
                _meshQuadTree.Initialize(waterInstance);
                waterInstance.RequireReinitializeMesh = false;
            }
            _meshQuadTree.UpdateQuadTree(cam, WaterSystem.Instance, forceUpdate: waterInstance.RequireUpdateMesh);
            waterInstance.RequireUpdateMesh = false;
        }
        
        internal static void SetGlobalOceanWaterShaderParams()
        {
            var useOcean = KWS_Ocean.Instance != null;
            
            Shader.SetKeyword(WaterKeywords.GlobalKeyword_KWS_USE_OCEAN_RENDERING,  useOcean);
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_RenderOcean,          useOcean ? 1 : 0);
            
            if (!useOcean) return;
            
            var oceanInstance        = KWS_Ocean.Instance;
            
            var useOceanFoam         = WaterQualityLevelSettings.ResolveQualityOverride(oceanInstance.OceanFoam,   WaterSystem.QualitySettings.UseOceanFoam);
            var skyLodRelativeToWind = Mathf.Lerp(0.25f, KWS_Settings.Reflection.MaxSkyLodAtFarDistance, Mathf.Pow(Mathf.Clamp01(oceanInstance.WindSpeed / 15f), 0.35f));
           
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_SkyLodRelativeToWind, skyLodRelativeToWind);
           
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_WindSpeed,      oceanInstance.WindSpeed);
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_WindRotation,   oceanInstance.WindRotation);
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_WindTurbulence, oceanInstance.WindTurbulence);
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_OceanTimeScale, oceanInstance.TimeScale);
            
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_WavesCascades,  oceanInstance.FftWavesCascades);
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_WavesAreaScale, oceanInstance.WavesAreaScale);
           
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_FoamTextureType,            (int)oceanInstance.FoamTextureType);
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_FoamTextureContrast,        oceanInstance.FoamTextureContrast);
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_FoamTextureScaleMultiplier, oceanInstance.FoamTextureScaleMultiplier);
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_OceanRainDropsIntensity,    oceanInstance.RainDropsIntensity);
         

            Shader.SetGlobalInt(ConstantWaterParams.KWS_UseOceanFoam,       useOceanFoam ? 1 : 0);
            Shader.SetGlobalInt(ConstantWaterParams.KWS_UseFilteredNormals, (int)oceanInstance.FftWavesQuality <= 64 ? 1 : 0);
            
            if (useOceanFoam)
            {
                Shader.SetGlobalFloat("KWS_OceanFoamStrength",                 oceanInstance.OceanFoamStrength);
                Shader.SetGlobalFloat("KWS_OceanFoamDisappearSpeedMultiplier", oceanInstance.OceanFoamLifetimeMultiplier);

            }
        }

        void UpdateOceanParams(Camera cam)
        {
            WaterSystem.Instance.CurrentMaxOceanWaveHeight  = 0;
            WaterSystem.Instance.OceanWavesPredictionOffset = 0;
            
            var useOcean      = KWS_Ocean.Instance != null;
            if (useOcean == false) return;
            
            var oceanInstance = KWS_Ocean.Instance;
            
            WaterSystem.Instance.OceanWavesPredictionOffset                        = 0.1f + Mathf.Clamp01(KWS_Ocean.Instance.WindSpeed / 15.0f);
            WaterSystem.Instance.CurrentMaxOceanWaveHeight                         = EvaluateMaxAmplitute();
            oceanInstance.OceanWorldSpaceBounds                                  = CalculateCurrentWorldSpaceBounds(cam);

#if !KWS_HDRP && !KWS_URP
            if (!_infiniteOceanShadowCullingAnchor) CreateInfiniteOceanShadowCullingAnchor();
            UpdateInfiniteOceanShadowCullingAnchor(cam);
#endif
        }

        internal Bounds CalculateCurrentWorldSpaceBounds(Camera cam)
        { 
            var instance = WaterSystem.Instance;

            var    camPos = cam.transform.position;
            camPos.y = instance.WaterLevel;
            
            Bounds bounds;
            bounds = new Bounds(camPos + new Vector3(0, -KWS_Settings.Mesh.MaxInfiniteOceanDepth + instance.CurrentMaxHeightOffsetRelativeToWaterLevel, 0),
                                new Vector3(1000000, KWS_Settings.Mesh.MaxInfiniteOceanDepth * 2, 1000000));
         

            return bounds;
        }

        void UpdateInfiniteOceanShadowCullingAnchor(Camera cam)
        {
            var camPos           = cam.transform.position;
            var waterRelativePos = camPos;
            waterRelativePos.y                                  = WaterSystem.Instance.WaterLevel;
            waterRelativePos.y                                  = Mathf.Min(waterRelativePos.y, camPos.y) - 250;
            _infiniteOceanShadowCullingAnchorTransform.position = waterRelativePos;
            _infiniteOceanShadowCullingAnchorTransform.rotation = Quaternion.Euler(270, 0, 0);
        }

        void CreateInfiniteOceanShadowCullingAnchor()
        {
            _infiniteOceanShadowCullingAnchor = KW_Extensions.CreateHiddenGameObject("InfiniteOceanShadowCullingAnchor");
            shadowFixInfiniteOceanMaterial    = KWS_CoreUtils.CreateMaterial(ShaderNames.InfiniteOceanShadowCullingAnchorName);
            shadowFixInfiniteOceanMesh        = KWS_CoreUtils.CreateQuad();

            _infiniteOceanShadowCullingAnchor.AddComponent<MeshRenderer>().sharedMaterial = shadowFixInfiniteOceanMaterial;
            _infiniteOceanShadowCullingAnchor.AddComponent<MeshFilter>().sharedMesh       = shadowFixInfiniteOceanMesh;


            _infiniteOceanShadowCullingAnchor.transform.rotation   = Quaternion.Euler(270, 0, 0);
            _infiniteOceanShadowCullingAnchor.transform.localScale = new Vector3(100000, 100000, 1);
            _infiniteOceanShadowCullingAnchorTransform             = _infiniteOceanShadowCullingAnchor.transform;
        }

        float FftSmoothstep(float x, float scaleX, float scaleY)
        {
            if (x > scaleX) return scaleY;
            return (-2 * Mathf.Pow(x / scaleX, 3) + 3 * Mathf.Pow(x / scaleX, 2)) * scaleY;
        }


        float EvaluateMaxAmplitute()
        {
            var   oceanInstance  = KWS_Ocean.Instance;
           
            int   cascades       = oceanInstance.FftWavesCascades;
            float windSpeed      = oceanInstance.WindSpeed;
            float windTurbulence = oceanInstance.WindTurbulence;
            float scale          = oceanInstance.WavesAreaScale * 0.5f;

            //main formula -2x^3+3x^2 -> (-2(x/scaleX)^3+3(x/scaleX)^2)*scaleY
            if (cascades == 4)
            {
                return scale * FftSmoothstep(windSpeed, Mathf.Lerp(35, 40, windTurbulence), Mathf.Lerp(9.5f, 23.0f, windTurbulence));
            }
            else if (cascades == 3)
            {
                return scale * FftSmoothstep(windSpeed, Mathf.Lerp(18, 22, windTurbulence), Mathf.Lerp(1.5f, 3.0f, windTurbulence));
            }
            else if (cascades == 2)
            {
                return scale * FftSmoothstep(windSpeed, Mathf.Lerp(8.5f, 8.5f, windTurbulence), Mathf.Lerp(0.38f, 0.51f, windTurbulence));
            }
            else if (cascades == 1)
            {
                return scale * FftSmoothstep(windSpeed, Mathf.Lerp(4, 4, windTurbulence), Mathf.Lerp(0.115f, 0.135f, windTurbulence));
            }

            return 1;

            //cascade | turbulence | wind speed | max amplitude
            //5 | 0 | 50 | 10
            //5 | 0.5 | 50 | 20
            //5 | 1 | 50 | 28
        }

      
    }
}