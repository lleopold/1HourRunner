using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

using static KWS.KW_Extensions;
using static KWS.KWS_ShaderConstants;

namespace KWS
{
    public partial class WaterSystem
    {

        #region Initialization

        private Texture2D[]    _blueNoiseTextures;
        private Texture2D[]    _causticLod0Textures;
        private Texture2D[]    _causticLod1Textures;
        private Texture2DArray _curlNoiseArray;
        private Texture2D      _rainMap;
        
        private Texture2DArray _bakedFftLod0;
        private Texture2DArray _bakedFftLod1;
        private Texture2DArray _bakedFftLod2;
        private Texture2DArray _bakedFftLod3;
        
        private Texture2DArray _bakedFftNormalLod0;
        private Texture2DArray _bakedFftNormalLod1;
        private Texture2DArray _bakedFftNormalLod2;
        private Texture2DArray _bakedFftNormalLod3;
        
        //If you press ctrl+z after deleting the water gameobject, unity returns all objects without links and save all objects until you close the editor. Not sure how to fix that =/ 
        void ClearUndoObjects(Transform parent)
        {
            if (parent.childCount > 0)
            {
                KW_Extensions.SafeDestroy(parent.GetChild(0).gameObject);
            }
        }


        private void OnAnyWaterSettingsChangedEvent(WaterSettingsCategory changedTab)
        {
            UpdateWaterInstance(changedTab);
        }

        private void UpdateWaterInstance(WaterSettingsCategory changedTab)
        {
            if (changedTab.HasTab(WaterSettingsCategory.Ocean))
            {
                RequireUpdateMesh = true;
            }

            if (changedTab.HasTab(WaterSettingsCategory.Mesh) || changedTab.HasTab(WaterSettingsCategory.Transform) || changedTab.HasTab(WaterSettingsCategory.DynamicWaves))
            {
                RebuildMesh();
            }

            LoadSharedResourceTextures();
        }


