//#define DEBUG_SIMULATION

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static KWS.KWS_DynamicWavesSimulationZone;

#if KWS_URP
using UnityEngine.Rendering.Universal;
#endif

#if KWS_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace KWS
{

    internal class DynamicWavesPass : WaterPass
    {
       
        private static List<KWS_DynamicWavesSimulationEffector>                       _interactScriptsInArea       = new();
        private static List<KWS_DynamicWavesSimulationEffector.DynamicWaveDataStruct> _visibleInteractionSpheres   = new();
        private static List<KWS_DynamicWavesSimulationEffector.DynamicWaveDataStruct> _visibleInteractionCubes     = new();
        private static List<KWS_DynamicWavesSimulationEffector.DynamicWaveDataStruct> _visibleInteractionTriangles = new();

        private static Material      _dynamicWavesMaterial;
        private static ComputeBuffer _computeBufferDynamicWavesMask;
        
        private static ComputeBuffer _defaultComputeBufferFoam;
        private static ComputeBuffer _defaultComputeBufferSplash;


        private static Dictionary<SplashParticlesMaxLimitEnum, float> _normalizedBudget = new()
        {
            { SplashParticlesMaxLimitEnum._50k, 1 },
            { SplashParticlesMaxLimitEnum._25k, 0.75f },
            { SplashParticlesMaxLimitEnum._15k, 0.5f },
            { SplashParticlesMaxLimitEnum._5k, 0 }
        };
        
        private static readonly int KwsDynamicWavesFlowSpeedMultiplier      = Shader.PropertyToID("KWS_DynamicWavesFlowSpeedMultiplier");
        private static readonly int KwsDynamicWavesUseWaterIntersection     = Shader.PropertyToID("KWS_DynamicWavesUseWaterIntersection");
        private static readonly int KwsDynamicWavesZoneInteractionType      = Shader.PropertyToID("KWS_DynamicWavesZoneInteractionType");
        private static readonly int KwsCurrentFrame                         = Shader.PropertyToID("KWS_CurrentFrame");
        private static readonly int KwsDynamicWavesZonePosition             = Shader.PropertyToID("KWS_DynamicWavesZonePosition");
        private static readonly int KwsDynamicWavesZoneSize                 = Shader.PropertyToID("KWS_DynamicWavesZoneSize");
        private static readonly int KwsDynamicWavesZoneRotationMatrix       = Shader.PropertyToID("KWS_DynamicWavesZoneRotationMatrix");

        private static readonly int KwsDeltaTime                            = Shader.PropertyToID("KWS_deltaTime");
        private static readonly int KwsDistancePerPixel                     = Shader.PropertyToID("KWS_DistancePerPixel");
        private static readonly int MaxParticles                            = Shader.PropertyToID("maxParticles");
        private static readonly int KwsWorldSpaceCameraPos                  = Shader.PropertyToID("KWS_WorldSpaceCameraPos");
        private static readonly int KwsCameraForward                        = Shader.PropertyToID("KWS_CameraForward");
       
        private static readonly int KwsSplashParticlesBudgetNormalized      = Shader.PropertyToID("KWS_SplashParticlesBudgetNormalized");
        private static readonly int KwsCurrentScreenSize                    = Shader.PropertyToID("KWS_CurrentScreenSize");
        private static readonly int KwsUsePhytoplanktonEmission             = Shader.PropertyToID("KWS_UsePhytoplanktonEmission");
        private static readonly int KwsFoamParticlesBuffer                  = Shader.PropertyToID("KWS_FoamParticlesBuffer");
        private static readonly int KwsParticlesFoamInterpolationTime       = Shader.PropertyToID("KWS_ParticlesFoamInterpolationTime");
        private static readonly int KwsSplashParticlesBuffer                = Shader.PropertyToID("KWS_SplashParticlesBuffer");
        private static readonly int KwsSplashParticlesScale                 = Shader.PropertyToID("KWS_SplashParticlesScale");
        private static readonly int KwsParticlesSplashInterpolationTime     = Shader.PropertyToID("KWS_ParticlesSplashInterpolationTime");
        private static readonly int KwsSplashParticlesAlphaMultiplier       = Shader.PropertyToID("KWS_SplashParticlesAlphaMultiplier");
        private static readonly int KwsCurrentAdvectedUVTarget              = Shader.PropertyToID("KWS_CurrentAdvectedUVTarget");

        private static readonly int KWS_DynamicWavesMaskBuffer                = Shader.PropertyToID("KWS_DynamicWavesMaskBuffer");
        private static readonly int KwsKwsPerlinNoise                         = Shader.PropertyToID("KWS_PerlinNoise");
        private static readonly int KwsKwsDynamicWavesZoneFlowSpeedMultiplier = Shader.PropertyToID("KWS_DynamicWavesZoneFlowSpeedMultiplier");
        private static readonly int KwsKwsDynamicWavesBakingMode              = Shader.PropertyToID("KWS_DynamicWavesBakingMode");


        private CommandBuffer _cmd;
        private CommandBuffer _cmdMap;

        private Mesh       _cubeMesh;
        private Mesh       _sphereMesh;
        private Mesh       _triangleMesh;
        private Mesh       _quadMesh;

        private static          RTHandle _dynamicWavesMap;
        private static          RTHandle _dynamicWavesAdditionalDataMap;
        private static          RTHandle _dynamicWavesNormalAndWetMap;
        private static          RTHandle _dynamicWavesAdvectedUVMap;
        private static          RTHandle _dynamicWavesColorMap;
        
            
        ComputeBuffer[]        _screenSpaceFoamBuffers     = new ComputeBuffer[ScreenSpaceFoamBufferSizes.Length];
        RTHandle               _screenSpaceFoamTexture;
        Material _screenSpaceFoamMaterial;
        
        static Vector4[]    ScreenSpaceFoamBufferSizes = 
        {
            new (2048, 1024, 0, 0),
            new (1024,  512,  0, 0),
            new (512,  256,  0, 0),
        };
        
        private const string ScreenSpaceFoamParticlesShaderName   = "Hidden/KriptoFX/KWS/KWS_DynamicWavesFoamParticlesShading";
        private const string FoamParticlesShaderName   = "Hidden/KriptoFX/KWS/KWS_DynamicWavesFoamParticles";
        private const string SplashParticlesShaderName = "Hidden/KriptoFX/KWS/KWS_DynamicWavesSplashParticles";

        private const string FoamComputeShaderKeyword   = "KWS_FOAM_MODE";
        private const string SplashComputeShaderKeyword = "KWS_SPLASH_MODE";


        private static readonly int KwsOceanWavesInfluenceStrength = Shader.PropertyToID("KWS_OceanWavesInfluenceStrength");
        private static readonly int KwsDynamicWavesLodIndex        = Shader.PropertyToID("KWS_DynamicWavesLodIndex");

        private static readonly int KwsFoamDisappearSpeed          = Shader.PropertyToID("KWS_FoamDisappearSpeed");
        private static readonly int KwsFoamEmissionRate            = Shader.PropertyToID("KWS_FoamEmissionRate");
        private static readonly int KwsSplashEmissionRate          = Shader.PropertyToID("KWS_SplashEmissionRate");


        private int ID_KWS_FoamParticlesBuffer      = Shader.PropertyToID("KWS_FoamParticlesBuffer");
        private int ID_KWS_FoamParticlesBuffer1 = Shader.PropertyToID("KWS_FoamParticlesBuffer1");
        private int ID_KWS_FoamParticlesBuffer2 = Shader.PropertyToID("KWS_FoamParticlesBuffer2");

        private int ID_KWS_SplashParticlesBuffer  = Shader.PropertyToID("KWS_SplashParticlesBuffer");
        private int ID_KWS_SplashParticlesBuffer1 = Shader.PropertyToID("KWS_SplashParticlesBuffer1");
        private int ID_KWS_SplashParticlesBuffer2 = Shader.PropertyToID("KWS_SplashParticlesBuffer2");


        public DynamicWavesPass()
        {
            _dynamicWavesMaterial                                =  KWS_CoreUtils.CreateMaterial(KWS_ShaderConstants.ShaderNames.DynamicWavesShaderName);
            SetDefaultBuffers();
            WaterSystem.OnOriginShifted += OnOriginShifted;
        }

        
       
        internal override string PassName => "Water.DynamicWavesPass";

        public override void Release()
        {
            _computeBufferDynamicWavesMask?.Release();
            _computeBufferDynamicWavesMask = null;
            
            _defaultComputeBufferFoam?.Release();
            _defaultComputeBufferFoam = null;
            
            _defaultComputeBufferSplash?.Release();
            _defaultComputeBufferSplash = null;

            KW_Extensions.SafeDestroy(_dynamicWavesMaterial, _cubeMesh, _sphereMesh, _triangleMesh, _quadMesh, _screenSpaceFoamMaterial);
            _dynamicWavesMaterial = null;
            _screenSpaceFoamMaterial = null;

            ReleaseMapTextures();
            
            _dynamicWavesAdvectedUVMap?.Release();
            _dynamicWavesAdvectedUVMap = null;
            
            _dynamicWavesColorMap?.Release();
            _dynamicWavesColorMap = null;

            
            _screenSpaceFoamTexture?.Release();
            _screenSpaceFoamTexture = null;

            for (int i = 0; i < _screenSpaceFoamBuffers.Length; i++)
            {
                _screenSpaceFoamBuffers[i]?.Release();
                _screenSpaceFoamBuffers[i] = null;
            }

            
            WaterSystem.OnOriginShifted -= OnOriginShifted;
            
            this.WaterLog(string.Empty, KW_Extensions.WaterLogMessageType.Release);
        }
        
     

        internal static void SetDefaultBuffers()
        {
            KWS_CoreUtils.SetFallbackBuffer<KWS_DynamicWavesSimulationEffector.DynamicWaveDataStruct>(ref _computeBufferDynamicWavesMask, KWS_DynamicWavesMaskBuffer);
            KWS_CoreUtils.SetFallbackBuffer<KWS_DynamicWavesHelpers.FoamParticle>(ref _defaultComputeBufferFoam, KwsFoamParticlesBuffer);
            KWS_CoreUtils.SetFallbackBuffer<KWS_DynamicWavesHelpers.SplashParticle>(ref _defaultComputeBufferSplash, KwsSplashParticlesBuffer);
            
        }

        void InitializeScreenSpaceFoam()
        {
            for (int i = 0; i < _screenSpaceFoamBuffers.Length; i++)
            {
                _screenSpaceFoamBuffers[i] = new ComputeBuffer((int)(ScreenSpaceFoamBufferSizes[i].x * ScreenSpaceFoamBufferSizes[i].y), sizeof(uint), ComputeBufferType.Structured);
            }

            _screenSpaceFoamTexture       = KWS_CoreUtils.RTHandleAllocVR(1920, 1080, DepthBits.None, KWS_CoreUtils.GetSafeR8G8B8A8_UNorm(), name: "_screenSpaceFoamTexture");
            if (_screenSpaceFoamMaterial == null) _screenSpaceFoamMaterial = KWS_CoreUtils.CreateMaterial(ScreenSpaceFoamParticlesShaderName, useWaterStencilMask: true);
        }

        void InitializeMapTexturesIfNeeded()
        {
            var slices = KWS_Settings.DynamicWaves.MaxDynamicWavesMapLods;
            int res    = (int)KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings.DynamicWavesMapResolution;

            if (_dynamicWavesMap != null && _dynamicWavesMap.rt != null && _dynamicWavesMap.rt.width == res) return;
            if (_dynamicWavesMap != null && _dynamicWavesMap.rt.width != res) ReleaseMapTextures();
                
            _dynamicWavesMap               = KWS_CoreUtils.RTHandles.Alloc(res, res, name: "_dynamicWavesMap",               colorFormat: GraphicsFormat.R16G16B16A16_SFloat,    slices: slices, dimension: TextureDimension.Tex2DArray);
            _dynamicWavesAdditionalDataMap = KWS_CoreUtils.RTHandles.Alloc(res, res, name: "_dynamicWavesAdditionalDataMap", colorFormat: KWS_CoreUtils.GetSafeR8G8B8A8_UNorm(), slices: slices, dimension: TextureDimension.Tex2DArray);
            _dynamicWavesNormalAndWetMap   = KWS_CoreUtils.RTHandles.Alloc(res, res, name: "_dynamicWavesNormalMap",         colorFormat: KWS_CoreUtils.GetSafeR8G8B8A8_UNorm(),       slices: slices, dimension: TextureDimension.Tex2DArray);
            
            this.WaterLog(_dynamicWavesMap);
        }
        

        void ReleaseMapTextures()
        {
            _dynamicWavesMap?.Release();
            _dynamicWavesAdditionalDataMap?.Release();
            _dynamicWavesNormalAndWetMap?.Release();

            _dynamicWavesMap               = null;
            _dynamicWavesAdditionalDataMap = null;
            _dynamicWavesNormalAndWetMap   = null;
        }

        void InitializeAdvectedMapTexture()
        {
            var slices = _dynamicWavesMap.rt.volumeDepth;
            var res    = _dynamicWavesMap.rt.width;
            _dynamicWavesAdvectedUVMap = KWS_CoreUtils.RTHandles.Alloc(res, res, name: "_dynamicWavesAdvectedUVMap", colorFormat: GraphicsFormat.R16G16B16A16_UNorm, slices: slices, dimension: TextureDimension.Tex2DArray);
        }

        void InitializeColorMapTexture()
        {
            var slices = _dynamicWavesMap.rt.volumeDepth;
            var res    = _dynamicWavesMap.rt.width;
            _dynamicWavesColorMap = KWS_CoreUtils.RTHandles.Alloc(res, res, name: "_dynamicWavesColorMap", colorFormat: KWS_CoreUtils.GetSafeR8G8B8A8_UNorm(), slices: slices, dimension: TextureDimension.Tex2DArray);
        }


        public override void ExecutePerFrame(HashSet<Camera> cameras, CustomFixedUpdates fixedUpdates1)
        {
            
            var cam = KWS_CoreUtils.GetFixedUpdateCamera(cameras);
            if (cam == null) return;
            
            if (KWS_TileZoneManager.VisibleDynamicWavesZones.Count == 0) return;

         
            foreach (var iZone in KWS_TileZoneManager.VisibleDynamicWavesZones)
            {
                if (!iZone.IsZoneVisible) continue;

                var zone = (KWS_DynamicWavesSimulationZone)iZone;
                
                zone.UpdateFixedTimer();
            
                if (zone.UseFoamParticles) zone._simulationData.FoamParticlesData.UpdateInterpolationTime(zone.CurrentFixedUpdateFramesCount, zone.MaxSkippedFrames, zone.CurrentTimeScale);
                if (zone.UseSplashParticles) zone._simulationData.SplashParticlesData.UpdateInterpolationTime(zone.CurrentFixedUpdateFramesCount, zone.MaxSkippedFrames, zone.CurrentTimeScale);
                
            }
            
#if !DEBUG_SIMULATION
            if (_cmd == null) _cmd = new CommandBuffer { name = PassName };
            _cmd.Clear();

            UpdateSimulation(_cmd, cam, KWS_TileZoneManager.VisibleDynamicWavesZones);

            Graphics.ExecuteCommandBuffer(_cmd);
#endif
        }
        
        
        public override void ExecuteBeforeCameraRendering(Camera cam, ScriptableRenderContext context)
        {
            if (KWS_TileZoneManager.VisibleDynamicWavesZones.Count == 0) return;
            
#if DEBUG_SIMULATION
            if (_cmd == null) _cmd = new CommandBuffer() { name = PassName };
            _cmd.Clear();

            UpdateSimulation(_cmd, cam, 1, KWS_TileZoneManager.VisibleDynamicWavesZones);

            Graphics.ExecuteCommandBuffer(_cmd);
#endif

            ExecuteParticles(cam);
            UpdateSimulationMap(cam);
        }


        
        public override void ExecuteCommandBuffer(WaterPassContext waterContext)
        {
            
            //if (Application.isPlaying  && waterContext.cam.cameraType == CameraType.Game
            // || !Application.isPlaying && waterContext.cam.cameraType    == CameraType.SceneView)
            {

                if (KWS_TileZoneManager.VisibleDynamicWavesZones.Count == 0 || !KWS_TileZoneManager.IsAnyZoneUseFoamParticles) return;

                var firstZone = (KWS_DynamicWavesSimulationZone)KWS_TileZoneManager.VisibleDynamicWavesZones[0];
                ClearParticlesBuffers(waterContext, firstZone);
                
                
                var simZones = KWS_TileZoneManager.VisibleDynamicWavesZones;
                foreach (var iZone in simZones)
                {
                    var zone = (KWS_DynamicWavesSimulationZone)iZone;

                    if (!zone.IsZoneVisible || !zone.IsZoneInitialized || zone.ZoneType == SimulationZoneTypeMode.BakedSimulation) continue;
                    
                    RenderParticlesToBuffer(waterContext, zone);
                }

                RenderParticlesToCameraTarget(waterContext);

            }

            
           // ExecuteParticles(waterContext.cam, waterContext.cmd); //todo add shadows
        }
        

        private void UpdateSimulation(CommandBuffer cmd, Camera cam, List<KWS_TileZoneManager.IWaterZone> simZones)
        {
            var                         isBakeModeActive = KWS_DynamicWavesSimulationZone.IsAnyZoneInBakeMode;
            WaterSystem.GlobalTimeScale = isBakeModeActive ? 20 : 1;
            
            foreach (var iZone in simZones)
            {
                var zone = (KWS_DynamicWavesSimulationZone)iZone;
                if(zone.CurrentFixedUpdateFramesCount == 0) continue;
              
                if (zone.IsBaking)
                {
                    UpdateZone(cmd, cam, zone.CurrentFixedUpdateFramesCount, zone);
                }
                else
                {
                    if (zone.UpdateAndCheckRender(cam)) UpdateZone(cmd, cam, zone.CurrentFixedUpdateFramesCount + zone.MaxSkippedFrames, zone);
                }
               
            }
            
        }


        private void UpdateZone(CommandBuffer cmd, Camera cam, int frames, KWS_DynamicWavesSimulationZone zone)
        {
            var timeScale = WaterSystem.GlobalTimeScale;
            frames = (int)(frames * timeScale);
          
            var zoneOffset = Vector3.zero;
            if (zone.ZoneType == SimulationZoneTypeMode.MovableZone)
            {
                zoneOffset = GetMovableZoneOffset(zone);
            }

            if (zone.ZoneType != SimulationZoneTypeMode.BakedSimulation || zone.IsBaking || zone.SavedMesh == null)
            {
                if(zone.ReceiveWaterFlowFromOtherZones) DrawZonesIntersection(cmd, zone);

                UpdateZoneShaderParams(cmd, zone);
                UpdateSimulationShaderParams(cmd, zone);
                
                DrawEffectors(cmd, frames, zone);
                ExecuteDynamicWaves(cmd, frames, zoneOffset, zone);
            }

            
            if (zone.ZoneType != SimulationZoneTypeMode.BakedSimulation && zone.UseFoamParticles) ExecuteFoamParticles(cmd, cam, frames     + 0, zone);
            if (zone.ZoneType != SimulationZoneTypeMode.BakedSimulation && zone.UseSplashParticles) ExecuteSplashParticles(cmd, cam, frames + 0, zone);

#if KWS_DEBUG
            WaterSharedResources.DynamicWavesRT               = zone._simulationData.GetTarget;
            WaterSharedResources.DynamicWavesAdditionalDataRT = zone._simulationData.GetAdditionalTarget;
            WaterSharedResources.DynamicWavesMaskRT           = zone._simulationData.DynamicWavesMask;

#endif

        }

      
        
        void UpdateSimulationMap(Camera cam)
        {
            if (_cmdMap == null) _cmdMap = new CommandBuffer { name = "Water.DynamicWavesPassMap" };
            _cmdMap.Clear();
           
            if (_quadMesh == null) _quadMesh = KWS_CoreUtils.CreateQuadXZ();

            InitializeMapTexturesIfNeeded();
            
            
            var zones    = KWS_TileZoneManager.VisibleDynamicWavesZones;
            var lodSizes = KWS_Settings.DynamicWaves.DynamicWavesMapLodDistances;
            var camPos   = cam.transform.position;
            
            Shader.SetGlobalVector("KWS_DynamicWavesMapPos", camPos);
            Shader.SetGlobalFloatArray("KWS_DynamicWavesMapLodSizes",         lodSizes);
            Shader.SetGlobalFloatArray("KWS_DynamicWavesMapLodSizesInverted", KWS_Settings.DynamicWaves.DynamicWavesMapLodDistancesInverted);

            // if (!IsRequireSimulationMap(camPos)) return;

            KWS.CoreUtils.SetRenderTarget(_cmdMap, _dynamicWavesMap,               ClearFlag.All, new Color(-10, -10, 0, 0),   depthSlice: -1);
            KWS.CoreUtils.SetRenderTarget(_cmdMap, _dynamicWavesAdditionalDataMap, ClearFlag.All, Color.clear,                 depthSlice: -1);
            KWS.CoreUtils.SetRenderTarget(_cmdMap, _dynamicWavesNormalAndWetMap,   ClearFlag.All, new Color(0.5f, 0.5f, 0, 0), depthSlice: -1);

            if (KWS_TileZoneManager.IsAnyZoneUseAdvectedUV            && _dynamicWavesAdvectedUVMap == null) InitializeAdvectedMapTexture();
            if (KWS_TileZoneManager.IsAnyZoneUseRequireColorRendering && _dynamicWavesColorMap      == null) InitializeColorMapTexture();

            if (KWS_TileZoneManager.IsAnyZoneUseRequireColorRendering)
            {
                KWS.CoreUtils.SetRenderTarget(_cmdMap, _dynamicWavesColorMap, ClearFlag.All, Color.clear, depthSlice: -1);
            }

            if (KWS_TileZoneManager.IsAnyZoneUseAdvectedUV) KWS.CoreUtils.SetRenderTarget(_cmdMap, _dynamicWavesAdvectedUVMap, ClearFlag.All, Color.clear, depthSlice: -1);



            for (var lodIndex = 0; lodIndex < lodSizes.Length; lodIndex++)
            {
                var lodSize     = lodSizes[lodIndex];
                var prevLodSize = lodIndex > 0 ? lodSizes[lodIndex - 1] : 0f;
                KWS_CoreUtils.SetOrthoMatrix_VP(_cmdMap, new Vector3(lodSize, 10000, lodSize), camPos, Quaternion.identity);

                KWS.CoreUtils.SetRenderTarget(_cmdMap, _dynamicWavesMap,               depthSlice: lodIndex);
                KWS.CoreUtils.SetRenderTarget(_cmdMap, _dynamicWavesAdditionalDataMap, depthSlice: lodIndex);
                KWS.CoreUtils.SetRenderTarget(_cmdMap, _dynamicWavesNormalAndWetMap,   depthSlice: lodIndex);



                if (KWS_TileZoneManager.IsAnyZoneUseAdvectedUV)
                {

                    if (KWS_TileZoneManager.IsAnyZoneUseRequireColorRendering)
                    {
                        KWS.CoreUtils.SetRenderTarget(_cmdMap, KWS_CoreUtils.GetMrt(_dynamicWavesMap, _dynamicWavesAdditionalDataMap, _dynamicWavesNormalAndWetMap, _dynamicWavesAdvectedUVMap, _dynamicWavesColorMap),
                                                      _dynamicWavesMap, ClearFlag.None, Color.clear, depthSlice: lodIndex);
                    }
                    else
                    {
                        KWS.CoreUtils.SetRenderTarget(_cmdMap, KWS_CoreUtils.GetMrt(_dynamicWavesMap, _dynamicWavesAdditionalDataMap, _dynamicWavesNormalAndWetMap, _dynamicWavesAdvectedUVMap),
                                                      _dynamicWavesMap, ClearFlag.None, Color.clear, depthSlice: lodIndex);
                    }

                }
                else
                {
                    if (KWS_TileZoneManager.IsAnyZoneUseRequireColorRendering)
                    {
                        KWS.CoreUtils.SetRenderTarget(_cmdMap, KWS_CoreUtils.GetMrt(_dynamicWavesMap, _dynamicWavesAdditionalDataMap, _dynamicWavesNormalAndWetMap, _dynamicWavesColorMap), _dynamicWavesMap, ClearFlag.None, Color.clear, depthSlice: lodIndex);
                    }
                    else
                    {
                        KWS.CoreUtils.SetRenderTarget(_cmdMap, KWS_CoreUtils.GetMrt(_dynamicWavesMap, _dynamicWavesAdditionalDataMap, _dynamicWavesNormalAndWetMap), _dynamicWavesMap, ClearFlag.None, Color.clear, depthSlice: lodIndex);
                    }
                }

                foreach (var iZone in zones)
                {
                    var zone = (KWS_DynamicWavesSimulationZone)iZone;
                    if (!zone.IsZoneVisible) continue;
                    if (!zone.IsZoneInitialized) continue;
                    
                    #if !UNITY_EDITOR
                        if (!KW_Extensions.BoundsIntersectsLod(iZone.OrientedBounds, camPos, lodSize)) continue;
                    #else 
                        if(Application.isPlaying) if (!KW_Extensions.BoundsIntersectsLod(iZone.OrientedBounds, camPos, lodSize)) continue;
                    #endif
                    
                    
                   //  Debug.Log($"Rendereds {zone.name }    lodIndex: {lodIndex}   zonePos: {zone.Position} "
                   //            +$"  camPos: {camPos}   aabbMin {iZone.OrientedBounds.min}  aabbMax {iZone.OrientedBounds.max}"
                   //          );
                    
                    
                    UpdateSimulationShaderParams(_cmdMap, zone);
                    _cmdMap.SetGlobalInteger(KwsDynamicWavesLodIndex, lodIndex);
                    _cmdMap.SetKeyword("KWS_LOCAL_DYNAMIC_WAVES_USE_ADVECTED_UV", zone.FoamType == FoamTypeEnum.Advected);
                    _cmdMap.SetKeyword("KWS_LOCAL_DYNAMIC_WAVES_USE_COLOR",       zone.RequireColorRendering);
                    _cmdMap.SetKeyword("KWS_DYNAMIC_WAVES_USE_MOVABLE_ZONE",      zone.ZoneType == SimulationZoneTypeMode.MovableZone);

                    var matrixTRS = Matrix4x4.TRS(zone.Position, zone.Rotation, zone.Size);
                    _cmdMap.DrawMesh(_quadMesh, matrixTRS, _dynamicWavesMaterial, 0, 4);
                }
            }

            Graphics.ExecuteCommandBuffer(_cmdMap);

            Shader.SetGlobalTexture("KWS_DynamicWavesMap",             _dynamicWavesMap);
            Shader.SetGlobalTexture("KWS_DynamicWavesAdditionalMap",   _dynamicWavesAdditionalDataMap);
            Shader.SetGlobalTexture("KWS_DynamicWavesNormalAndWetMap", _dynamicWavesNormalAndWetMap);
            if (KWS_TileZoneManager.IsAnyZoneUseAdvectedUV) Shader.SetGlobalTexture("KWS_DynamicWavesAdvectedUVMap",       _dynamicWavesAdvectedUVMap);
            if (KWS_TileZoneManager.IsAnyZoneUseRequireColorRendering) Shader.SetGlobalTexture("KWS_DynamicWavesColorMap", _dynamicWavesColorMap);

        }

      

        public void DrawMeshInstancedProcedural(CommandBuffer cmd, Mesh mesh, List<KWS_DynamicWavesSimulationEffector.DynamicWaveDataStruct> objects)
        {
            MeshUtils.InitializePropertiesBuffer(cmd, objects, ref _computeBufferDynamicWavesMask, false);

            if (_computeBufferDynamicWavesMask != null && _computeBufferDynamicWavesMask.count > 0)
            {
                cmd.SetGlobalBuffer(KWS_DynamicWavesMaskBuffer, _computeBufferDynamicWavesMask);
                cmd.DrawMeshInstancedProcedural(mesh, 0, _dynamicWavesMaterial, 1, objects.Count);
            }
           
        }


        internal void DrawEffectors(CommandBuffer cmd, int frames, KWS_DynamicWavesSimulationZone zone)
        {
            var simulationData = zone._simulationData;

            var interactScripts = GetInteractScriptsInArea(zone.Position, zone.Size, zone.Rotation);
            KWS_CoreUtils.SetOrthoMatrix_VP(cmd, zone.Size, zone.Position, zone.transform.rotation);

            _visibleInteractionCubes.Clear();
            _visibleInteractionSpheres.Clear();
            _visibleInteractionTriangles.Clear();

            if (!_cubeMesh) _cubeMesh         = MeshUtils.CreateCubeMesh();
            if (!_sphereMesh) _sphereMesh     = MeshUtils.CreateSphereMesh(0.5f, 7, 4);
            if (!_triangleMesh) _triangleMesh = MeshUtils.CreateTriangle(1);

            var currentFrames                  = (zone.IsBaking) ? 1 : frames;

            var isAnyObjectRequireColorRendering = false;
            foreach (var instance in interactScripts)
            {
                instance.CustomUpdate(currentFrames);
                if (instance.UseSourceColor) isAnyObjectRequireColorRendering = true;
            }

            if(isAnyObjectRequireColorRendering && !zone.RequireColorRendering) zone.RequireColorRendering = true;
            
            if (zone.RequireColorRendering)
            {
                if (simulationData.DynamicWavesMaskColor == null || simulationData.DynamicWavesMaskColor.rt == null) simulationData.InitializeSimTexturesColor(cmd);

                CoreUtils.SetRenderTarget(cmd, simulationData.DynamicWavesMaskColor, ClearFlag.Color, Color.clear);

                CoreUtils.SetRenderTarget(cmd, simulationData.DynamicWavesMask,      ClearFlag.Color, new Color(0.5f, 0.5f, 0.5f, 0));
                CoreUtils.SetRenderTarget(cmd, simulationData.DynamicWavesMaskDepth, ClearFlag.Depth, new Color(0.5f, 0.5f, 0.5f, 0));

                CoreUtils.SetRenderTarget(cmd, KWS_CoreUtils.GetMrt(simulationData.DynamicWavesMask, simulationData.DynamicWavesMaskColor), simulationData.DynamicWavesMaskDepth);
            }
            else
            {
                CoreUtils.SetRenderTarget(cmd, simulationData.DynamicWavesMask, simulationData.DynamicWavesMaskDepth, ClearFlag.All, new Color(0.5f, 0.5f, 0.5f, 0));
            }


            foreach (var instance in interactScripts)
            {
                if (instance.InteractionType == KWS_DynamicWavesSimulationEffector.InteractionTypeEnum.ObstacleObject)
                {
                    if (!instance.CurrentMesh) continue;
                    if (!instance.IsObstacleBoundsIntersectWater) continue;
                    
                    cmd.SetGlobalFloat(KWS_ShaderConstants.DynamicWaves.KWS_DynamicWavesWaterSurfaceHeight, instance.DynamicWaveData.WaterHeight);
                    cmd.SetGlobalFloat(KWS_ShaderConstants.DynamicWaves.KWS_DynamicWavesForce,              instance.DynamicWaveData.Force);
                    cmd.SetGlobalVector(KWS_ShaderConstants.DynamicWaves.KWS_DynamicWavesForceDirection,     instance.DynamicWaveData.ForceDirection);
                    cmd.SetGlobalFloat("KWS_ObstacleIntersectionAlphaFade",              instance._obstacleIntersectionAlphaFade);
                    
                    cmd.SetGlobalInteger(KwsDynamicWavesUseWaterIntersection, (int)instance.DynamicWaveData.UseWaterIntersection);
                    cmd.SetGlobalInteger(KwsDynamicWavesZoneInteractionType,  (int)instance.DynamicWaveData.ZoneInteractionType);
                    cmd.DrawMesh(instance.CurrentMesh, instance.DynamicWaveData.MatrixTRS, _dynamicWavesMaterial, 0, 0);
                    
                }
                else
                {
                    switch (instance.ForceType)
                    {
                        case KWS_DynamicWavesSimulationEffector.ForceTypeEnum.Sphere:
                            _visibleInteractionSpheres.Add(instance.DynamicWaveData);
                            break;
                        case KWS_DynamicWavesSimulationEffector.ForceTypeEnum.Box:  
                            _visibleInteractionCubes.Add(instance.DynamicWaveData);
                            break;
                        case KWS_DynamicWavesSimulationEffector.ForceTypeEnum.Triangle: 
                            _visibleInteractionTriangles.Add(instance.DynamicWaveData);
                            break;
                    }
                }
            }
             
            if (_visibleInteractionSpheres.Count   > 0) DrawMeshInstancedProcedural(cmd, _sphereMesh,   _visibleInteractionSpheres);
            if (_visibleInteractionCubes.Count     > 0) DrawMeshInstancedProcedural(cmd, _cubeMesh,     _visibleInteractionCubes);
            if (_visibleInteractionTriangles.Count > 0) DrawMeshInstancedProcedural(cmd, _triangleMesh, _visibleInteractionTriangles);
        }
        
        internal void DrawZonesIntersection(CommandBuffer cmd, KWS_DynamicWavesSimulationZone zone)
        {
            var simulationData = zone._simulationData;

            if (simulationData.DynamicWavesIntersections == null) simulationData.InitializeIntersectionTextures();
            if (_quadMesh                                == null) _quadMesh = KWS_CoreUtils.CreateQuadXZ();
            
            KWS_CoreUtils.SetOrthoMatrix_VP(cmd, zone.Size, zone.Position, zone.transform.rotation);
            KWS.CoreUtils.SetRenderTarget(cmd, simulationData.DynamicWavesIntersections, ClearFlag.Color, Color.clear);
            
            foreach (var intersectedZone in zone._intersectedZones)
            {
                cmd.SetGlobalVector(KwsDynamicWavesZonePosition,       intersectedZone.Position);
                cmd.SetGlobalVector(KwsDynamicWavesZoneSize,           intersectedZone.Size);
                cmd.SetGlobalVector(KwsDynamicWavesZoneRotationMatrix, intersectedZone.RotationMatrix);
                cmd.SetGlobalTexture(KWS_ShaderConstants.DynamicWaves.KWS_DynamicWaves,                 intersectedZone._simulationData.GetTarget);
                
                var matrixTRS = Matrix4x4.TRS(intersectedZone.Position, intersectedZone.Rotation, intersectedZone.Size);
                cmd.DrawMesh(_quadMesh, matrixTRS, _dynamicWavesMaterial, 0, 6);
            }
            
        }

        private Vector3 GetMovableZoneOffset(KWS_DynamicWavesSimulationZone zone)
        {
            zone.UpdateMovableZoneCache();
            
            var areaSize = zone.Size;
            var offset   = Vector3.zero;

            offset                            =  zone.Position - zone._lastRenderedDynamicPosition;
            
            offset.x                          /= areaSize.x;
            offset.z                          /= areaSize.z;
            zone._lastRenderedDynamicPosition =  zone.Position;
            
            offset = Quaternion.Inverse(zone.Rotation) * offset;
            return offset;
        }


        private void ExecuteDynamicWaves(CommandBuffer cmd, int fpsFrames, Vector3 worldOffsetFromTheLastFrame, KWS_DynamicWavesSimulationZone zone)
        {
            var data = zone._simulationData;

            if (zone.FoamType == FoamTypeEnum.Advected && (data.DynamicWavesAdvectedUV1 == null || !data.DynamicWavesAdvectedUV1.rt)) data.InitializeAdvectedUvTextures();
            
            var currentOffset = worldOffsetFromTheLastFrame;

            for (var i = 0; i < fpsFrames; i++)
            {
                cmd.SetGlobalInteger(KwsCurrentFrame, data.CurrentFrame);
                
                cmd.SetGlobalVector(KWS_ShaderConstants.DynamicWaves.KW_AreaOffset, currentOffset);
                cmd.SetGlobalTexture(KWS_ShaderConstants.DynamicWaves.KWS_CurrentTarget,           data.DynamicWaves2);
                cmd.SetGlobalTexture(KWS_ShaderConstants.DynamicWaves.KWS_CurrentAdditionalTarget, data.GetAdditionalTarget);
                if (data.DynamicWavesMaskColor != null) cmd.SetGlobalTexture(KWS_ShaderConstants.DynamicWaves.KWS_CurrentColorTarget, data.GetColorTarget);

                if (data.DynamicWavesColorData1 != null)
                    CoreUtils.SetRenderTarget(cmd, KWS_CoreUtils.GetMrt(data.DynamicWaves1, data.GetAdditionalTargetNext, data.GetColorTargetNext), data.DynamicWaves1);
                else
                    CoreUtils.SetRenderTarget(cmd, KWS_CoreUtils.GetMrt(data.DynamicWaves1, data.GetAdditionalTargetNext), data.DynamicWaves1);
                cmd.BlitTriangle(_dynamicWavesMaterial, 2);

                currentOffset = Vector4.zero;

                cmd.SetGlobalVector(KWS_ShaderConstants.DynamicWaves.KW_AreaOffset, currentOffset);
              
                cmd.SetGlobalTexture(KWS_ShaderConstants.DynamicWaves.KWS_CurrentTarget, data.DynamicWaves1);

                if (zone.FoamType == FoamTypeEnum.Advected)
                {
                    cmd.SetGlobalTexture(KwsCurrentAdvectedUVTarget, data.GetAdvectedUVTarget);
                    CoreUtils.SetRenderTarget(cmd, KWS_CoreUtils.GetMrt(data.DynamicWaves2, data.DynamicWavesNormals, data.GetAdvectedUVTargetNext), data.DynamicWaves2);
                }
                else
                {
                    CoreUtils.SetRenderTarget(cmd, KWS_CoreUtils.GetMrt(data.DynamicWaves2, data.DynamicWavesNormals), data.DynamicWaves2);
                }
           
                cmd.BlitTriangle(_dynamicWavesMaterial, 3);

                data.SwapSimulationBuffers();
            }
        }   
        

        internal static void UpdateComputeShaderMapTextures(CommandBuffer cmd, ComputeShader cs, int kernelIndex)
        {
            cmd.SetComputeTextureParam(cs, kernelIndex, "KWS_DynamicWavesMap",             _dynamicWavesMap.GetSafeArrayTexture());
            cmd.SetComputeTextureParam(cs, kernelIndex, "KWS_DynamicWavesAdditionalMap",   _dynamicWavesAdditionalDataMap.GetSafeArrayTexture());
            cmd.SetComputeTextureParam(cs, kernelIndex, "KWS_DynamicWavesNormalAndWetMap", _dynamicWavesNormalAndWetMap.GetSafeArrayTexture());
            cmd.SetComputeTextureParam(cs, kernelIndex, "KWS_DynamicWavesAdvectedUVMap",   _dynamicWavesAdvectedUVMap.GetSafeArrayTexture());
            cmd.SetComputeTextureParam(cs, kernelIndex, "KWS_DynamicWavesColorMap",   _dynamicWavesAdvectedUVMap.GetSafeArrayTexture());
        }

        private static void UpdateZoneShaderParams(CommandBuffer cmd, KWS_DynamicWavesSimulationZone zone)
        {
            var isBakeMode = zone.IsBaking || (zone.ZoneType == SimulationZoneTypeMode.BakedSimulation && zone.SavedMesh == null);
            
            cmd.SetKeyword("KWS_DYNAMIC_WAVES_USE_MOVABLE_ZONE",      zone.ZoneType == SimulationZoneTypeMode.MovableZone);
            cmd.SetKeyword("KWS_LOCAL_DYNAMIC_WAVES_USE_COLOR",       zone.RequireColorRendering);
            cmd.SetKeyword("KWS_LOCAL_DYNAMIC_WAVES_USE_ADVECTED_UV", zone.FoamType == FoamTypeEnum.Advected);
            cmd.SetKeyword("KWS_LOCAL_DYNAMIC_WAVES_USE_INTERSECTION", zone.ReceiveWaterFlowFromOtherZones);

            cmd.SetGlobalFloat(KwsOceanWavesInfluenceStrength, zone.OceanWavesInfluenceStrength);
            cmd.SetGlobalFloat("KWS_ZoneShorelineDistance",    zone.ShorelineDistance);
            
            cmd.SetGlobalFloat("KWS_TurbulenceFoamStrength", zone.TurbulenceFoamStrength);
            cmd.SetGlobalFloat("KWS_WaveCrestFoamStrength",  zone.WaveCrestFoamStrength);
            cmd.SetGlobalFloat("KWS_ShorelineFoamStrength",  zone.ShorelineFoamStrength);
            cmd.SetGlobalFloat(KwsFoamDisappearSpeed,        zone.FoamDisappearSpeed);
            
            cmd.SetGlobalFloat(KwsFoamEmissionRate,              zone.FoamEmissionRate);
            cmd.SetGlobalFloat(KwsSplashEmissionRate,            zone.SplashEmissionRate);
            
            cmd.SetGlobalFloat("KWS_MaxFoamRenderingDistance",   zone.MaxFoamRenderingDistance);
            cmd.SetGlobalFloat("KWS_MaxSplashRenderingDistance", zone.MaxSplashRenderingDistance);
            
            cmd.SetGlobalVector("KWS_DynamicWavesZoneFoamData", new Vector4((int)zone.FoamTextureType, zone.FoamTextureContrast, zone.FoamTextureScaleMultiplier, zone.FoamAlpha));
            cmd.SetGlobalFloat(KwsKwsDynamicWavesBakingMode,              isBakeMode ? 1 : 0);
            cmd.SetGlobalInteger("KWS_DynamicWavesUseFoamTexture", zone.UseFoamTexture ? 1 : 0);
            cmd.SetGlobalInteger("KWS_DynamicWavesUseAdvectedUV",  zone.FoamType == FoamTypeEnum.Advected ? 1 : 0);
            cmd.SetGlobalFloat("KWS_DynamicWavesRainDropsIntensity", zone.RainDropsIntensity);
            
        }

        internal static void UpdateSimulationShaderParams(CommandBuffer cmd, KWS_DynamicWavesSimulationZone zone)
        {
            var simData    = zone._simulationData;
           
            
            var deltaTime = simData.FoamParticlesData.TimeSlices / (60f        / (1 + zone.MaxSkippedFrames));
            cmd.SetGlobalFloat("KWS_DeltaTime",         deltaTime);
            cmd.SetGlobalFloat("KWS_UnscaledDeltaTime", (deltaTime * Time.timeScale) /  Mathf.Max(1, zone.CurrentTimeScale));
            
            cmd.SetGlobalFloat("KWS_SimulationWaveSpeedMultiplier",       zone.SimulationWaveSpeedMultiplier);
            cmd.SetGlobalFloat(KwsKwsDynamicWavesZoneFlowSpeedMultiplier, zone.FlowSpeedMultiplier);
            cmd.SetGlobalFloat("KWS_SimulationTimeScale",                 zone.CurrentTimeScale);
            cmd.SetGlobalFloat("KWS_EvaporationRate",                     zone.EvaporationRate);
            cmd.SetGlobalFloat("KWS_RainDropsIntensity",                     zone.RainDropsIntensity);
            
            cmd.SetGlobalVector(KwsDynamicWavesZonePosition,       zone.Position);
            cmd.SetGlobalVector(KwsDynamicWavesZoneSize,           zone.Size);
            cmd.SetGlobalVector(KwsDynamicWavesZoneRotationMatrix, zone.RotationMatrix);
        
            cmd.SetGlobalInteger("KWS_DynamicWavesZoneID",        ((KWS_TileZoneManager.IWaterZone)zone).ID);
            
            cmd.SetGlobalTexture(KWS_ShaderConstants.DynamicWaves.KWS_DynamicWavesMaskRT,    simData.DynamicWavesMask);
            cmd.SetGlobalTexture(KWS_ShaderConstants.DynamicWaves.KWS_DynamicWavesDepthMask, simData.DynamicWavesMaskDepth);

            cmd.SetGlobalTexture(KWS_ShaderConstants.OrthoDepth.KWS_WaterOrthoDepthRT,  simData.Depth);
            cmd.SetGlobalTexture(KWS_ShaderConstants.OrthoDepth.KWS_WaterOrthoDepthSDF, simData.DepthSDF);

            cmd.SetGlobalVector(KWS_ShaderConstants.OrthoDepth.KWS_OrthoDepthPos,         zone.Position);
            cmd.SetGlobalVector(KWS_ShaderConstants.OrthoDepth.KWS_OrthoDepthNearFarSize, zone.BakedNearFarSizeXZ);

            cmd.SetGlobalTexture(KWS_ShaderConstants.DynamicWaves.KWS_DynamicWaves,                 simData.GetTarget);
            cmd.SetGlobalTexture(KWS_ShaderConstants.DynamicWaves.KWS_DynamicWavesNormals,          simData.GetNormals);
            cmd.SetGlobalTexture(KWS_ShaderConstants.DynamicWaves.KWS_DynamicWavesAdditionalDataRT, simData.GetAdditionalTarget);
            
            if (zone.FoamType == FoamTypeEnum.Advected &&  simData.DynamicWavesAdvectedUV1 != null && simData.DynamicWavesAdvectedUV1.rt) cmd.SetGlobalTexture("KWS_DynamicWavesAdvectedUV",                     simData.GetAdvectedUVTarget);
            if (zone.RequireColorRendering && simData.GetColorTarget != null)
            {
                cmd.SetGlobalTexture(KWS_ShaderConstants.DynamicWaves.KWS_DynamicWavesMaskColorRT, simData.DynamicWavesMaskColor);
                cmd.SetGlobalTexture(KWS_ShaderConstants.DynamicWaves.KWS_DynamicWavesColorDataRT, simData.GetColorTarget);
            }
            
            if(simData.DynamicWavesIntersections!= null) cmd.SetGlobalTexture("KWS_DynamicWavesIntersection", simData.DynamicWavesIntersections);
        }

        internal static void UpdateZoneShaderParams(Material mat, KWS_DynamicWavesSimulationZone zone)
        {
            if (mat == null)
            {
                return;
            }
            
            var isPreviewMode = zone.ZoneType == SimulationZoneTypeMode.BakedSimulation && (zone._isBakeMode || zone._isPreviewMode);
            
            mat.SetFloat(KwsOceanWavesInfluenceStrength, zone.OceanWavesInfluenceStrength);
            mat.SetFloat("KWS_ZoneShorelineDistance", zone.ShorelineDistance);
            
            mat.SetFloat("KWS_TurbulenceFoamStrength", zone.TurbulenceFoamStrength);
            mat.SetFloat("KWS_WaveCrestFoamStrength",  zone.WaveCrestFoamStrength);
            mat.SetFloat("KWS_ShorelineFoamStrength",  zone.ShorelineFoamStrength);
            mat.SetFloat("KWS_FoamDisappearSpeed",     zone.FoamDisappearSpeed);
            
            mat.SetFloat("KWS_FoamEmissionRate", zone.FoamEmissionRate);
            mat.SetFloat(KwsSplashEmissionRate,  zone.SplashEmissionRate);
            
            mat.SetFloat("KWS_MaxFoamRenderingDistance",   zone.MaxFoamRenderingDistance);
            mat.SetFloat("KWS_MaxSplashRenderingDistance", zone.MaxSplashRenderingDistance);
            
            mat.SetVector("KWS_DynamicWavesZoneFoamData", new Vector4((int)zone.FoamTextureType, zone.FoamTextureContrast, zone.FoamTextureScaleMultiplier, zone.FoamAlpha));
            mat.SetFloat("KWS_BakedZoneInPreviewMode", isPreviewMode ? 1 : 0);
            mat.SetInteger("KWS_DynamicWavesUseFoamTexture", zone.UseFoamTexture ? 1 : 0);
            mat.SetInteger("KWS_DynamicWavesUseAdvectedUV",  zone.FoamType == FoamTypeEnum.Advected ? 1 : 0);
          
        }
        
        internal static void UpdateSimulationShaderParams(Material mat, KWS_DynamicWavesSimulationZone zone)
        {
            if (mat == null)
            {
                return;
            }
            
            var simData    = zone._simulationData;
            
            var deltaTime = simData.FoamParticlesData.TimeSlices / (60f        / (1 + zone.MaxSkippedFrames));
            mat.SetFloat("KWS_DeltaTime",           deltaTime);
            mat.SetFloat("KWS_UnscaledDeltaTime",   (deltaTime * Time.timeScale) /  Mathf.Max(1, zone.CurrentTimeScale));
            mat.SetFloat("KWS_SimulationTimeScale", zone.CurrentTimeScale);
            mat.SetFloat("KWS_EvaporationRate",     zone.EvaporationRate);
            mat.SetFloat("KWS_RainDropsIntensity",     zone.RainDropsIntensity);
            
            mat.SetFloat(KwsKwsDynamicWavesZoneFlowSpeedMultiplier, zone.FlowSpeedMultiplier);
            mat.SetFloat("KWS_SimulationWaveSpeedMultiplier",       zone.SimulationWaveSpeedMultiplier);
            mat.SetVector(KwsDynamicWavesZonePosition,       zone.Position);
            mat.SetVector(KwsDynamicWavesZoneSize,           zone.Size);
            mat.SetVector(KwsDynamicWavesZoneRotationMatrix, zone.RotationMatrix);

            mat.SetVector(KWS_ShaderConstants.OrthoDepth.KWS_OrthoDepthPos,         zone.Position);
            mat.SetVector(KWS_ShaderConstants.OrthoDepth.KWS_OrthoDepthNearFarSize, zone.BakedNearFarSizeXZ);

            if (zone.ZoneType == SimulationZoneTypeMode.BakedSimulation && zone.SavedDynamicWavesSimulation)
            {
                mat.SetTexture(KWS_ShaderConstants.OrthoDepth.KWS_WaterOrthoDepthRT,  zone.SavedDepth);
                mat.SetTexture(KWS_ShaderConstants.OrthoDepth.KWS_WaterOrthoDepthSDF, zone.SavedDistanceField);

                mat.SetTexture(KWS_ShaderConstants.DynamicWaves.KWS_DynamicWaves,                 zone.SavedDynamicWavesSimulation);
                mat.SetTexture(KWS_ShaderConstants.DynamicWaves.KWS_DynamicWavesAdditionalDataRT, zone.SavedDynamicWavesAdditionalDataSimulation);
            }
            else
            {
                mat.SetTexture(KWS_ShaderConstants.DynamicWaves.KWS_DynamicWavesMaskRT,    simData.DynamicWavesMask);
                mat.SetTexture(KWS_ShaderConstants.DynamicWaves.KWS_DynamicWavesDepthMask, simData.DynamicWavesMaskDepth);

                mat.SetTexture(KWS_ShaderConstants.OrthoDepth.KWS_WaterOrthoDepthRT,  simData.Depth);
                mat.SetTexture(KWS_ShaderConstants.OrthoDepth.KWS_WaterOrthoDepthSDF, simData.DepthSDF);

                mat.SetTexture(KWS_ShaderConstants.DynamicWaves.KWS_DynamicWaves,                                                                                                          simData.GetTarget);
                mat.SetTexture(KWS_ShaderConstants.DynamicWaves.KWS_DynamicWavesNormals,                                                                                                   simData.GetNormals);
                mat.SetTexture(KWS_ShaderConstants.DynamicWaves.KWS_DynamicWavesAdditionalDataRT,                                                                                          simData.GetAdditionalTarget);
                if (zone.FoamType == FoamTypeEnum.Advected &&  simData.DynamicWavesAdvectedUV1 != null && simData.DynamicWavesAdvectedUV1.rt)  mat.SetTexture("KWS_DynamicWavesAdvectedUV",simData.GetAdvectedUVTarget);
                if (zone.RequireColorRendering && simData.GetColorTarget != null)
                {
                    mat.SetTexture(KWS_ShaderConstants.DynamicWaves.KWS_DynamicWavesMaskColorRT, simData.DynamicWavesMaskColor);
                    mat.SetTexture(KWS_ShaderConstants.DynamicWaves.KWS_DynamicWavesColorDataRT, simData.GetColorTarget);
                }
            }
            
          
        }
        
        private void ExecuteFoamParticles(CommandBuffer cmd, Camera cam, int fpsFrames, KWS_DynamicWavesSimulationZone zone)
        {
            var simData       = zone._simulationData;
            var particlesData = simData.FoamParticlesData;
            var particleCS    = particlesData._particlesComputeShader;
            if (!particleCS) return;

            if (Time.frameCount <= 3) ClearBuckets(cmd, particlesData, particleCS);

            var deltaTime = particlesData.TimeSlices / (60f / (1 + zone.MaxSkippedFrames));
           // KWS_CoreUtils.SetAllVPCameraMatrices(cam, cmd, particleCS);
            
            ParticlesInitKernelData(cmd, cam, zone, particlesData, particleCS, deltaTime);

            var bucket = particlesData.ActiveBucket;

           
            ParticlesKernelSpawnBucket(cmd, particlesData, bucket, particleCS, simData, ID_KWS_FoamParticlesBuffer1, ID_KWS_FoamParticlesBuffer2);

            ParticlesKernelInitGPUDispatchBucket(cmd, particlesData, bucket, particleCS);

            ParticlesKernelUpdateBucket(cmd, particlesData, bucket, particleCS, simData, ID_KWS_FoamParticlesBuffer, ID_KWS_FoamParticlesBuffer2);

            ParticlesKernelComputeIndirectRenderingArgsBucket(cmd, particlesData, bucket, particleCS);

            bucket.Swap();
            particlesData.CurrentFrame++;
        }

     
        private void ParticlesKernelSpawnBucket(CommandBuffer cmd,        ParticlesData  particlesData, ParticlesData.ParticlesBuffer bucket,
                                                ComputeShader particleCS, SimulationData simData,       int                           currentBuffer, int nextBuffer)
        {
            if (KWS_TileZoneManager.IsAnyZoneUseRequireColorRendering)
                cmd.SetComputeTextureParam(particleCS, particlesData._kernelSpawnParticles,
                                           KWS_ShaderConstants.DynamicWaves.KWS_DynamicWavesColorDataRT, simData.GetColorTarget.GetSafeTexture());

            var target = simData.GetTarget;

            cmd.SetComputeTextureParam(particleCS, particlesData._kernelSpawnParticles, KWS_ShaderConstants.DynamicWaves.KWS_DynamicWaves,                 target);
            cmd.SetComputeTextureParam(particleCS, particlesData._kernelSpawnParticles, KWS_ShaderConstants.DynamicWaves.KWS_DynamicWavesAdditionalDataRT, simData.GetAdditionalTarget);

            cmd.SetComputeBufferParam(particleCS, particlesData._kernelSpawnParticles, "KWS_CounterBufferSpawn", bucket.CounterBufferSpawn);
            cmd.SetComputeBufferParam(particleCS, particlesData._kernelSpawnParticles, "KWS_CounterBufferRender",    bucket.CounterBufferRender);
            if (particlesData.TileParticleCountBuffer != null) cmd.SetComputeBufferParam(particleCS, particlesData._kernelSpawnParticles, "KWS_TileParticleCount", particlesData.TileParticleCountBuffer);
            
            cmd.SetComputeBufferParam(particleCS, particlesData._kernelSpawnParticles, currentBuffer,              bucket.CurrentBuffer);
            cmd.SetComputeBufferParam(particleCS, particlesData._kernelSpawnParticles, nextBuffer, bucket.NextBuffer);

            float threads  = 8;
            float sliceMul = 1.0f;
            var dispatchSize = new Vector2Int(
                Mathf.CeilToInt(sliceMul * target.rt.width  / threads),
                Mathf.CeilToInt(sliceMul * target.rt.height / threads));

            cmd.DispatchCompute(particleCS, particlesData._kernelSpawnParticles, dispatchSize.x, dispatchSize.y, 1);
        }
        
        
        private void ParticlesKernelInitGPUDispatchBucket(CommandBuffer cmd, ParticlesData particlesData, ParticlesData.ParticlesBuffer bucket, ComputeShader particleCS)
        {
            cmd.SetComputeBufferParam(particleCS, particlesData._kernelUpdateParticlesDispatchArgs, "KWS_CounterBufferSpawn", bucket.CounterBufferSpawn);
            cmd.SetComputeBufferParam(particleCS, particlesData._kernelUpdateParticlesDispatchArgs, "KWS_CounterBufferUpdate", bucket.CounterBufferUpdate);
            cmd.SetComputeBufferParam(particleCS, particlesData._kernelUpdateParticlesDispatchArgs, "KWS_UpdateIndirectArgs", bucket.UpdateDispatchIndirectArgs);

            if (particlesData.TileParticleCountBuffer != null) cmd.SetComputeBufferParam(particleCS, particlesData._kernelUpdateParticlesDispatchArgs, "KWS_TileParticleCount", particlesData.TileParticleCountBuffer);

            
            cmd.DispatchCompute(particleCS, particlesData._kernelUpdateParticlesDispatchArgs, 1, 1, 1);
        }
        
        private void ParticlesKernelUpdateBucket(CommandBuffer cmd,        ParticlesData  particlesData, ParticlesData.ParticlesBuffer bucket,
                                                 ComputeShader particleCS, SimulationData simData,       int                           currentBuffer, int nextBuffer)
        {
            if (KWS_TileZoneManager.IsAnyZoneUseRequireColorRendering)
                cmd.SetComputeTextureParam(particleCS, particlesData._kernelUpdateParticles,
                                           KWS_ShaderConstants.DynamicWaves.KWS_DynamicWavesColorDataRT, simData.GetColorTarget.GetSafeTexture());

            cmd.SetComputeTextureParam(particleCS, particlesData._kernelUpdateParticles, KWS_ShaderConstants.DynamicWaves.KWS_DynamicWavesAdditionalDataRT, simData.GetAdditionalTarget);
            cmd.SetComputeTextureParam(particleCS, particlesData._kernelUpdateParticles, KWS_ShaderConstants.DynamicWaves.KWS_DynamicWaves,                 simData.GetTarget);
            cmd.SetComputeTextureParam(particleCS, particlesData._kernelUpdateParticles, KwsKwsPerlinNoise,                                                 WaterSharedResources.KWS_PerlinNoise.GetSafeTexture());

            cmd.SetComputeBufferParam(particleCS, particlesData._kernelUpdateParticles, "KWS_CounterBufferSpawn",  bucket.CounterBufferSpawn);
            cmd.SetComputeBufferParam(particleCS, particlesData._kernelUpdateParticles, "KWS_CounterBufferUpdate", bucket.CounterBufferUpdate);
            if (particlesData.TileParticleCountBuffer != null) cmd.SetComputeBufferParam(particleCS, particlesData._kernelUpdateParticles, "KWS_TileParticleCount", particlesData.TileParticleCountBuffer);

            cmd.SetComputeBufferParam(particleCS, particlesData._kernelUpdateParticles, currentBuffer, bucket.CurrentBuffer);
            cmd.SetComputeBufferParam(particleCS, particlesData._kernelUpdateParticles, nextBuffer,  bucket.NextBuffer);

            cmd.DispatchCompute(particleCS, particlesData._kernelUpdateParticles, bucket.UpdateDispatchIndirectArgs, 0);
        }
        
        private void ParticlesKernelComputeIndirectRenderingArgsBucket(CommandBuffer                 cmd,    ParticlesData particlesData,
                                                                       ParticlesData.ParticlesBuffer bucket, ComputeShader particleCS)
        {
            
            cmd.SetComputeBufferParam(particleCS, particlesData._kernelParticlesToScreenDispatchArgs, "KWS_CounterBufferSpawn",                  bucket.CounterBufferSpawn);
            cmd.SetComputeBufferParam(particleCS, particlesData._kernelParticlesToScreenDispatchArgs, "KWS_CounterBufferUpdate",                 bucket.CounterBufferUpdate);
            cmd.SetComputeBufferParam(particleCS, particlesData._kernelParticlesToScreenDispatchArgs, "KWS_CounterBufferRender",                 bucket.CounterBufferRender);
            cmd.SetComputeBufferParam(particleCS, particlesData._kernelParticlesToScreenDispatchArgs, "KWS_ScreenSpaceFoamDispatchIndirectArgs", bucket.RenderDispatchIndirectArgs);
            cmd.SetComputeBufferParam(particleCS, particlesData._kernelParticlesToScreenDispatchArgs, "KWS_ParticlesIndirectArgs", particlesData.VertexParticlesIndirectArgs);
            
            cmd.DispatchCompute(particleCS, particlesData._kernelParticlesToScreenDispatchArgs, 1, 1, 1);
        }
        

        private void ClearBuckets(CommandBuffer cmd, ParticlesData particlesData, ComputeShader particleCS)
        {
            if (particlesData.BucketParticleBuffer == null) return;

            foreach (var bucket in particlesData.BucketParticleBuffer)
            {
                if (bucket == null) continue;

                bucket.LocalFrame        = 0;
                bucket.InterpolationTime = 0;
                
                cmd.SetComputeBufferParam(particleCS, particlesData._kernelClearCounters, "KWS_CounterBufferSpawn",                  bucket.CounterBufferSpawn);
                cmd.SetComputeBufferParam(particleCS, particlesData._kernelClearCounters, "KWS_CounterBufferUpdate",                 bucket.CounterBufferUpdate);
                cmd.SetComputeBufferParam(particleCS, particlesData._kernelClearCounters, "KWS_CounterBufferRender",                 bucket.CounterBufferRender);
                cmd.SetComputeBufferParam(particleCS, particlesData._kernelClearCounters, "KWS_UpdateIndirectArgs",                  bucket.UpdateDispatchIndirectArgs);
                cmd.SetComputeBufferParam(particleCS, particlesData._kernelClearCounters, "KWS_ScreenSpaceFoamDispatchIndirectArgs", bucket.RenderDispatchIndirectArgs);

                cmd.DispatchCompute(particleCS, particlesData._kernelClearCounters, 1, 1, 1);
            }

            particlesData.CurrentFrame = 0;
        }
        
        
        
        private void ExecuteSplashParticles(CommandBuffer cmd, Camera cam, int fpsFrames, KWS_DynamicWavesSimulationZone zone)
        {/*
            var simData       = zone._simulationData;
            var particlesData = simData.SplashParticlesData;
            var particleCS    = particlesData._particlesComputeShader;
            if (!particleCS) return;

            if (Time.frameCount <= 3) ClearBuffer(cmd, particlesData, particleCS);

            var deltaTime = fpsFrames / 60f;

            KWS_CoreUtils.SetAllVPCameraMatrices(cam, cmd, particleCS);

            ParticlesInitKernelData(cmd, cam, zone, particlesData, particleCS, deltaTime);
            ParticlesKernelSpawn(cmd, particlesData, particleCS, simData, ID_KWS_SplashParticlesBuffer1, ID_KWS_SplashParticlesBuffer2);
            ParticlesKernelInitGPUDispatch(cmd, particlesData, particleCS);
            ParticlesResetPinPongBuffers(cmd, particlesData);
            ParticlesKernelUpdate(cmd, particlesData, particleCS, simData, ID_KWS_SplashParticlesBuffer, ID_KWS_SplashParticlesBuffer2);
            ParticlesCopyCounters(cmd, particlesData);
            ParticlesKernelComputeIndirectRenderingArgs(cmd, particlesData, particleCS);

            particlesData.SwapParticlesBuffers();
            particlesData.ParticlesInterpolationTime = 0;*/
            
            var simData       = zone._simulationData;
            var particlesData = simData.SplashParticlesData;
            var particleCS    = particlesData._particlesComputeShader;
            if (!particleCS) return;

            
            int tileSize = 64;

            int screenWidth  = cam.pixelWidth;
            int screenHeight = cam.pixelHeight;

            int tilesX = (screenWidth  + tileSize - 1) / tileSize;
            int tilesY = (screenHeight + tileSize - 1) / tileSize;

            int tilesCount = tilesX * tilesY;
            
            particlesData.TileParticleCountBuffer = KWS_CoreUtils.GetOrUpdateBuffer<uint>(ref particlesData.TileParticleCountBuffer, tilesCount, ComputeBufferType.Structured);
            cmd.SetComputeIntParam(particleCS, "KWS_TilesCount", tilesCount);
            cmd.SetComputeVectorParam(particleCS, "KWS_TilesSize", new Vector2(tilesX, tilesY));
            
            if (Time.frameCount <= 3) ClearBuckets(cmd, particlesData, particleCS);

            var deltaTime = particlesData.TimeSlices / (60f / (1 + zone.MaxSkippedFrames));
            KWS_CoreUtils.SetAllVPCameraMatrices(cam, cmd, particleCS);
            
            ParticlesInitKernelData(cmd, cam, zone, particlesData, particleCS, deltaTime);

            var bucket = particlesData.ActiveBucket;

           
            ParticlesKernelSpawnBucket(cmd, particlesData, bucket, particleCS, simData, ID_KWS_SplashParticlesBuffer1, ID_KWS_SplashParticlesBuffer2);

            ParticlesKernelInitGPUDispatchBucket(cmd, particlesData, bucket, particleCS);

            //clear screen space tiles
            //cmd.SetComputeBufferParam(particleCS, particlesData._kernelClearScreenSpaceParticlesBuffer, "KWS_TileParticleCount", particlesData.TileParticleCountBuffer);

            
            ParticlesKernelUpdateBucket(cmd, particlesData, bucket, particleCS, simData, ID_KWS_SplashParticlesBuffer, ID_KWS_SplashParticlesBuffer2);

            ParticlesKernelComputeIndirectRenderingArgsBucket(cmd, particlesData, bucket, particleCS);

          
            bucket.Swap();
            particlesData.CurrentFrame++;
            
            
        }

        

        private void ParticlesInitKernelData(CommandBuffer cmd, Camera cam, KWS_DynamicWavesSimulationZone zone, ParticlesData particlesData, ComputeShader particleCS, float deltaTime)
        {
            cmd.SetKeyword("KWS_BAKED_ZONE", zone.ZoneType == SimulationZoneTypeMode.BakedSimulation);
            
            cmd.SetComputeFloatParam(particleCS, KwsOceanWavesInfluenceStrength,     zone.OceanWavesInfluenceStrength);
            cmd.SetComputeFloatParam(particleCS, "KWS_ZoneShorelineDistance", zone.ShorelineDistance);
            cmd.SetComputeFloatParam(particleCS, KwsDynamicWavesFlowSpeedMultiplier, zone.FlowSpeedMultiplier);
            cmd.SetComputeFloatParam(particleCS, "KWS_TurbulenceFoamStrength", zone.TurbulenceFoamStrength);
            cmd.SetComputeFloatParam(particleCS, "KWS_WaveCrestFoamStrength",  zone.WaveCrestFoamStrength);
            cmd.SetComputeFloatParam(particleCS, KwsFoamDisappearSpeed,     zone.FoamDisappearSpeed);
           
            cmd.SetComputeFloatParam(particleCS,                   KwsDeltaTime, deltaTime * Time.timeScale);
            cmd.SetComputeFloatParam(particleCS,                   "KWS_UnscaledDeltaTime",  (deltaTime * Time.timeScale) /  Mathf.Max(1, zone.CurrentTimeScale));

            cmd.SetComputeVectorParam(particleCS, KwsDistancePerPixel, new Vector2(2f * zone.Size.x / zone.TextureSize.x, 2f * zone.Size.z / zone.TextureSize.y));
            cmd.SetComputeIntParam(particleCS, MaxParticles, particlesData.TimeSliceParticlesBudget);
            cmd.SetComputeVectorParam(particleCS, KwsWorldSpaceCameraPos, cam.transform.position);
            cmd.SetComputeVectorParam(particleCS, KwsCameraForward,       cam.transform.forward);

            cmd.SetComputeFloatParam(particleCS, "KWS_FoamParticleLifetime",         zone.FoamParticleLifetime);
            cmd.SetComputeFloatParam(particleCS, "KWS_FoamClumping",                 zone.FoamClumping);
            
            cmd.SetComputeFloatParam(particleCS, KwsSplashParticlesBudgetNormalized, _normalizedBudget[zone.MaxSplashParticlesBudget]);
            cmd.SetComputeIntParam(particleCS, KwsCurrentFrame, particlesData.CurrentFrame);
            cmd.SetComputeVectorParam(particleCS, KwsCurrentScreenSize, new Vector4(cam.pixelWidth, cam.pixelHeight, 0, 0)); //_ScreenParams doesnt works in editor

            cmd.SetComputeIntParam(particleCS, KwsUsePhytoplanktonEmission, zone.UsePhytoplanktonEmission ? 1 : 0);
            cmd.SetComputeIntParam(particleCS, "KWS_ReceiveWaterFlowFromOtherZones", zone.ReceiveWaterFlowFromOtherZones ? 1 : 0);
         
            
        }

        void ClearParticlesBuffers(WaterPassContext ctx, KWS_DynamicWavesSimulationZone zone)
        {
            if (_screenSpaceFoamTexture == null || _screenSpaceFoamBuffers[0] == null) InitializeScreenSpaceFoam();
            
            var cmd           = ctx.cmd;
            var particlesData =  zone._simulationData.FoamParticlesData;
            var particleCS    = particlesData._particlesComputeShader;
            if (!particleCS) return;
            int clearKernel = particlesData._kernelClearScreenSpaceParticlesBuffer;

            //clear
            cmd.SetComputeBufferParam(particleCS, clearKernel, "ScreenSpaceFoamBufferRW0", _screenSpaceFoamBuffers[0]);
            cmd.SetComputeBufferParam(particleCS, clearKernel, "ScreenSpaceFoamBufferRW1", _screenSpaceFoamBuffers[1]);
            cmd.SetComputeBufferParam(particleCS, clearKernel, "ScreenSpaceFoamBufferRW2", _screenSpaceFoamBuffers[2]);

            for (int i = 0; i < _screenSpaceFoamBuffers.Length; i++)
            {
                cmd.SetComputeIntParam(particleCS, "KWS_CurrentScreenSpaceBufferLodIndex", i);
                int clearGroups = Mathf.CeilToInt((ScreenSpaceFoamBufferSizes[i].x * ScreenSpaceFoamBufferSizes[i].y) / 64.0f);

                cmd.DispatchCompute(particleCS, clearKernel, clearGroups, 1, 1);
            }
        }
        
        void RenderParticlesToBuffer(WaterPassContext ctx, KWS_DynamicWavesSimulationZone zone)
        {
            if (_screenSpaceFoamTexture == null || _screenSpaceFoamBuffers[0] == null) InitializeScreenSpaceFoam();

            var cmd           = ctx.cmd;
            var particlesData = zone._simulationData.FoamParticlesData;
            var particleCS    = particlesData._particlesComputeShader;

            if (!particleCS) return;

            int kernel = particlesData._kernelRenderParticlesToScreenSpaceBuffer;

            for (int i = 0; i < particlesData.BucketParticleBuffer.Length; i++)
            {
                var bucket = particlesData.BucketParticleBuffer[i];
                if (bucket == null) continue;

                cmd.SetComputeBufferParam(particleCS, kernel, "ScreenSpaceFoamBufferRW0", _screenSpaceFoamBuffers[0]);
                cmd.SetComputeBufferParam(particleCS, kernel, "ScreenSpaceFoamBufferRW1", _screenSpaceFoamBuffers[1]);
                cmd.SetComputeBufferParam(particleCS, kernel, "ScreenSpaceFoamBufferRW2", _screenSpaceFoamBuffers[2]);

                cmd.SetComputeBufferParam(particleCS, kernel, "KWS_FoamParticlesBuffer", bucket.CurrentBuffer);
                cmd.SetComputeBufferParam(particleCS, kernel, "KWS_CounterBufferRender",  bucket.CounterBufferRender);

                cmd.SetComputeFloatParam(particleCS, KwsParticlesFoamInterpolationTime, bucket.InterpolationTime); 
                cmd.SetComputeFloatParam(particleCS, "KWS_FoamParticlesScale",          zone.FoamParticlesScale);
               
                cmd.DispatchCompute(particleCS, kernel, bucket.RenderDispatchIndirectArgs, 0);
            }
        }
        
        void RenderParticlesToCameraTarget(WaterPassContext ctx)
        {
            if (_screenSpaceFoamTexture == null || _screenSpaceFoamBuffers[0] == null) InitializeScreenSpaceFoam();
            
            var cmd           = ctx.cmd;
           
            Shader.SetGlobalBuffer("ScreenSpaceFoamBuffer0", _screenSpaceFoamBuffers[0]);
            Shader.SetGlobalBuffer("ScreenSpaceFoamBuffer1", _screenSpaceFoamBuffers[1]);
            Shader.SetGlobalBuffer("ScreenSpaceFoamBuffer2", _screenSpaceFoamBuffers[2]);
            Shader.SetGlobalVectorArray("ScreenSpaceFoamBufferSizes", ScreenSpaceFoamBufferSizes);
            
           
            cmd.SetKeyword("KWS_USE_PHYTOPLANKTON_EMISSION", KWS_TileZoneManager.IsAnyZoneUsePhytoplanktonEmission);
           
            //render to texture

            CoreUtils.SetRenderTarget(cmd, _screenSpaceFoamTexture,       ClearFlag.Color, Color.black);
            cmd.BlitTriangle(_screenSpaceFoamMaterial, pass: 0);
            cmd.BlitTriangleRTHandle(_screenSpaceFoamTexture, ctx.cameraColor,  _screenSpaceFoamMaterial, ClearFlag.None, Color.black, pass: 2);
            
            Shader.SetGlobalTexture("ScreenSpaceFoamTex",       _screenSpaceFoamTexture);
        }
        
       
        
        private void OnOriginShifted(Vector3 shift)
        {
           
            var zones = KWS_TileZoneManager.DynamicWavesZones;
            {
                foreach (var izone in zones)
                {
                    var zone = (KWS_DynamicWavesSimulationZone)izone;

                    if (zone.ZoneType == SimulationZoneTypeMode.BakedSimulation) continue;

                    if (zone.UseFoamParticles)
                    {
                        ShiftBuffers(shift, zone._simulationData.FoamParticlesData, ID_KWS_FoamParticlesBuffer1);
                    }

                    if (zone.UseSplashParticles)
                    {
                        ShiftBuffers(shift, zone._simulationData.SplashParticlesData, ID_KWS_SplashParticlesBuffer1);
                    }
                }
            }
            
        }

        private static void ShiftBuffers(Vector3 shift, ParticlesData data, int bufferID)
        {
            var cs     = data._particlesComputeShader;
            if (cs == null) return;
            if (data.ActiveBucket == null) return;

            foreach (var bucket in data.BucketParticleBuffer)
            {
                if (bucket.CurrentBuffer != null && bucket.CurrentBuffer.count > 0 && bucket.NextBuffer != null)
                {
                    int kernel = data._kernelShiftParticles;
                    int groups = Mathf.Max(1, Mathf.CeilToInt(bucket.CurrentBuffer.count / 256f));

                    cs.SetVector( "KWS_OriginShift", shift);
                    cs.SetBuffer(kernel, bufferID, bucket.CurrentBuffer);
                    cs.Dispatch(kernel, groups, 1, 1);
                    cs.SetBuffer(kernel, bufferID, bucket.NextBuffer);
                    cs.Dispatch(kernel, groups, 1, 1);
                }
            }
                        
          
        }


        private void ExecuteParticles(Camera cam)
        {
            var simZones = KWS_TileZoneManager.VisibleDynamicWavesZones;
            foreach (var iZone in simZones)
            {
                var zone = (KWS_DynamicWavesSimulationZone)iZone;

                if(!zone.IsZoneVisible || !zone.IsZoneInitialized || zone.ZoneType == SimulationZoneTypeMode.BakedSimulation) continue;
                
                // if (zone.UseFoamParticles)
                // {
                //     var particlesData = zone._simulationData.FoamParticlesData;
                //     var mat           = particlesData._particlesMaterial;
                //
                //     if (particlesData.GetCurrentParticlesBuffer != null &&
                //         particlesData.GetCurrentParticlesBuffer.count > 0)
                //     {
                //         mat.SetBuffer(KwsFoamParticlesBuffer, particlesData.GetCurrentParticlesBuffer);
                //         mat.SetFloat(KwsParticlesFoamInterpolationTime, particlesData.ParticlesInterpolationTime);
                //         mat.SetFloat(KwsFoamParticlesScale,             zone.FoamParticlesScale * WaterSystem._globalWaterScaleFactor);
                //         mat.SetFloat(KwsFoamParticlesAlphaMultiplier,   zone.FoamParticlesAlphaMultiplier);
                //         mat.SetKeyword("KWS_USE_PHYTOPLANKTON_EMISSION",    zone.UsePhytoplanktonEmission);
                //         mat.SetKeyword("KWS_LOCAL_DYNAMIC_WAVES_USE_COLOR", zone.RequireColorRendering);
                //         
                //         UpdateSimulationShaderParams(mat, zone);
                //
                //         var renderParams = particlesData._particlesRenderParams;
                //         renderParams.camera      = cam;
                //         renderParams.worldBounds = ((KWS_TileZoneManager.IWaterZone)zone).OrientedBounds;
                //         Graphics.RenderPrimitivesIndirect(renderParams, MeshTopology.Triangles, particlesData.ParticlesIndirectArgs);
                //     }
                //   
                // }

                if (zone.UseSplashParticles)
                {
                    var particlesData = zone._simulationData.SplashParticlesData;
                    var mat           = particlesData._particlesMaterial;
                    var bucket        = particlesData.ActiveBucket;
                    if (bucket == null) continue;
                    
                    if (bucket.CurrentBuffer       != null &&
                        bucket.CurrentBuffer.count > 0)
                    {
                        mat.SetTexture(KWS_ShaderConstants.DynamicWaves.KWS_DynamicWavesColorDataRT,
                            zone._simulationData.GetColorTarget);
                        mat.SetBuffer(KwsSplashParticlesBuffer, bucket.CurrentBuffer);
                        mat.SetFloat(KwsParticlesSplashInterpolationTime, bucket.InterpolationTime);
                        mat.SetFloat(KwsSplashParticlesScale,             zone.SplashParticlesScale * WaterSystem._globalWaterScaleFactor);
                        mat.SetFloat(KwsSplashParticlesAlphaMultiplier,   zone.SplashParticlesAlphaMultiplier);

                        var isVertexShadow = zone.ReceiveShadowMode is SplashReceiveShadowModeEnum.DirectionalLowQuality
                            or SplashReceiveShadowModeEnum.AllShadowsLowQuality;
                        var isDirShadowOnly =
                            zone.ReceiveShadowMode is SplashReceiveShadowModeEnum.DirectionalLowQuality
                                or SplashReceiveShadowModeEnum.DirectionalHighQuality;
                        var isAllShadows = zone.ReceiveShadowMode is SplashReceiveShadowModeEnum.AllShadowsLowQuality
                            or SplashReceiveShadowModeEnum.AllShadowsHighQuality;

                        mat.SetKeyword("KWS_LOCAL_DYNAMIC_WAVES_USE_COLOR", zone.RequireColorRendering);
                        mat.SetKeyword("KWS_USE_PER_VERTEX_SHADOWS", isVertexShadow);
                        mat.SetKeyword("KWS_USE_DIR_SHADOW", isDirShadowOnly);
                        mat.SetKeyword("KWS_USE_ALL_SHADOWS", isAllShadows);
                        mat.SetKeyword("KWS_USE_SPLASH_SHADOW_CAST_FAST",
                            zone.CastShadowMode == SplashCasticShadowModeEnum.LowQuality);

                        UpdateSimulationShaderParams(mat, zone);

                        var renderParams = particlesData._particlesRenderParams;
                        renderParams.camera      = cam;
                        renderParams.worldBounds = ((KWS_TileZoneManager.IWaterZone)zone).OrientedBounds;
                        renderParams.shadowCastingMode = zone.CastShadowMode == SplashCasticShadowModeEnum.Disabled
                            ? ShadowCastingMode.Off
                            : ShadowCastingMode.On;

                        Graphics.RenderPrimitivesIndirect(renderParams, MeshTopology.Triangles, particlesData.VertexParticlesIndirectArgs);
                        //cmd.DrawProceduralIndirect(Matrix4x4.identity, mat, 0, MeshTopology.Triangles, particlesData.VertexParticlesIndirectArgs, 0);
                    }
                }
            }
        }

     
        private List<KWS_DynamicWavesSimulationEffector> GetInteractScriptsInArea(Vector3 pos, Vector3 size, Quaternion rotation)
        {
            _interactScriptsInArea.Clear();

            var halfSize   = new Vector2(size.x * 0.5f, size.z * 0.5f);
            var inverseRot = Quaternion.Inverse(rotation);

            foreach (var instance in KWS_TileZoneManager.DynamicWavesEffectors)
            {
                var instancePos = instance.transform.position;
                var relative    = instancePos - pos;

                var local   = inverseRot * relative;
                var local2D = new Vector2(local.x, local.z);

                if (Mathf.Abs(local2D.x) <= halfSize.x && Mathf.Abs(local2D.y) <= halfSize.y) _interactScriptsInArea.Add(instance);
            }

            return _interactScriptsInArea;
        }

        internal class ParticlesData
        {
            internal int TimeSlices = 4;

            internal int _kernelClearCounters;
            internal int _kernelSpawnParticles;
            internal int _kernelUpdateParticlesDispatchArgs;
            internal int _kernelUpdateParticles;
            internal int _kernelParticlesToScreenDispatchArgs;

            internal int _kernelClearScreenSpaceParticlesBuffer;
            internal int _kernelRenderParticlesToScreenSpaceBuffer;
            
            internal int _kernelShiftParticles;
            
            internal ComputeShader _particlesComputeShader;
         
            internal Material      _particlesMaterial;
            internal RenderParams  _particlesRenderParams;
            
            
            public int MaxParticlesBudget;
            public int TimeSliceParticlesBudget;

            internal GraphicsBuffer VertexParticlesIndirectArgs;
            internal ComputeBuffer  TileParticleCountBuffer;
  
            internal LocalKeyword  ShaderKeyword;

            internal ParticlesBuffer[] BucketParticleBuffer;
            
            public int             CurrentFrame;
            public int             ActiveBucketIndex => CurrentFrame % TimeSlices;
            public ParticlesBuffer ActiveBucket      => BucketParticleBuffer[ActiveBucketIndex];
            
            
            public class ParticlesBuffer
            {
                public ComputeBuffer BufferA;
                public ComputeBuffer BufferB;
                
                public ComputeBuffer CounterBufferSpawn;
                public ComputeBuffer CounterBufferUpdate;
                public ComputeBuffer CounterBufferRender;
                
                public ComputeBuffer UpdateDispatchIndirectArgs;
                public ComputeBuffer RenderDispatchIndirectArgs;
                
                public int           LocalFrame;
                public float         InterpolationTime;
                
                public  ComputeBuffer CurrentBuffer   => (LocalFrame & 1) == 0 ? BufferA : BufferB;
                public  ComputeBuffer NextBuffer  => (LocalFrame & 1) == 0 ? BufferB : BufferA;
                
               
                
                
                private int             timeSlices;
                
                public void InitializeBuffers<T>(int timeSliceParticlesBudget, int slices)where T : struct
                {
                    
                    if (BufferA == null || BufferA.count != timeSliceParticlesBudget)
                    {
                        ReleaseParticlesBuffers();

                        BufferA = KWS_CoreUtils.GetOrUpdateBuffer<T>(ref BufferA, timeSliceParticlesBudget, ComputeBufferType.Structured);
                        BufferB = KWS_CoreUtils.GetOrUpdateBuffer<T>(ref BufferB, timeSliceParticlesBudget,        ComputeBufferType.Structured);


                        CounterBufferSpawn = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Structured);
                        CounterBufferSpawn.SetData(new uint[] { 0 });
                        
                        CounterBufferUpdate = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Structured);
                        CounterBufferUpdate.SetData(new uint[] { 0 });

                    
                        CounterBufferRender = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Structured);
                        CounterBufferRender.SetData(new uint[] { 0 });
                    }
                    
                    if (UpdateDispatchIndirectArgs == null)
                    {
                        UpdateDispatchIndirectArgs = new ComputeBuffer(3, sizeof(int), ComputeBufferType.IndirectArguments);
                        var args = new uint[3] {1, 1, 1};
                        UpdateDispatchIndirectArgs.SetData(args);
                    }
                    
                    if (RenderDispatchIndirectArgs == null)
                    {
                        RenderDispatchIndirectArgs = new ComputeBuffer(3, sizeof(int), ComputeBufferType.IndirectArguments);
                        var args = new uint[3] {1, 1, 1};
                        RenderDispatchIndirectArgs.SetData(args);
                    }
                }
                
                public void ReleaseParticlesBuffers()
                {
                    BufferA?.Release();
                    BufferB?.Release();
                    CounterBufferSpawn?.Release();
                    CounterBufferUpdate?.Release();
                    CounterBufferRender?.Release();
                    UpdateDispatchIndirectArgs?.Release();
                    RenderDispatchIndirectArgs?.Release();

                    BufferA                    = BufferB                    = CounterBufferSpawn = CounterBufferRender = CounterBufferUpdate = null;
                    UpdateDispatchIndirectArgs = RenderDispatchIndirectArgs = null;
                    
                    LocalFrame         = 0;
                }
                
                
                public void Swap()
                {
                    LocalFrame++;
                    InterpolationTime = 0;
                }
                
            }

   
            public void InitializeParticlesBuffers<T>(int particlesTotal, string particlesShaderName, string shaderKeyword, bool isTriangle, int timeSlicedFrames) where T : struct
            {
                if (particlesTotal % timeSlicedFrames != 0)
                {
                    throw new Exception($"particlesTotal ({particlesTotal}) must be divisible by slices ({timeSlicedFrames})");
                }
                    
                TimeSlices               = timeSlicedFrames;
                TimeSliceParticlesBudget = particlesTotal / timeSlicedFrames;
                MaxParticlesBudget       = particlesTotal;

                if (BucketParticleBuffer == null)
                {
                    BucketParticleBuffer = new ParticlesBuffer[timeSlicedFrames];
                    for (int i = 0; i < BucketParticleBuffer.Length; i++)
                    {
                        BucketParticleBuffer[i] = new ParticlesBuffer();
                    }
                }
                
                foreach (var particlesBuffer in BucketParticleBuffer)
                {
                    particlesBuffer.ReleaseParticlesBuffers();
                    particlesBuffer.InitializeBuffers<T>(TimeSliceParticlesBudget, timeSlicedFrames);
                }
                

                if (VertexParticlesIndirectArgs == null)
                {
                    VertexParticlesIndirectArgs = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 4, sizeof(uint));
                    var args = new uint[4] { isTriangle ? 3u : 6u, 1, 0, 0 }; //trinagless count, instances count, 0, 0
                    VertexParticlesIndirectArgs.SetData(args);
                }


                if (!_particlesComputeShader)
                {
                    _particlesComputeShader = KWS_CoreUtils.LoadComputeShader("Common/CommandPass/KWS_DynamicWavesFoamParticlesCompute");

                    _kernelClearCounters = _particlesComputeShader.FindKernel("ClearCounters");
                    _kernelSpawnParticles                = _particlesComputeShader.FindKernel("SpawnParticles");
                    _kernelUpdateParticlesDispatchArgs   = _particlesComputeShader.FindKernel("UpdateParticlesDispatchArgs");
                    _kernelUpdateParticles               = _particlesComputeShader.FindKernel("UpdateParticles");
                    _kernelParticlesToScreenDispatchArgs = _particlesComputeShader.FindKernel("ParticlesToScreenDispatchArgs");
                    
                    _kernelClearScreenSpaceParticlesBuffer = _particlesComputeShader.FindKernel("ClearScreenSpaceParticlesBuffer");
                    _kernelRenderParticlesToScreenSpaceBuffer     = _particlesComputeShader.FindKernel("RenderParticlesToScreenSpaceBuffer");
                    
                    _kernelShiftParticles              = _particlesComputeShader.FindKernel("ShiftParticles");
                }
                

                if (_particlesMaterial == null) _particlesMaterial = KWS_CoreUtils.CreateMaterial(particlesShaderName, useWaterStencilMask: true);
                _particlesRenderParams = new RenderParams(_particlesMaterial);
                ShaderKeyword          = new LocalKeyword(_particlesComputeShader, shaderKeyword);

                
                _particlesComputeShader.SetKeyword(ShaderKeyword, true);
            }

            public void ReleaseParticlesBuffers()
            {
                if (BucketParticleBuffer != null)
                {
                    foreach (var particlesBuffer in BucketParticleBuffer)
                    {
                        particlesBuffer.ReleaseParticlesBuffers();
                    }
                }
                

                VertexParticlesIndirectArgs?.Release();
                VertexParticlesIndirectArgs = null;
                
                TileParticleCountBuffer?.Release();
                TileParticleCountBuffer = null;
                
                KW_Extensions.SafeDestroy(_particlesComputeShader, _particlesMaterial);
                _particlesMaterial      = null;
                _particlesComputeShader = null;

                this.WaterLog(string.Empty, KW_Extensions.WaterLogMessageType.Release);
            }
            
            public void UpdateInterpolationTime(int framesCount, int skippedFrames, float timeScale)
            {
                var currentFPS  = DefaultFPS / Mathf.Max(1, framesCount);
                var slicedDelta = timeScale  * KW_Extensions.DeltaTime() * (currentFPS / (1 + skippedFrames) / TimeSlices);

                if (BucketParticleBuffer == null) return;

                for (int i = 0; i < BucketParticleBuffer.Length; i++)
                {
                    BucketParticleBuffer[i].InterpolationTime += slicedDelta;
                }
            }
         
        }

        internal class SimulationData
        {
            //public Vector3 ZonePos;
            //public Vector3 ZoneSize;
            //public Vector4 NearFarSizeXZ;
            //public Vector2Int TextureSize;

            public int CurrentFrame;

            public Texture  Depth;
            public Texture  DepthSDF;
            public RTHandle DynamicWaves1;
            public RTHandle DynamicWaves2;
            public RTHandle DynamicWavesAdditionalData1;
            public RTHandle DynamicWavesAdditionalData2;
            public RTHandle DynamicWavesColorData1;
            public RTHandle DynamicWavesColorData2;
            public RTHandle DynamicWavesMask;

            public RTHandle DynamicWavesMaskColor;
            public RTHandle DynamicWavesMaskDepth;
            public RTHandle DynamicWavesNormals;

            public RTHandle DynamicWavesAdvectedUV1;
            public RTHandle DynamicWavesAdvectedUV2;
            
            public RTHandle DynamicWavesIntersections;


            internal ParticlesData FoamParticlesData   = new();
            internal ParticlesData SplashParticlesData = new();

            public RTHandle GetTarget               => DynamicWaves2;
            public RTHandle GetAdditionalTarget     => CurrentFrame % 2 == 0 ? DynamicWavesAdditionalData2 : DynamicWavesAdditionalData1;
            public RTHandle GetAdditionalTargetNext => CurrentFrame % 2 == 0 ? DynamicWavesAdditionalData1 : DynamicWavesAdditionalData2;
            public RTHandle GetNormals              => DynamicWavesNormals;
            
            public RTHandle GetAdvectedUVTarget     => CurrentFrame % 2 == 0 ? DynamicWavesAdvectedUV2 : DynamicWavesAdvectedUV1;
            public RTHandle GetAdvectedUVTargetNext => CurrentFrame % 2 == 0 ? DynamicWavesAdvectedUV1 : DynamicWavesAdvectedUV2;
            


            public RenderTexture GetColorTarget
            {
                get
                {
                    return (CurrentFrame % 2 == 0 ? DynamicWavesColorData2 : DynamicWavesColorData1);
                }
            }

            public RTHandle GetColorTargetNext => CurrentFrame % 2 == 0 ? DynamicWavesColorData1 : DynamicWavesColorData2;


            public void InitializeSimTextures(int width, int height)
            {
                DynamicWaves1 = KWS_CoreUtils.RTHandles.Alloc(width, height, name: "_dynamicWavesRT1", colorFormat: GraphicsFormat.R16G16B16A16_UNorm);
                DynamicWaves2 = KWS_CoreUtils.RTHandles.Alloc(width, height, name: "_dynamicWavesRT2", colorFormat: GraphicsFormat.R16G16B16A16_UNorm);

                DynamicWavesAdditionalData1 = KWS_CoreUtils.RTHandles.Alloc(width, height, name: "_dynamicWavesAdditionalData1", colorFormat: GraphicsFormat.R8G8B8A8_UNorm);
                DynamicWavesAdditionalData2 = KWS_CoreUtils.RTHandles.Alloc(width, height, name: "_dynamicWavesAdditionalData2", colorFormat: GraphicsFormat.R8G8B8A8_UNorm);

                DynamicWavesNormals = KWS_CoreUtils.RTHandles.Alloc(width, height, name: "_dynamicWavesNormals", colorFormat: GraphicsFormat.R8G8B8A8_SNorm);

                DynamicWavesMask      = KWS_CoreUtils.RTHandles.Alloc(width, height, name: "_dynamicWavesMaskRT",      colorFormat: GraphicsFormat.R8G8B8A8_UNorm);
                DynamicWavesMaskDepth = KWS_CoreUtils.RTHandles.Alloc(width, height, name: "_dynamicWavesMaskDepthRT", depthBufferBits: DepthBits.Depth16, colorFormat: GraphicsFormat.None);
             
               // "ShorelineWavesPass".WaterLog(DynamicWaves1, DynamicWavesAdditionalData1, DynamicWavesNormals, DynamicWavesMask, DynamicWavesMaskDepth);

                this.WaterLog(DynamicWaves1);
            }

            public void InitializeAdvectedUvTextures()
            {
                var width  = DynamicWaves1.rt.width;
                var height = DynamicWaves1.rt.height;
                
                DynamicWavesAdvectedUV1 = KWS_CoreUtils.RTHandles.Alloc(width, height, name: "_dynamicWavesAdvectedUV1", colorFormat: GraphicsFormat.R16G16B16A16_UNorm);
                DynamicWavesAdvectedUV2 = KWS_CoreUtils.RTHandles.Alloc(width, height, name: "_dynamicWavesAdvectedUV2", colorFormat: GraphicsFormat.R16G16B16A16_UNorm);

            }

            public void InitializeSimTexturesColor(CommandBuffer cmd)
            {
                var width  = DynamicWaves1.rt.width;
                var height = DynamicWaves1.rt.height;

                DynamicWavesMaskColor  = KWS_CoreUtils.RTHandles.Alloc(width, height, name: "_dynamicWavesMaskColor",  colorFormat: GraphicsFormat.R8G8B8A8_UNorm);
                DynamicWavesColorData1 = KWS_CoreUtils.RTHandles.Alloc(width, height, name: "_dynamicWavesColorData1", colorFormat: GraphicsFormat.R8G8B8A8_UNorm);
                DynamicWavesColorData2 = KWS_CoreUtils.RTHandles.Alloc(width, height, name: "_dynamicWavesColorData2", colorFormat: GraphicsFormat.R8G8B8A8_UNorm);
                
                CoreUtils.SetRenderTarget(cmd, DynamicWavesMaskColor,  ClearFlag.Color, Color.clear);
                CoreUtils.SetRenderTarget(cmd, DynamicWavesColorData1, ClearFlag.Color, Color.clear);
                CoreUtils.SetRenderTarget(cmd, DynamicWavesColorData2, ClearFlag.Color, Color.clear);


                "ShorelineWavesPass".WaterLog(DynamicWavesMaskColor, DynamicWavesColorData1);
            }

            public void InitializeIntersectionTextures()
            {
                var width  = DynamicWaves1.rt.width;
                var height = DynamicWaves1.rt.height;
                DynamicWavesIntersections = KWS_CoreUtils.RTHandles.Alloc(width, height, name: "_dynamicWavesIntersections",  colorFormat: GraphicsFormat.R16_SFloat);
            }
            

            public void InitializePrebakedDepth(Texture depth, Texture depthSDF, Vector3 zonePos, Vector3 zoneSize)
            {
                Depth    = depth;
                DepthSDF = depthSDF;
                //ZonePos = zonePos;
                // ZoneSize = zoneSize;

                // NearFarSizeXZ = new Vector4(zonePos.y + zoneSize.y * 0.5f, zoneSize.y, zoneSize.x, zoneSize.z);
                //TextureSize = new Vector2Int(Depth.width, Depth.height);
            }

            public void InitializePrebakedSimData(Texture dynamicWaves)
            {
                var cmd = new CommandBuffer();
                cmd.Blit(dynamicWaves, DynamicWaves1);
                cmd.Blit(dynamicWaves, DynamicWaves2);
                Graphics.ExecuteCommandBuffer(cmd);

                CurrentFrame = 3;
                this.WaterLog("InitializePrebakedSimData");
            }
            
            public void PostprocessPrebakedSimData(KWS_DynamicWavesSimulationZone zone)
            {
                var cmd    = new CommandBuffer();

                var target = GetTarget.rt;
                var tempRT = Shader.PropertyToID("KWS_PrebakedTempRT");
                cmd.GetTemporaryRT(tempRT, target.width, target.height, 0, FilterMode.Bilinear, target.graphicsFormat);
                
                UpdateSimulationShaderParams(cmd, zone);
                cmd.BlitTriangle(target, tempRT, _dynamicWavesMaterial, 5);
                cmd.Blit(tempRT, target);
                
                cmd.ReleaseTemporaryRT(tempRT);
                Graphics.ExecuteCommandBuffer(cmd);

                
            }

            public void InitializeParticlesBuffers(bool useFoamParticles, int maxFoamParticles, bool useSplashParticles, int maxSplashParticles)
            {
                if (useFoamParticles)
                {
                    FoamParticlesData.ReleaseParticlesBuffers();
                    FoamParticlesData.InitializeParticlesBuffers<KWS_DynamicWavesHelpers.FoamParticle>(maxFoamParticles, FoamParticlesShaderName, 
                                                                                                       FoamComputeShaderKeyword, isTriangle: false, timeSlicedFrames: 4);
                }

                if (useSplashParticles)
                {
                    SplashParticlesData.ReleaseParticlesBuffers();
                    SplashParticlesData.InitializeParticlesBuffers<KWS_DynamicWavesHelpers.SplashParticle>(maxSplashParticles, SplashParticlesShaderName, 
                                                                                                           SplashComputeShaderKeyword, isTriangle: true, timeSlicedFrames: 1);
                }
            }


            public void InitializeWetDecalData()
            {
            }

            public void SwapSimulationBuffers()
            {
                CurrentFrame++;
            }


            public void Release()
            {
                DynamicWaves1?.Release();
                DynamicWaves2?.Release();
                DynamicWavesAdditionalData1?.Release();
                DynamicWavesAdditionalData2?.Release();
                DynamicWavesNormals?.Release();
                DynamicWavesMask?.Release();
                DynamicWavesMaskDepth?.Release();

                DynamicWavesColorData1?.Release();
                DynamicWavesColorData2?.Release();
                DynamicWavesMaskColor?.Release();
                
                DynamicWavesAdvectedUV1?.Release();
                DynamicWavesAdvectedUV2?.Release();
                
                DynamicWavesIntersections?.Release();
              
                DynamicWaves1           = DynamicWaves2           = DynamicWavesAdditionalData1 = DynamicWavesAdditionalData2 = DynamicWavesNormals = DynamicWavesMask = DynamicWavesMaskDepth = null;
                DynamicWavesAdvectedUV1 = DynamicWavesAdvectedUV2 = null;
              
                DynamicWavesColorData1    = DynamicWavesColorData2 = DynamicWavesMaskColor = null;
                DynamicWavesIntersections = null;

                FoamParticlesData.ReleaseParticlesBuffers();
                SplashParticlesData.ReleaseParticlesBuffers();


                CurrentFrame = 0;

                this.WaterLog(string.Empty, KW_Extensions.WaterLogMessageType.ReleaseRT);
            }
        }
    }
}