        private void LoadSharedResourceTextures()
        {
            //if (QualitySettings.UseIntersectionFoam)
            //{
            //    if (WaterSharedResources.KWS_IntersectionFoamTex == null) WaterSharedResources.KWS_IntersectionFoamTex = Resources.Load<Texture2D>(KWS_Settings.ResourcesPaths.KWS_IntersectionFoamTex);
            //    Shader.SetGlobalTexture("KWS_IntersectionFoamTex", WaterSharedResources.KWS_IntersectionFoamTex);
            //}

            if (QualitySettings != null && QualitySettings.UseOceanFoam)
            {
                if (WaterSharedResources.KWS_OceanFoamTex == null) WaterSharedResources.KWS_OceanFoamTex = Resources.Load<Texture2D>(KWS_Settings.ResourcesPaths.KWS_OceanFoamTex);
                Shader.SetGlobalTexture("KWS_OceanFoamTex", WaterSharedResources.KWS_OceanFoamTex);
            }

            //todo check zones relative load
            // if (WaterZoneManager.DynamicWavesSimulationZones.Count > 0 || QualitySettings.UseOceanFoam || QualitySettings.UseIntersectionFoam)
            {
                if (WaterSharedResources.KWS_FluidsFoamTex == null) WaterSharedResources.KWS_FluidsFoamTex = Resources.Load<Texture2D>(KWS_Settings.ResourcesPaths.KWS_FluidsFoamTex);
                Shader.SetGlobalTexture("KW_FluidsFoamTex", WaterSharedResources.KWS_FluidsFoamTex);
                
                if (WaterSharedResources.KWS_PerlinNoise == null) WaterSharedResources.KWS_PerlinNoise = Resources.Load<Texture2D>(KWS_Settings.ResourcesPaths.KWS_PerlinNoise);
                Shader.SetGlobalTexture("KWS_PerlinNoise", WaterSharedResources.KWS_PerlinNoise);

                if (WaterSharedResources.KWS_SplashTex == null) { WaterSharedResources.KWS_SplashTex = Resources.Load<Texture2D>(KWS_Settings.ResourcesPaths.KWS_SplashTex0); }
                Shader.SetGlobalTexture("KWS_SplashTex0", WaterSharedResources.KWS_SplashTex);
                
                if (WaterSharedResources.KWS_RainMap == null) WaterSharedResources.KWS_RainMap = Resources.Load<Texture2D>(KWS_Settings.ResourcesPaths.KWS_RainMap);
                Shader.SetGlobalTexture("KWS_RainMap", WaterSharedResources.KWS_RainMap);
            }

            if (Instance.UseWaterDropsEffect)
            {
                if (WaterSharedResources.KWS_WaterDropsTexture == null) WaterSharedResources.KWS_WaterDropsTexture = Resources.Load<Texture2D>(KWS_Settings.ResourcesPaths.KWS_WaterDrops);
                Shader.SetGlobalTexture("KWS_WaterDropsTexture", WaterSharedResources.KWS_WaterDropsTexture);

                if (WaterSharedResources.KWS_WaterDropsMaskTexture == null) WaterSharedResources.KWS_WaterDropsMaskTexture = Resources.Load<Texture2D>(KWS_Settings.ResourcesPaths.KWS_WaterDropsMask);
                Shader.SetGlobalTexture("KWS_WaterDropsMaskTexture", WaterSharedResources.KWS_WaterDropsMaskTexture);
            }

            if (_curlNoiseArray == null) _curlNoiseArray = Resources.Load<Texture2DArray>("Textures/CurlNoiseArray");
            Shader.SetGlobalTexture("KWS_CurlNoiseArray", _curlNoiseArray);
            
            
            //  if (WaterZoneManager.DynamicWavesSimulationZones.Count > 0)
            {
                if (WaterSharedResources.KWS_WaterDynamicWavesFlowMapNormal == null) WaterSharedResources.KWS_WaterDynamicWavesFlowMapNormal = Resources.Load<Texture2D>(KWS_Settings.ResourcesPaths.KWS_WaterDynamicWavesFlowMapNormal);
                Shader.SetGlobalTexture("KWS_WaterDynamicWavesFlowMapNormal", WaterSharedResources.KWS_WaterDynamicWavesFlowMapNormal);
            }
#if KWS_DEBUG
            if (_bakedFftLod0 == null) _bakedFftLod0 = Resources.Load<Texture2DArray>("Textures/Ocean/FFT_0");
            if (_bakedFftLod1 == null) _bakedFftLod1 = Resources.Load<Texture2DArray>("Textures/Ocean/FFT_1");
            if (_bakedFftLod2 == null) _bakedFftLod2 = Resources.Load<Texture2DArray>("Textures/Ocean/FFT_2");
            if (_bakedFftLod3 == null) _bakedFftLod3 = Resources.Load<Texture2DArray>("Textures/Ocean/FFT_3");
            
            if (_bakedFftNormalLod0 == null) _bakedFftNormalLod0 = Resources.Load<Texture2DArray>("Textures/Ocean/Normals_0");
            if (_bakedFftNormalLod1 == null) _bakedFftNormalLod1 = Resources.Load<Texture2DArray>("Textures/Ocean/Normals_1");
            if (_bakedFftNormalLod2 == null) _bakedFftNormalLod2 = Resources.Load<Texture2DArray>("Textures/Ocean/Normals_2");
            if (_bakedFftNormalLod3 == null) _bakedFftNormalLod3 = Resources.Load<Texture2DArray>("Textures/Ocean/Normals_3");
            
            Shader.SetGlobalTexture("KWS_BakedFFT_Lod0", _bakedFftLod0);
            Shader.SetGlobalTexture("KWS_BakedFFT_Lod1", _bakedFftLod1);
            Shader.SetGlobalTexture("KWS_BakedFFT_Lod2", _bakedFftLod2);
            Shader.SetGlobalTexture("KWS_BakedFFT_Lod3", _bakedFftLod3);
            
            Shader.SetGlobalTexture("KWS_BakedNormalFFT_Lod0", _bakedFftNormalLod0);
            Shader.SetGlobalTexture("KWS_BakedNormalFFT_Lod1", _bakedFftNormalLod1);
            Shader.SetGlobalTexture("KWS_BakedNormalFFT_Lod2", _bakedFftNormalLod2);
            Shader.SetGlobalTexture("KWS_BakedNormalFFT_Lod3", _bakedFftNormalLod3);
#endif
            
#if KWS_DEBUG
            if (WaterSystem.TestTexture != null)
            {
                Shader.SetGlobalTexture("KWS_TestTexture", WaterSystem.TestTexture);
            }
#endif

        }

        void LoadPerFrameTextures()
        {
            if (_blueNoiseTextures == null) _blueNoiseTextures = Resources.LoadAll<Texture2D>("Textures/STBN");
            if (_causticLod0Textures           == null) _causticLod0Textures = Resources.LoadAll<Texture2D>("Textures/Caustic/Lod0");
            if (_causticLod1Textures           == null) _causticLod1Textures = Resources.LoadAll<Texture2D>("Textures/Caustic/Lod1");
           
         
        }

        
        void UpdatePerFrameTextures(CustomFixedUpdates fixedUpdates)
        {
            //per frame update
            if (_blueNoiseTextures != null && _blueNoiseTextures.Length > 0)
            {
                int currentIndex                                                         = (int)(KWS_UpdateManager.GlobalFrame % (float)_blueNoiseTextures.Length);
                Shader.SetGlobalTexture("KWS_BlueNoise3D", _blueNoiseTextures[currentIndex]);
            }

            if (_causticLod0Textures != null && _causticLod0Textures.Length > 0)
            {
                int currentIndex = (int)(KWS_UpdateManager.Global30FpsFrame % (float)_causticLod0Textures.Length);
                Shader.SetGlobalTexture("KWS_CausticFrameLod0", _causticLod0Textures[currentIndex]);
            }

            if (_causticLod1Textures != null && _causticLod1Textures.Length > 0)
            {
                int currentIndex = (int)(KWS_UpdateManager.Global45FpsFrame % (float)_causticLod1Textures.Length);
                Shader.SetGlobalTexture("KWS_CausticFrameLod1", _causticLod1Textures[currentIndex]);
            }
            
        }


        internal void InitializeOrUpdateMesh()
        {
            RequireReinitializeMesh = true;
        }


    
        internal void RebuildMesh()
        {
            InitializeOrUpdateMesh();
        }



        void InitializeWaterCommonResources()
        {
            InitializeOrUpdateMesh();

            IsWaterInitialized = true;
        }

        private static void UnloadResources()
        {
            if (WaterSharedResources.KWS_OceanFoamTex != null)
            {
                Resources.UnloadAsset(WaterSharedResources.KWS_OceanFoamTex);
                WaterSharedResources.KWS_OceanFoamTex = null;
            }

            if (WaterSharedResources.KWS_IntersectionFoamTex != null)
            {
                Resources.UnloadAsset(WaterSharedResources.KWS_IntersectionFoamTex);
                WaterSharedResources.KWS_IntersectionFoamTex = null;
            }

            if (WaterSharedResources.KWS_FluidsFoamTex != null)
            {
                Resources.UnloadAsset(WaterSharedResources.KWS_FluidsFoamTex);
                WaterSharedResources.KWS_FluidsFoamTex = null;
            }
            
            if (WaterSharedResources.KWS_PerlinNoise != null)
            {
                Resources.UnloadAsset(WaterSharedResources.KWS_PerlinNoise);
                WaterSharedResources.KWS_PerlinNoise = null;
            }
        }

        static WaterSurfaceRequestPoint _worldPointRequest = new WaterSurfaceRequestPoint();
        private static Dictionary<Camera, UnderwaterSurfaceState> _underwaterStateCameras = new Dictionary<Camera, UnderwaterSurfaceState>();

        private const int MinHeightToDropSplashDrops = 1;

        internal class UnderwaterSurfaceState
        {
            public WaterSurfaceRequestArray Request = new WaterSurfaceRequestArray();
            public Vector3[] CameraNearPlanePoints = new Vector3[6];

            public bool isCameraPartialUnderwater;
            public bool isCameraFullUnderwater;
            public bool IsCameraRequireWaterDrops;
        }

        internal static void RequestUnderwaterState(HashSet<Camera> cameras)
        {
            var useUnderwaterEffect = WaterQualityLevelSettings.ResolveQualityOverride(Instance.UnderwaterEffect, WaterSystem.QualitySettings.UseUnderwaterEffect);
            if (useUnderwaterEffect == false) return;
            
            foreach (var stateCamera in _underwaterStateCameras.Keys.ToList())
            {
                if (!cameras.Contains(stateCamera))
                {
                    _underwaterStateCameras.Remove(stateCamera);
                }
            }

            foreach (var camera in cameras)
            {
                if (!_underwaterStateCameras.ContainsKey(camera))
                {
                    _underwaterStateCameras[camera] = new UnderwaterSurfaceState();
                }
            }

            var wavesPredictionOffset = Instance.OceanWavesPredictionOffset * Vector3.down;  //underwater prediction because async readback can't have 100% accuracy position, because ~1 frame delay. 

            foreach (var stateCamera in _underwaterStateCameras)
            {
                var cam = stateCamera.Key;
                CalculateNearPlaneWorldPoints(stateCamera.Key, ref stateCamera.Value.CameraNearPlanePoints);
                stateCamera.Value.CameraNearPlanePoints[0] = ViewportToWorldPoint(cam, new Vector3(0, 0, cam.nearClipPlane)); //bot left
                stateCamera.Value.CameraNearPlanePoints[1] = ViewportToWorldPoint(cam, new Vector3(1, 0, cam.nearClipPlane)); //bot right
                stateCamera.Value.CameraNearPlanePoints[2] = ViewportToWorldPoint(cam, new Vector3(0, 1, cam.nearClipPlane)); //top left
                stateCamera.Value.CameraNearPlanePoints[3] = ViewportToWorldPoint(cam, new Vector3(1, 1, cam.nearClipPlane)); //top right


                stateCamera.Value.CameraNearPlanePoints[4] = stateCamera.Value.CameraNearPlanePoints[0] + wavesPredictionOffset;
                stateCamera.Value.CameraNearPlanePoints[5] = stateCamera.Value.CameraNearPlanePoints[1] + wavesPredictionOffset;


                stateCamera.Value.Request.SetNewPositions(stateCamera.Value.CameraNearPlanePoints);
                WaterSystem.TryGetWaterSurfaceData(stateCamera.Value.Request);
            }

            foreach (var stateCamera in _underwaterStateCameras)
            {
                stateCamera.Value.isCameraFullUnderwater    = false;
                stateCamera.Value.isCameraPartialUnderwater = false;
                
                if (!stateCamera.Value.Request.IsDataReady) continue;

                
                var result = stateCamera.Value.Request.Result;
                var points = stateCamera.Value.CameraNearPlanePoints;
                const float cameraMinThreshold = 0.02f;

                if (KWS_Ocean.Instance == null)
                {
                    if(result[0].HasWater == 0 ||
                       result[1].HasWater == 0 ||
                       result[2].HasWater == 0 ||
                       result[3].HasWater == 0) continue;
                }
              
                if (points[5].y < result[5].Position.y + cameraMinThreshold
                 || points[4].y < result[4].Position.y + cameraMinThreshold
                 || points[0].y < result[0].Position.y + cameraMinThreshold
                 || points[1].y < result[1].Position.y + cameraMinThreshold
                 || points[2].y < result[2].Position.y + cameraMinThreshold
                 || points[3].y < result[3].Position.y + cameraMinThreshold)
                    stateCamera.Value.isCameraPartialUnderwater = true;
                else stateCamera.Value.isCameraPartialUnderwater = false;

                if (points[0].y < result[0].Position.y + cameraMinThreshold
                 && points[1].y < result[1].Position.y + cameraMinThreshold
                 && points[2].y < result[2].Position.y + cameraMinThreshold
                 && points[3].y < result[3].Position.y + cameraMinThreshold) stateCamera.Value.isCameraFullUnderwater = true;
                else stateCamera.Value.isCameraFullUnderwater = false;

                stateCamera.Value.IsCameraRequireWaterDrops = (points[0].y < result[0].Position.y + MinHeightToDropSplashDrops) && result[0].Foam > 0.2f;

            }

            var localZones = KWS_TileZoneManager.VisibleLocalWaterZones;
            foreach (var iZone in localZones)
            {
                var zone = (KWS_LocalWaterZone)iZone;
                if (zone.OverrideMesh && zone.CustomMesh)
                {
                    foreach (var stateCamera in _underwaterStateCameras)
                    {
                        if (zone.Bounds.Contains(stateCamera.Key.transform.position))
                        {
                            stateCamera.Value.isCameraPartialUnderwater = true;
                        }
                    }
                }
            }
            //
            // if (Instance.RenderOcean == false)
            // {
            //     var dynamicWavesZones = KWS_TileZoneManager.VisibleDynamicWavesZones;
            //     foreach (var iZone in dynamicWavesZones)
            //     {
            //         var zone = (KWS_DynamicWavesSimulationZone)iZone;
            //         foreach (var stateCamera in _underwaterStateCameras)
            //         {
            //             if (zone.Bounds.Contains(stateCamera.Key.transform.position))
            //             {
            //                 stateCamera.Value.isCameraPartialUnderwater = true;
            //             }
            //         }
            //     }
            // }


        }


        #endregion

        #region Render Logic 

        internal void ExecutePerFrame(HashSet<Camera> cameras, CustomFixedUpdates fixedUpdates)
        {
            if (!IsWaterInitialized) InitializeWaterCommonResources();
            UpdatePerFrameTextures(fixedUpdates);
            
            var cam = KWS_CoreUtils.GetFixedUpdateCamera(cameras);
            if (cam != null)
            {
                SetGlobalCameraShaderParams(cam);
            }
        }

   
        internal void ExecutePerCamera(Camera cam)
        {
         
            if (!KWS_UpdateManager.FrustumCaches.TryGetValue(cam, out var frustumCache)) return;

            var bounds = Instance.WorldSpaceBoundsWithZones;
            IsWaterVisible = KW_Extensions.IsBoxVisibleAccurate(ref frustumCache.FrustumPlanes, ref frustumCache.FrustumCorners, bounds.min, bounds.max);

            var useUnderwaterEffect     = WaterQualityLevelSettings.ResolveQualityOverride(UnderwaterEffect, QualitySettings.UseUnderwaterEffect);
            if (useUnderwaterEffect && _underwaterStateCameras.ContainsKey(cam))
            {
                IsCameraPartialUnderwater = _underwaterStateCameras[cam].isCameraPartialUnderwater;
                IsCameraFullUnderwater    = _underwaterStateCameras[cam].isCameraFullUnderwater;
                IsCameraRequireWaterDrops = _underwaterStateCameras[cam].IsCameraRequireWaterDrops;

                if (IsCameraPartialUnderwater && KWS_TileZoneManager.VisibleClipMeshZones.Count > 0)
                {
                    var cameraPos = cam.transform.position;
                    foreach (var clipZone in KWS_TileZoneManager.VisibleClipMeshZones)
                    {
                        if (clipZone.Bounds.Contains(cameraPos))
                        {
                            IsCameraPartialUnderwater = false;
                            IsCameraFullUnderwater    = false;
                            IsCameraRequireWaterDrops = false;
                            break;
                        }
                    }
                }
            }

#if KWS_DEBUG
           // Test4 = new Vector4(-60, 25, 0, 25);
            Shader.SetGlobalVector("Test4", Test4);
            if (KWS_Ocean.Instance != null && KWS_Ocean.Instance.DebugUnderwater) IsCameraPartialUnderwater = true;
            if (KWS_CoreUtils.SinglePassStereoEnabled) UnityEngine.XR.XRSettings.eyeTextureResolutionScale  = VRScale;
#endif


            
            
            SetGlobalCameraShaderParams(cam);
            SetGlobalPlatformSpecificShaderParams(cam);

            SetQualitySettingsGlobalKeywords(cam);
            SetQualitySettingsShaderParams();
            SetSettingsConstantShaderParams();
        }



        internal static Matrix4x4[] KWS_MATRIX_VP;

        internal static void SetGlobalCameraShaderParams(Camera cam)
        {
            KWS_MATRIX_VP = KWS_CoreUtils.SetAllVPCameraMatricesAndGetVP(cam);

            Shader.SetGlobalInt(DynamicWaterParams.KWS_IsCameraPartialUnderwater, IsCameraPartialUnderwater ? 1 : 0);

            Shader.SetGlobalFloat(DynamicWaterParams.KWS_Time, KW_Extensions.TotalTime());
            Shader.SetGlobalFloat(ConstantWaterParams.KW_GlobalTimeScale, WaterSystem.GlobalTimeScale);

            Shader.SetGlobalInteger(ConstantWaterParams.KWS_WaterLayerMask, KWS_Settings.Water.WaterLayer);
            Shader.SetGlobalInteger(ConstantWaterParams.KWS_WaterLightLayerMask, KWS_Settings.Water.LightLayer);

            var camTransform = cam.transform;
            Shader.SetGlobalVector(DynamicWaterParams.KWS_CameraForward,       camTransform.forward);
            Shader.SetGlobalVector(DynamicWaterParams.KWS_CameraRight,         camTransform.right);
            Shader.SetGlobalVector(DynamicWaterParams.KWS_CameraUp,            camTransform.up);
            Shader.SetGlobalVector(DynamicWaterParams.KWS_WorldSpaceCameraPos, camTransform.position);

            Shader.SetGlobalInteger(DynamicWaterParams.KWS_IsEditorCamera, cam.cameraType == CameraType.SceneView ? 1 : 0);
        }


     
        static void SetQualitySettingsGlobalKeywords(Camera cam)
        {

            var usePlanarReflection      = WaterQualityLevelSettings.ResolveQualityOverride(Instance.PlanarReflection,      QualitySettings.UsePlanarReflection);
            var useScreenSpaceReflection = WaterQualityLevelSettings.ResolveQualityOverride(Instance.ScreenSpaceReflection, QualitySettings.UseScreenSpaceReflection);

            var useCausticEffect                    = WaterQualityLevelSettings.ResolveQualityOverride(Instance.CausticEffect,                          QualitySettings.UseCausticEffect);
            var useRefractionDispersion             = WaterQualityLevelSettings.ResolveQualityOverride(Instance.RefractionDispersion,                   QualitySettings.UseRefractionDispersion);
            var volumetricLighting                  = WaterQualityLevelSettings.ResolveQualityOverride(Instance.VolumetricLighting,                     QualitySettings.UseVolumetricLight);

            var useUnderwaterEffect                 = WaterQualityLevelSettings.ResolveQualityOverride(Instance.UnderwaterEffect,                       QualitySettings.UseUnderwaterEffect);

            var useUnderwaterReflection = useUnderwaterEffect
                                       && Instance.UnderwaterReflectionMode == WaterQualityLevelSettings.UnderwaterReflectionModeEnum.PhysicalAproximatedReflection
                                       && (IsCameraPartialUnderwater)
                                       && !cam.orthographic;


            var useRefractionIOR            = Instance.RefractionMode == WaterQualityLevelSettings.RefractionModeEnum.PhysicalAproximationIOR;
            var visibleZones                = KWS_TileZoneManager.VisibleDynamicWavesZones.Count;
          
            var useColoredDynamicWavesZones = QualitySettings.UseDynamicWaves && visibleZones > 0 && KWS_TileZoneManager.IsAnyZoneUseRequireColorRendering;
            var useDynamicWavesZones        = QualitySettings.UseDynamicWaves && visibleZones > 0 && !KWS_TileZoneManager.IsAnyZoneUseRequireColorRendering;
           
           
            var useVolumeDirCaustic  = useCausticEffect && Instance.VolumetricLightCausticMode == WaterQualityLevelSettings.QualityOverrideEnum.UseQualitySettings 
                                                        && QualitySettings.VolumetricLightCausticMode == WaterQualityLevelSettings.VolumetricLightCausticModeEnum.DirLightOnly;
            var useVolumeFullCaustic = useCausticEffect && Instance.VolumetricLightCausticMode        == WaterQualityLevelSettings.QualityOverrideEnum.UseQualitySettings 
                                                        && QualitySettings.VolumetricLightCausticMode == WaterQualityLevelSettings.VolumetricLightCausticModeEnum.AllLights;
         
            var useClipZones = KWS_TileZoneManager.VisibleClipMeshZones.Count > 0;
            
            
            Shader.SetKeyword(WaterKeywords.GlobalKeyword_KWS_STEREO_INSTANCING_ON, KWS_CoreUtils.SinglePassStereoEnabled);


            Shader.SetKeyword(WaterKeywords.GlobalKeyword_KWS_SSR_REFLECTION,        useScreenSpaceReflection);
            Shader.SetKeyword(WaterKeywords.GlobalKeyword_KWS_USE_PLANAR_REFLECTION, usePlanarReflection);
            Shader.SetKeyword(WaterKeywords.GlobalKeyword_KWS_REFLECT_SUN,           Instance.ReflectSun);

            Shader.SetKeyword(WaterKeywords.GlobalKeyword_KWS_USE_REFRACTION_IOR,        useRefractionIOR);
            Shader.SetKeyword(WaterKeywords.GlobalKeyword_KWS_USE_REFRACTION_DISPERSION, useRefractionDispersion);

            Shader.SetKeyword(WaterKeywords.GlobalKeyword_KWS_USE_VOLUMETRIC_LIGHT,       volumetricLighting);
            Shader.SetKeyword(WaterKeywords.GlobalKeyword_KWS_USE_VOLUMETRIC_DIR_CAUSTIC, useVolumeDirCaustic);
            Shader.SetKeyword(WaterKeywords.GlobalKeyword_KWS_USE_VOLUMETRIC_FULL_CAUSTIC, useVolumeFullCaustic);
            
            Shader.SetKeyword(WaterKeywords.GlobalKeyword_KWS_USE_COLORED_DYNAMIC_WAVES,         useColoredDynamicWavesZones);
            Shader.SetKeyword(WaterKeywords.GlobalKeyword_KWS_USE_DYNAMIC_WAVES,                 useDynamicWavesZones);

            Shader.SetKeyword(WaterKeywords.GlobalKeyword_KWS_USE_LOCAL_WATER_ZONES, KWS_TileZoneManager.VisibleLocalWaterZones.Count > 0);

            Shader.SetKeyword(WaterKeywords.GlobalKeyword_KWS_USE_CAUSTIC,            useCausticEffect);
            Shader.SetKeyword(WaterKeywords.GlobalKeyword_KWS_USE_CAUSTIC_FILTERING,  QualitySettings.UseOceanCausticHighQualityFiltering);
            Shader.SetKeyword(WaterKeywords.GlobalKeyword_KWS_USE_CAUSTIC_DISPERSION, QualitySettings.UseOceanCausticDispersion);

            Shader.SetKeyword(WaterKeywords.GlobalKeyword_KWS_USE_UNDERWATER_REFLECTION, useUnderwaterReflection);
            Shader.SetKeyword(WaterKeywords.GlobalKeyword_KWS_USE_HALF_LINE_TENSION,     Instance.UseUnderwaterHalfLineTensionEffect);
            Shader.SetKeyword(WaterKeywords.GlobalKeyword_KWS_USE_CLIP_MASKING,          useClipZones);
            
        }

        static void SetQualitySettingsShaderParams()
        {                
            Shader.SetGlobalFloat(DynamicWaterParams.KWS_WaterLevel,       Instance.WaterLevel);
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_RefractionSimpleStrength,     Instance.RefractionSimpleStrength);
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_RefractionDispersionStrength, Instance.RefractionDispersionStrength * KWS_Settings.Water.MaxRefractionDispersion);

            Shader.SetGlobalFloat("KWS_GlobalTimeScale",  GlobalTimeScale);
            
            Shader.SetGlobalFloat(ConstantWaterParams.KW_GlobalTimeScale,                                   GlobalTimeScale);
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_Transparent,                                      Instance.Transparent);
            Shader.SetGlobalFloat(ConstantWaterParams.KW_WaterFarDistance,                                  QualitySettings.MeshDetailingFarDistance * WaterSystem._globalWaterScaleFactor);
            Shader.SetGlobalFloat(ConstantWaterParams.KW_ReflectionClipOffset,                              QualitySettings.ReflectionClipPlaneOffset);
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_SunCloudiness,                                    Instance.ReflectedSunCloudinessStrength);
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_SunStrength,                                      Instance.ReflectedSunStrength);
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_UnderwaterHalfLineTensionScale,                   Instance.UnderwaterHalfLineTensionScale);
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_ScreenSpaceBordersStretching,                     QualitySettings.ScreenSpaceBordersStretching);
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_AnisoReflectionsScale,                            Instance.AnisotropicReflectionsScale);
            Shader.SetGlobalFloat(VolumetricLightConstantsID.KWS_VolumetricLightTemporalAccumulationFactor, Instance.VolumetricLightTemporalReprojectionAccumulationFactor);
            Shader.SetGlobalFloat(CausticID.KWS_CausticStrength,                                            Instance.CausticStrength);
            
            Shader.SetGlobalInteger(ConstantWaterParams.KWS_OverrideSkyColor,        Instance.OverrideSkyColor ? 1 : 0);
            Shader.SetGlobalInteger(ConstantWaterParams.KWS_UseRefractionIOR,        Instance.RefractionMode == WaterQualityLevelSettings.RefractionModeEnum.PhysicalAproximationIOR ? 1 : 0);
            Shader.SetGlobalInteger(ConstantWaterParams.KWS_UseRefractionDispersion, QualitySettings.UseRefractionDispersion ? 1 : 0);
            
            Shader.SetGlobalInteger(ConstantWaterParams.KWS_UseCausticDispersion,   QualitySettings.UseOceanCausticDispersion ? 1 : 0);
            Shader.SetGlobalInteger(ConstantWaterParams.KWS_DisableCausticInShadow, Instance.DisableCausticsInShadow ? 1 : 0);
            
            Shader.SetGlobalInteger(ConstantWaterParams.UseScreenSpaceReflectionSky, QualitySettings.UseScreenSpaceReflectionSky ? 1 : 0);
            Shader.SetGlobalInteger(VolumetricLightConstantsID.KWS_RayMarchSteps,    QualitySettings.VolumetricLightIteration);
            
            Shader.SetGlobalInteger("KWS_IsAnyZoneUseFoamParticles",        KWS_TileZoneManager.IsAnyZoneUseFoamParticles ? 1 : 0);
            
            Shader.SetGlobalVector(ConstantWaterParams.KWS_CustomSkyColor, Instance.CustomSkyColor);
            Shader.SetGlobalVector(ConstantWaterParams.KWS_WaterColor,     Instance.WaterColor);
            Shader.SetGlobalVector(ConstantWaterParams.KWS_TurbidityColor, Instance.TurbidityColor);
      
            
            
            var useWetEffect = WaterQualityLevelSettings.ResolveQualityOverride(Instance.WetEffect, QualitySettings.UseWetEffect);
            if (useWetEffect)
            {
                Shader.SetGlobalFloat(ConstantWaterParams.KWS_WetStrength, Instance.WetStrength);
                Shader.SetGlobalFloat(ConstantWaterParams.KWS_WetLevel,    Instance.WetnessHeightAboveWater);
            }


        }

        static void SetSettingsConstantShaderParams()
        {
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_SunMaxValue,                  KWS_Settings.Reflection.MaxSunStrength);
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_AnisoWindCurvePower,          KWS_Settings.Reflection.AnisotropicReflectionsCurvePower);
            Shader.SetGlobalFloat(ConstantWaterParams.KWS_AbsorbtionOverrideMultiplier, KWS_Settings.VolumetricLighting.AbsorbtionOverrideMultiplier);

            Shader.SetGlobalFloat("KWS_GlobalWaterScaleFactor", WaterSystem._globalWaterScaleFactor);
   
            if (KWS_Ocean.Instance != null)
            {
                var domainSize              = KWS_Settings.FFT.FftDomainSizes       * _globalWaterScaleFactor;
                var domainSizeScaled        = domainSize                            * KWS_Ocean.Instance.WavesAreaScale  * _globalWaterScaleFactor;
                var domainVisibleArea       = KWS_Settings.FFT.FftDomainVisibleArea * _globalWaterScaleFactor;
                var domainVisibleAreaScaled = domainVisibleArea                     * KWS_Ocean.Instance.WavesAreaScale  * _globalWaterScaleFactor;

                float maxVisibleDistance = QualitySettings.MeshDetailingFarDistance * WaterSystem._globalWaterScaleFactor * 0.5f;
                domainVisibleArea = Vector4.Min(domainVisibleArea, maxVisibleDistance * Vector4.one);

                Shader.SetGlobalVector(ConstantWaterParams.KWS_WavesDomainSizes,          domainSize);
                Shader.SetGlobalVector(ConstantWaterParams.KWS_WavesDomainSizesInv,       domainSize.Inverse());
                Shader.SetGlobalVector(ConstantWaterParams.KWS_WavesDomainScaledSizes,    domainSizeScaled);
                Shader.SetGlobalVector(ConstantWaterParams.KWS_WavesDomainScaledSizesInv, domainSizeScaled.Inverse());

                Shader.SetGlobalVector(ConstantWaterParams.KWS_WavesDomainVisibleArea,          domainVisibleArea);
                Shader.SetGlobalVector(ConstantWaterParams.KWS_WavesDomainVisibleAreaInv,       domainVisibleArea.Inverse());
                Shader.SetGlobalVector(ConstantWaterParams.KWS_WavesDomainScaledVisibleArea,    domainVisibleAreaScaled);
                Shader.SetGlobalVector(ConstantWaterParams.KWS_WavesDomainScaledVisibleAreaInv, domainVisibleAreaScaled.Inverse());

                Shader.SetGlobalVector(ConstantWaterParams.KWS_WavesDomainHeightScales, KWS_Settings.FFT.FftDomainHeightScales);
            }

        }

        #endregion

    }

}