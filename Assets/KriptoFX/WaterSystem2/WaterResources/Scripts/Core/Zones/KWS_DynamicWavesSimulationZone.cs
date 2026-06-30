using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

#if UNITY_EDITOR
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("KriptoFX.KWS2.Editor")]
#endif

namespace KWS
{
    [ExecuteInEditMode]
    public class KWS_DynamicWavesSimulationZone : MonoBehaviour, KWS_TileZoneManager.IWaterZone
    {
        public                                        SimulationZoneTypeMode ZoneType = SimulationZoneTypeMode.StaticZone;
        [FormerlySerializedAs("FollowObject")] public GameObject             FollowTarget;

        public LayerMask IntersectionLayerMask          = ~(1 << KWS_Settings.Water.WaterLayer);
        public float     SimulationResolutionPerMeter   = 3.5f;
        public float     SimulationWaveSpeedMultiplier  = 0.7f;
        public float     FlowSpeedMultiplier            = 0.75f;
        public int       SimulationFPS                  = 60;
        public float     OceanWavesInfluenceStrength    = 1.0f;
        public float     ShorelineDistance              = 25;
        public float     EvaporationRate                = 0.05f;
        public float     RainDropsIntensity           = 0;
        public bool      ReceiveWaterFlowFromOtherZones = false;

        public bool                                          UseFoamTexture              = true;
        public FoamTypeEnum                                  FoamType                    = FoamTypeEnum.FlowMap;
        public WaterQualityLevelSettings.FoamTextureTypeEnum FoamTextureType             = WaterQualityLevelSettings.FoamTextureTypeEnum.Type2;
        public float                                         FoamTextureContrast         = 2.25f;
        public float                                         FoamTextureScaleMultiplier  = 1f;
        public float                                         FoamAlpha                   = 1.0f;
        
        public float TurbulenceFoamStrength = 0.2f;
        public float WaveCrestFoamStrength  = 0.5f;
        public float ShorelineFoamStrength  = 0.5f;
        public float FoamDisappearSpeed     = 0.75f;

        public bool                      UseFoamParticles             = true;
        public FoamParticlesMaxLimitEnum MaxFoamParticlesBudget       = FoamParticlesMaxLimitEnum._1Million;
        public float                     MaxFoamRenderingDistance     = 200;
        public float                     FoamParticlesScale           = 1.0f;
        public float                     FoamParticlesAlphaMultiplier = 0.7f;
        public float                     FoamEmissionRate             = 0.5f;
        public float                     FoamClumping                 = 0.4f;
        public float                     FoamParticleLifetime         = 1.5f;
        public bool                      UsePhytoplanktonEmission     = false;

        public bool                        UseSplashParticles             = true;
        public SplashParticlesMaxLimitEnum MaxSplashParticlesBudget       = SplashParticlesMaxLimitEnum._15k;
        public float                       MaxSplashRenderingDistance     = 400;
        public float                       SplashParticlesScale           = 0.6f;
        public float                       SplashParticlesAlphaMultiplier = 0.6f;
        public float                       SplashEmissionRate             = 0.5f;
        public float                       BakedSplashEmissionRate        = 1.0f;


        public SplashCasticShadowModeEnum  CastShadowMode    = SplashCasticShadowModeEnum.Disabled;
        public SplashReceiveShadowModeEnum ReceiveShadowMode = SplashReceiveShadowModeEnum.DirectionalHighQuality;


        public Vector2Int TextureSize             => BakedTextureSize;
        public Vector3    Position                => BakedAreaPos;
        public Vector3    Size                    => BakedAreaSize;
        public Quaternion Rotation                => BakedAreaRotation;
        public Vector4    RotationMatrix          => BakedRotationMatrix;
        //public Bounds     Bounds                  => bakedBounds;
        public float      ClosetsDistanceToCamera { get; set; }
        public bool       IsZoneVisible           => _IsZoneVisible;
        public bool       IsZoneInitialized       => _isInitialized;

        /// <summary>
        /// Returns the current simulation data texture (RGBA16 format unsigned normalized [0-1]).
        /// Each channel stores the following encoded values:
        /// - R: Velocity X, encoded from [-10, 10] range
        /// - G: Velocity Z, encoded from [-10, 10] range
        /// - B: Water Height Offset relative to terrain, encoded from [-2, 20] range
        /// - A: Terrain World Height, normalized by distance from water to zone top
        /// 
        /// Use the following logic to decode:
        /// Velocity.xy = (RG - 0.5) * 20.0
        /// Height      = B * 22.0 - 2.0
        /// Depth       = A * abs(WaterPosY - (ZonePosY + ZoneSizeY * 0.5))
        /// </summary>
        public RenderTexture GetSimulationDataTexture => _simulationData?.GetTarget.rt;

        /// <summary>
        /// Returns the additional simulation data texture in RGBA8 format (unorm).
        /// Each channel contains the following data:
        ///
        /// - R: Wetmap - surface wetness mask (used for darkening wet surfaces over time)
        /// - G: Shoreline Mask Fade - Signed Distance Field (SDF) mask for shore fading
        /// - B: Foam Mask - intensity of generated foam in the water
        /// - A: Wetmap Depth - height of wetting effect (used to discard objects above water level)
        /// </summary>
        public RenderTexture GetSimulationAdditionalDataTexture => _simulationData?.GetAdditionalTarget.rt;

        public RenderTexture GetSimulationNormal                 => _simulationData?.GetNormals.rt;
        public RenderTexture GetSimulationColor                  => _simulationData?.GetColorTarget;
        public RenderTexture GetSimulationEffectorsData          => _simulationData?.DynamicWavesMask;
        public RenderTexture GetSimulationObstacleEffectorsDepth => _simulationData?.DynamicWavesMaskDepth;
        public Texture       GetSimulationSceneIntersectionDepth => _simulationData?.Depth;
        public Texture       GetSimulationSceneIntersectionDepthSDF => _simulationData?.DepthSDF;
        public bool          IsBaking                            => _isBakeMode;

        [SerializeField] internal bool ShowSimulationSettings    = true;
        [SerializeField] internal bool ShowFoamTextureSettings   = false;
        [SerializeField] internal bool ShowFoamParticlesSettings = false;
        [SerializeField] internal bool ShowSplashSettings        = false;

        int KWS_TileZoneManager.IWaterZone.                                   ID                 { get; set; }
        Bounds KWS_TileZoneManager.IWaterZone.                                OrientedBounds     => bakedOrientedBounds;
        KWS_TileZoneManager.PrecomputedOBBZone KWS_TileZoneManager.IWaterZone.PrecomputedObbZone => _precomputedObbZone;

        internal List<KWS_DynamicWavesSimulationZone> _intersectedZones = new  List<KWS_DynamicWavesSimulationZone>();

        // bool 

        [Space] public Texture2D SavedDepth;
        public         Texture2D SavedDistanceField;
        public         Texture2D SavedDynamicWavesSimulation;
        public         Texture2D SavedDynamicWavesAdditionalDataSimulation;
        public         Mesh      SavedMesh;
        public         Material  SavedMeshMaterial;
        public         Mesh      SavedSplashMesh;
        public         Material  SavedSplashMaterial;

        [SerializeField] internal Vector3    BakedAreaPos;
        [SerializeField] internal Vector3    BakedAreaSize;
        [SerializeField] internal Quaternion BakedAreaRotation;
        [SerializeField] internal Vector4    BakedRotationMatrix;
        [SerializeField] internal Vector4    BakedNearFarSizeXZ;
        [SerializeField] internal Vector2Int BakedTextureSize;
        [SerializeField] internal Bounds     bakedBounds;
        [SerializeField] internal Bounds     bakedOrientedBounds;
        [SerializeField] internal float      bakedSimSize;
        [SerializeField] internal float      bakedWaterLevel = float.MinValue;

        internal bool _isBakeMode;
        internal bool _isPreviewMode => SavedMesh == null;

        internal static bool IsAnyZoneInBakeMode;
        internal static bool IsAnyZoneRequireUpdateIntersections;
       
        internal Vector3                       _lastRenderedDynamicPosition;
        KWS_TileZoneManager.PrecomputedOBBZone _precomputedObbZone;

        private  Vector3 _getSafeScale => Vector3.Max(transform.localScale, new Vector3(2, 2, 2));

        internal DynamicWavesPass.SimulationData _simulationData = new DynamicWavesPass.SimulationData();

        private  Material      _sdfBakeMaterial;
        private  CommandBuffer _bakeCmd;
        private  Camera        _bakeDepthCamera;
        private  GameObject    _bakeDepthCameraGO;
        internal RenderTexture _bakeDepthRT;
        internal RenderTexture _bakeDepthSdfRT;
        private  GameObject    _bakedMeshGO;

        private GameObject                 _bakedSplashParticleSystemGO;
        private ParticleSystem             _bakedSplashParticleSystem;
        private ParticleSystem.MinMaxCurve _bakedSplashStartCurveSize;

        MeshFilter        _bakedMeshFilter;
        MeshRenderer      _bakedMeshRenderer;
        internal Mesh     _bakedMeshPreview;
        private  Material _bakedMeshPreviewMaterial;

       

        float        _lastResolutionPerMeter;
        bool         _lastUseFoamParticles;
        bool         _lastUseSplashParticles;
        int          _lastLayerMask;
        FoamTypeEnum _lastFoamType;

        FoamParticlesMaxLimitEnum   _lastFoamParticlesMaxLimit;
        SplashParticlesMaxLimitEnum _lastSplashParticlesMaxLimit;

        internal int  CurrentSkippedFrames;
        internal int  MaxSkippedFrames;
        internal bool RequireColorRendering;
        bool          _isInitialized;
        bool          _isInitializedSDF;

        private       bool  _IsZoneVisible;
        
        
        internal        int   MaxSimulationTexturePixels = 2048 * 1024;
        private const   float sdfScaleResolution         = 0.25f;
        internal static float DefaultFPS                 = 60f;
        private const   int   _maxAllowedFrames          = 2;
        
        private  float _leftTime;
        internal int   CurrentFixedUpdateFramesCount;
        internal float CurrentTimeScale => SimulationFPS / DefaultFPS;

        
        [SerializeField] internal bool ResetCachePath = false;


        private static readonly int KwsSplashParticlesAlphaMultiplier = Shader.PropertyToID("KWS_SplashParticlesAlphaMultiplier");

        public enum FoamParticlesMaxLimitEnum
        {
            #if KWS_EXTREME
            _5Million = 5000000,
            #endif
            _3Million = 3000000,
            _2Million = 2000000,
            _1Million = 1000000,
            _500k     = 500000,
            _250k     = 250000,
            _100k     = 100000
        }

        public enum SplashParticlesMaxLimitEnum
        {
            _50k = 50000,
            _25k = 25000,
            _15k = 15000,
            _5k  = 5000,
        }

        public enum SplashCasticShadowModeEnum
        {
            Disabled,
            LowQuality,
            HighQuality
        }

        public enum SplashReceiveShadowModeEnum
        {
            Disabled,
            DirectionalLowQuality,
            DirectionalHighQuality,
            AllShadowsLowQuality,
            AllShadowsHighQuality,
        }

        public enum SimulationZoneTypeMode
        {
            StaticZone,
            MovableZone,
            BakedSimulation
        }

        public enum FoamTypeEnum
        {
            FlowMap,
            Advected,
        }

        public void ForceUpdateZone(bool resetSimulation = true)
        {
            InitializeNewZone(resetSimulation);
            IsAnyZoneRequireUpdateIntersections = true;
        }

        public void StartRuntimeBaking()
        {
            _isBakeMode                                   = true;
            IsAnyZoneInBakeMode = true;
            ForceUpdateZone();
        }

        public void StopRuntimeBaking()
        {
            _isBakeMode                                   = false;
            IsAnyZoneInBakeMode = false;
            WaterSystem.GlobalTimeScale = 1;
        }

        internal bool IsZoneAllowed()
        {
            var scale = _getSafeScale;
            return (scale.x * SimulationResolutionPerMeter) * (scale.z * SimulationResolutionPerMeter) <= MaxSimulationTexturePixels;
        }

        void InitializeNewZone(bool resetSimulation)
        {
            if (WaterSystem.Instance == null)
            {
                //Debug.LogWarning("Water System Manager is not initialized.");
                return;
            }

            BakedAreaPos                 = transform.position;
            _lastRenderedDynamicPosition = BakedAreaPos;
            
            BakedAreaSize                = _getSafeScale;
            BakedAreaRotation            = transform.rotation;
            BakedTextureSize             = new Vector2Int(Mathf.CeilToInt(BakedAreaSize.x * SimulationResolutionPerMeter / 4f) * 4, Mathf.CeilToInt(BakedAreaSize.z * SimulationResolutionPerMeter / 4f) * 4);
            bakedBounds                  = new Bounds(BakedAreaPos, BakedAreaSize);
            bakedOrientedBounds          = KW_Extensions.GetOrientedBounds(BakedAreaPos, BakedAreaSize, BakedAreaRotation);
            CachePrecomputedOBBZone();
            bakedSimSize    = SimulationResolutionPerMeter;
            bakedWaterLevel = WaterSystem.Instance.WaterLevel;
            var angleRad = BakedAreaRotation.eulerAngles.y * Mathf.Deg2Rad;
            var cos      = Mathf.Cos(angleRad);
            var sin      = Mathf.Sin(angleRad);
            BakedRotationMatrix = new Vector4(cos, sin, -sin, cos);

            if (ZoneType != SimulationZoneTypeMode.MovableZone)
            {
                BakeDepth(BakedTextureSize.x, BakedTextureSize.y);
                if (bakedWaterLevel > float.MinValue) SdfDepth(bakedWaterLevel);
            }

            if (resetSimulation)
            {
                _simulationData.Release();

                _simulationData.InitializeSimTextures(BakedTextureSize.x, BakedTextureSize.y);
                _simulationData.InitializePrebakedDepth(_bakeDepthRT, _bakeDepthSdfRT, BakedAreaPos, BakedAreaSize);
                _simulationData.InitializeWetDecalData();
                _simulationData.InitializeParticlesBuffers(UseFoamParticles, (int)MaxFoamParticlesBudget, UseSplashParticles, (int)MaxSplashParticlesBudget);
            }
            else if (_simulationData.CurrentFrame > 3)
            {
                _simulationData.InitializePrebakedDepth(_bakeDepthRT, _bakeDepthSdfRT, BakedAreaPos, BakedAreaSize);
            }

            if (ZoneType == SimulationZoneTypeMode.BakedSimulation)
            {
                UpdateBakedData();
            }

            _isInitialized = true;
        }

        private void InitializeSavedZone()
        {
            bakedBounds         = new Bounds(BakedAreaPos, BakedAreaSize);
            bakedOrientedBounds = KW_Extensions.GetOrientedBounds(BakedAreaPos, BakedAreaSize, BakedAreaRotation);
            CachePrecomputedOBBZone();

            var angleRad = BakedAreaRotation.eulerAngles.y * Mathf.Deg2Rad;
            var cos      = Mathf.Cos(angleRad);
            var sin      = Mathf.Sin(angleRad);
            BakedRotationMatrix = new Vector4(cos, sin, -sin, cos);

            _simulationData.InitializeSimTextures(BakedTextureSize.x, BakedTextureSize.y);
            _simulationData.InitializePrebakedDepth(SavedDepth, SavedDistanceField, BakedAreaPos, BakedAreaSize);
            _simulationData.InitializeWetDecalData();
            _simulationData.InitializeParticlesBuffers(UseFoamParticles, (int)MaxFoamParticlesBudget, UseSplashParticles, (int)MaxSplashParticlesBudget);

            if (SavedDynamicWavesSimulation != null) _simulationData.InitializePrebakedSimData(SavedDynamicWavesSimulation);

            if (ZoneType == SimulationZoneTypeMode.BakedSimulation)
            {
                UpdateBakedData();
            }

            _isInitialized = true;
        }

        internal void ValueChanged()
        {
            if (Math.Abs(_lastResolutionPerMeter - SimulationResolutionPerMeter) > 0.05f || _lastLayerMask          != IntersectionLayerMask)
            {
                Initialize();
            }

            if (_lastUseFoamParticles   != UseFoamParticles   || _lastFoamParticlesMaxLimit   != MaxFoamParticlesBudget ||
                _lastUseSplashParticles != UseSplashParticles || _lastSplashParticlesMaxLimit != MaxSplashParticlesBudget
             )
            {
                _simulationData.InitializeParticlesBuffers(UseFoamParticles, (int)MaxFoamParticlesBudget, UseSplashParticles, (int)MaxSplashParticlesBudget);
            }


            _lastResolutionPerMeter      = SimulationResolutionPerMeter;
            _lastUseFoamParticles        = UseFoamParticles;
            _lastFoamParticlesMaxLimit   = MaxFoamParticlesBudget;
            _lastUseSplashParticles      = UseSplashParticles;
            _lastSplashParticlesMaxLimit = MaxSplashParticlesBudget;
            _lastLayerMask               = IntersectionLayerMask;
        }
        
        internal void UpdateFixedTimer()
        {
            _leftTime += KW_Extensions.DeltaTime() * WaterSystem.GlobalTimeScale;
            var fixedDelta = 1f / SimulationFPS;
            
            int frames = 0;
            while (_leftTime > 0f)
            {
                _leftTime -= fixedDelta;
                frames++;
                if (frames > _maxAllowedFrames)
                {
                    _leftTime                     = 0;
                    CurrentFixedUpdateFramesCount = frames;
                    return;
                }
            }

            CurrentFixedUpdateFramesCount = frames;
        }


        void KWS_TileZoneManager.IWaterZone.UpdateVisibility(Camera cam)
        {
            _IsZoneVisible = false;

            if (!KWS_UpdateManager.FrustumCaches.TryGetValue(cam, out var cache))
            {
                return;
            }

            var planes = cache.FrustumPlanes;

            if (ZoneType == SimulationZoneTypeMode.MovableZone)
            {
                _IsZoneVisible = KW_Extensions.IsBoxVisibleApproximated(ref planes, bakedOrientedBounds.min, bakedOrientedBounds.max);
            }
            else _IsZoneVisible = KW_Extensions.IsBoxVisibleApproximated(ref planes, bakedOrientedBounds.min, bakedOrientedBounds.max);
        }

        internal bool UpdateAndCheckRender(Camera cam)
        {
            if (!_IsZoneVisible || !_isInitialized) return false;

            if (ZoneType == SimulationZoneTypeMode.BakedSimulation) return true;

            var distanceToAABB = KW_Extensions.DistanceToAABB(cam.transform.position, bakedOrientedBounds.min, bakedOrientedBounds.max);

            var staticZoneMaxDistanceToDropFPS  = 300;
           // var movableZoneMaxDistanceToDropFPS = 300;

            if (ZoneType == SimulationZoneTypeMode.StaticZone)
            {
                var normalizedDistance = Mathf.Clamp01(-0.1f + distanceToAABB * (1f / staticZoneMaxDistanceToDropFPS));
                MaxSkippedFrames = Mathf.RoundToInt(Mathf.Lerp(0, 4, normalizedDistance));
            }
            else if (ZoneType == SimulationZoneTypeMode.MovableZone)
            {
               // var normalizedDistance = Mathf.Clamp01(-0.25f + distanceToAABB * (1f / movableZoneMaxDistanceToDropFPS));
               // MaxSkippedFrames = Mathf.RoundToInt(Mathf.Lerp(0, 3, normalizedDistance));
                MaxSkippedFrames     = 0;
                CurrentSkippedFrames = 0;
                return true;
            }
            else
            {
                Debug.LogError("implement SimulationZoneTypeMode!");
            }

            if (++CurrentSkippedFrames > MaxSkippedFrames) CurrentSkippedFrames = 0;
            return (CurrentSkippedFrames == 0);
        }


        void Initialize()
        {
            if (SavedDepth != null && (ZoneType == SimulationZoneTypeMode.StaticZone || ZoneType == SimulationZoneTypeMode.BakedSimulation))
            {
                InitializeSavedZone();
            }
            else
            {
                InitializeNewZone(true);
            }

            IsAnyZoneRequireUpdateIntersections =  true;
           
        }

        private Vector3 _lastShift;
        void OnOriginShifted(Vector3 shift)
        {
            _lastShift = shift;
             transform.position                    -= shift;
             _lastRenderedDynamicPosition -= shift;
             UpdateMovableZoneCache();
        }

        internal void ForceUpdateZoneIntersections()
        {
            _intersectedZones.Clear();
            var zones         = KWS_TileZoneManager.VisibleDynamicWavesZones;
          
            foreach (var waterZone in zones)
            {
                var zone = (KWS_DynamicWavesSimulationZone)waterZone;
                if(zone.ZoneType != SimulationZoneTypeMode.StaticZone || zone == this) continue;
                
                if (bakedOrientedBounds.Intersects(waterZone.OrientedBounds))
                {
                    _intersectedZones.Add(zone);
                }
            }
        }


        internal void CachePrecomputedOBBZone()
        {
            var     bounds   = bakedBounds;
            Vector2 center   = new Vector2(bounds.center.x, bounds.center.z);
            Vector2 halfSize = new Vector2(bounds.size.x, bounds.size.z) * 0.5f;

            float   angleRad  = transform.rotation.eulerAngles.y * Mathf.Deg2Rad;
            float   cos       = Mathf.Cos(angleRad);
            float   sin       = Mathf.Sin(angleRad);
            Vector4 rotMatrix = new Vector4(cos, -sin, sin, cos);

            _precomputedObbZone           = new KWS_TileZoneManager.PrecomputedOBBZone();
            _precomputedObbZone.Center    = center;
            _precomputedObbZone.Axis      = new Vector2[2];
            _precomputedObbZone.Axis[0]   = new Vector2(rotMatrix.x, rotMatrix.y); // right
            _precomputedObbZone.Axis[1]   = new Vector2(rotMatrix.z, rotMatrix.w); // forward
            _precomputedObbZone.HalfSize  = halfSize;
            _precomputedObbZone.RotMatrix = rotMatrix;

            Vector2 bX = new Vector2(1, 0);
            Vector2 bY = new Vector2(0, 1);

            _precomputedObbZone.Extents    = new float[2];
            _precomputedObbZone.Extents[0] = Mathf.Abs(Vector2.Dot(_precomputedObbZone.Axis[0] * halfSize.x, bX)) + Mathf.Abs(Vector2.Dot(_precomputedObbZone.Axis[1] * halfSize.y, bX));
            _precomputedObbZone.Extents[1] = Mathf.Abs(Vector2.Dot(_precomputedObbZone.Axis[0] * halfSize.x, bY)) + Mathf.Abs(Vector2.Dot(_precomputedObbZone.Axis[1] * halfSize.y, bY));
        }


        void OnEnable()
        {
            KWS_TileZoneManager.DynamicWavesZones.Add(this);
            WaterSystem.OnOriginShifted         += OnOriginShifted; 
            //Invoke(nameof(LateOnEnable), 0.01f);
        }

        internal void ManualInitialize()
        {
            transform.hasChanged  = false;
            _IsZoneVisible        = false;
            _isInitialized        = false;
            _isInitializedSDF     = false;
            _isBakeMode           = false;
            IsAnyZoneInBakeMode   = false;
            RequireColorRendering = false;

            Initialize();

            _lastResolutionPerMeter      = SimulationResolutionPerMeter;
            _lastUseFoamParticles        = UseFoamParticles;
            _lastFoamParticlesMaxLimit   = MaxFoamParticlesBudget;
            _lastUseSplashParticles      = UseSplashParticles;
            _lastSplashParticlesMaxLimit = MaxSplashParticlesBudget;
            _lastLayerMask               = IntersectionLayerMask;
        }

        void OnDisable()
        {
            _isInitialized    = false;
            _isInitializedSDF = false;


            KWS_TileZoneManager.DynamicWavesZones.Remove(this);
            WaterSystem.OnOriginShifted         -= OnOriginShifted; 
            _simulationData.Release();

            ReleaseTextures();
            KW_Extensions.SafeDestroy(_sdfBakeMaterial, _bakeDepthCameraGO, _bakedMeshGO, _bakedMeshPreview, _bakedMeshPreviewMaterial, _bakedSplashParticleSystemGO);
            _sdfBakeMaterial             = null;
            _bakeDepthCameraGO           = null;
            _bakedMeshGO                 = null;
            _bakedMeshPreview            = null;
            _bakedMeshPreviewMaterial    = null;
            _bakedSplashParticleSystemGO = null;

        }

        internal void UpdateMovableZoneCache()
        {
            if (ZoneType == SimulationZoneTypeMode.MovableZone)
            {
                if(FollowTarget != null) transform.position = FollowTarget.transform.position;

                BakedAreaPos      = transform.position;
                BakedAreaRotation = transform.rotation;
                BakedAreaSize     = _getSafeScale;
                bakedBounds       = new Bounds(BakedAreaPos, BakedAreaSize);

                BakedNearFarSizeXZ = new Vector4(BakedAreaPos.y + BakedAreaSize.y * 0.5f, BakedAreaSize.y, BakedAreaSize.x, BakedAreaSize.z);

                bakedOrientedBounds = KW_Extensions.GetOrientedBounds(BakedAreaPos, BakedAreaSize, BakedAreaRotation);
                CachePrecomputedOBBZone();

                var angleRad = BakedAreaRotation.eulerAngles.y * Mathf.Deg2Rad;
                var cos      = Mathf.Cos(angleRad);
                var sin      = Mathf.Sin(angleRad);
                BakedRotationMatrix = new Vector4(cos, sin, -sin, cos);
            }

        }
        
        
        void LateUpdate()
        {

#if UNITY_EDITOR
            //by some reason unity resets serialized material links after ctrl + s
            if (ZoneType == SimulationZoneTypeMode.BakedSimulation && SavedMeshMaterial != null)
            {
                DynamicWavesPass.UpdateSimulationShaderParams(SavedMeshMaterial, this);
            }
#endif
        }
        

        void BakeDepth(int simulationWidth, int simulationHeight)
        {
            var zonePos  = transform.position;
            var zoneSize = _getSafeScale;

            var camPos   = new Vector3(zonePos.x, zonePos.y + zoneSize.y * 0.5f, zonePos.z);
            var areaSize = Mathf.Max(zoneSize.x, zoneSize.z);
            var far      = zoneSize.y;
            BakedNearFarSizeXZ = new Vector4(camPos.y, far, zoneSize.x, zoneSize.z);

            if (ZoneType == SimulationZoneTypeMode.MovableZone) return;

            if (_sdfBakeMaterial == null) _sdfBakeMaterial = KWS_CoreUtils.CreateDepthSdfMaterial();
            if (_bakeCmd         == null) _bakeCmd         = new CommandBuffer() { name = "ComputeDepthSDF" };

            if (_bakeDepthCameraGO == null)
            {
                _bakeDepthCameraGO                         = ReflectionUtils.CreateDepthCamera("Bake Ortho Depth Camera", out _bakeDepthCamera);
                _bakeDepthCameraGO.transform.parent        = transform;
                _bakeDepthCameraGO.transform.localRotation = Quaternion.Euler(90, 0, 0);
                _bakeDepthCameraGO.transform.localScale    = Vector3.one;

#if KWS_DEBUG
                _bakeDepthCameraGO.hideFlags = HideFlags.DontSave;
#else
                _bakeDepthCameraGO.hideFlags = HideFlags.HideAndDontSave;
#endif
            }

            var simulationWidthSDF  = Mathf.CeilToInt(sdfScaleResolution * zoneSize.x * SimulationResolutionPerMeter / 4f) * 4;
            var simulationHeightSDF = Mathf.CeilToInt(sdfScaleResolution * zoneSize.z * SimulationResolutionPerMeter / 4f) * 4;

            simulationWidthSDF  = Mathf.Min(512, simulationWidthSDF);
            simulationHeightSDF = Mathf.Min(512, simulationHeightSDF);

            if (_bakeDepthRT    != null) RenderTexture.ReleaseTemporary(_bakeDepthRT);
            if (_bakeDepthSdfRT != null) RenderTexture.ReleaseTemporary(_bakeDepthSdfRT);

            _bakeDepthRT    = RenderTexture.GetTemporary(simulationWidth,    simulationHeight,    24, RenderTextureFormat.Depth);
            _bakeDepthSdfRT = RenderTexture.GetTemporary(simulationWidthSDF, simulationHeightSDF, 0,  GraphicsFormat.R16G16B16A16_SFloat);

            _bakeDepthCamera.transform.position = camPos;
            _bakeDepthCamera.orthographicSize   = areaSize * 0.5f;
            _bakeDepthCamera.nearClipPlane      = 0.01f;
            _bakeDepthCamera.farClipPlane       = far;
            _bakeDepthCamera.cullingMask        = IntersectionLayerMask;
            _bakeDepthCamera.aspect             = (float)simulationWidth / simulationHeight;
            if (_bakeDepthCamera.aspect > 1.0) _bakeDepthCamera.orthographicSize /= _bakeDepthCamera.aspect;

            KWS_CoreUtils.RenderDepth(_bakeDepthCamera, _bakeDepthRT);
        }

        void SdfDepth(float waterLevel)
        {
            var zoneSize = _getSafeScale;
            var areaSize = Mathf.Max(zoneSize.x, zoneSize.z);

            KWS_CoreUtils.ComputeSDF(_bakeCmd, _sdfBakeMaterial, areaSize, BakedNearFarSizeXZ, waterLevel, _bakeDepthRT, _bakeDepthSdfRT);
            _isInitializedSDF = true;
        }


        void ReleaseTextures()
        {
            if (_bakeDepthRT    != null) RenderTexture.ReleaseTemporary(_bakeDepthRT);
            if (_bakeDepthSdfRT != null) RenderTexture.ReleaseTemporary(_bakeDepthSdfRT);

            _bakeDepthRT = _bakeDepthSdfRT = null;


            this.WaterLog(string.Empty, KW_Extensions.WaterLogMessageType.ReleaseRT);
        }


        #region BakedSimulation

        void InitializePreviewData()
        {
            var currentVertices = TextureSize.x * TextureSize.y;
            if (_bakedMeshPreview == null || _bakedMeshPreview != null && currentVertices != _bakedMeshPreview.vertices.Length)
            {
                KW_Extensions.SafeDestroy(_bakedMeshPreview);
                _bakedMeshPreview = MeshUtils.CreateZoneMesh(TextureSize);
            }

            if (_bakedMeshPreviewMaterial == null) _bakedMeshPreviewMaterial = KWS_CoreUtils.CreateMaterial(KWS_ShaderConstants.ShaderNames.WaterZoneInstanceShaderName, useWaterStencilMask: true);
        }


        internal void UpdateBakedData()
        {
            if (_bakedMeshGO == false)
            {
                _bakedMeshGO                  = new GameObject("BakedMeshRenderer");
                _bakedMeshGO.transform.parent = transform;

                _bakedMeshGO.transform.position      = new Vector3(transform.position.x, bakedWaterLevel, transform.position.z);
                _bakedMeshGO.transform.localRotation = Quaternion.identity;
                _bakedMeshGO.transform.localScale    = Vector3.one;

                _bakedMeshGO.layer = KWS_Settings.Water.WaterLayer;

#if KWS_DEBUG
                _bakedMeshGO.hideFlags = HideFlags.DontSave;
#else
                _bakedMeshGO.hideFlags = HideFlags.HideAndDontSave;
#endif
                _bakedMeshFilter   = _bakedMeshGO.AddComponent<MeshFilter>();
                _bakedMeshRenderer = _bakedMeshGO.AddComponent<MeshRenderer>();
            }


            var currentMesh     = SavedMesh;
            var currentMaterial = SavedMeshMaterial;

            if (SavedMesh == null || currentMaterial == null)
            {
                InitializePreviewData();
                currentMesh     = _bakedMeshPreview;
                currentMaterial = _bakedMeshPreviewMaterial;
            }

            DynamicWavesPass.UpdateZoneShaderParams(currentMaterial, this);
            DynamicWavesPass.UpdateSimulationShaderParams(currentMaterial, this);


            _bakedMeshFilter.sharedMesh       = currentMesh;
            _bakedMeshRenderer.sharedMaterial = currentMaterial;

            if (UseSplashParticles)
            {
                if (_bakedSplashParticleSystemGO == null)
                {
                    _bakedSplashParticleSystemGO = Instantiate(Resources.Load<GameObject>(KWS_Settings.ResourcesPaths.KWS_BakedSplashParticles), transform);

                    _bakedSplashParticleSystemGO.transform.position      = new Vector3(transform.position.x, bakedWaterLevel, transform.position.z);
                    _bakedSplashParticleSystemGO.transform.localRotation = Quaternion.identity;
                    _bakedSplashParticleSystemGO.transform.localScale    = Vector3.one;
                    _bakedSplashParticleSystemGO.layer                   = KWS_Settings.Water.WaterLayer;
#if KWS_DEBUG
                    _bakedSplashParticleSystemGO.hideFlags = HideFlags.DontSave;
#else
                    _bakedSplashParticleSystemGO.hideFlags = HideFlags.HideAndDontSave;
#endif

                    _bakedSplashParticleSystem = _bakedSplashParticleSystemGO.GetComponent<ParticleSystem>();
                    _bakedSplashStartCurveSize = _bakedSplashParticleSystem.main.startSize;

                    var shapeModule = _bakedSplashParticleSystem.shape;
                    shapeModule.enabled = false;

#if UNITY_EDITOR
                    var editorAsm   = typeof(UnityEditor.SceneView).Assembly;
                    var psUtilsType = editorAsm.GetType("UnityEditor.ParticleSystemEditorUtils");
                    var previewProp = psUtilsType.GetProperty("previewLayers", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    previewProp.SetValue(null, 1u << KWS_Settings.Water.WaterLayer);
#endif
                }

                if (SavedSplashMesh)
                {
                    var shapeModule = _bakedSplashParticleSystem.shape;
                    shapeModule.mesh    = SavedSplashMesh;
                    shapeModule.enabled = true;

                    _bakedSplashParticleSystem.GetComponent<Renderer>().sharedMaterial = SavedSplashMaterial;
                    UpdateSplashParticlesParams();
                }
            }

            if (_bakedSplashParticleSystemGO) CheckAndEnableParticles();
        }

        void CheckAndEnableParticles()
        {
            if (UseSplashParticles == false)
            {
                _bakedSplashParticleSystemGO.SetActive(false);
                return;
            }
            else _bakedSplashParticleSystemGO.SetActive(true);

        }

        void UpdateSplashParticlesParams()
        {


            var psRendered = _bakedSplashParticleSystem.GetComponent<Renderer>();

            var mat = SavedSplashMaterial;

            mat.SetFloat(KwsSplashParticlesAlphaMultiplier, SplashParticlesAlphaMultiplier);

            var isVertexShadow  = ReceiveShadowMode is SplashReceiveShadowModeEnum.DirectionalLowQuality or SplashReceiveShadowModeEnum.AllShadowsLowQuality;
            var isDirShadowOnly = ReceiveShadowMode is SplashReceiveShadowModeEnum.DirectionalLowQuality or SplashReceiveShadowModeEnum.DirectionalHighQuality;
            var isAllShadows    = ReceiveShadowMode is SplashReceiveShadowModeEnum.AllShadowsLowQuality or SplashReceiveShadowModeEnum.AllShadowsHighQuality;

            mat.SetKeyword("KWS_USE_PER_VERTEX_SHADOWS",      isVertexShadow);
            mat.SetKeyword("KWS_USE_DIR_SHADOW",              isDirShadowOnly);
            mat.SetKeyword("KWS_USE_ALL_SHADOWS",             isAllShadows);
            mat.SetKeyword("KWS_USE_SPLASH_SHADOW_CAST_FAST", CastShadowMode == SplashCasticShadowModeEnum.LowQuality);

            psRendered.shadowCastingMode = CastShadowMode == SplashCasticShadowModeEnum.Disabled ? ShadowCastingMode.Off : ShadowCastingMode.On;

            var                        main      = _bakedSplashParticleSystem.main;
            ParticleSystem.MinMaxCurve curveSize = main.startSize;
            curveSize.constantMin = _bakedSplashStartCurveSize.constantMin * SplashParticlesScale;
            curveSize.constantMax = _bakedSplashStartCurveSize.constantMax * SplashParticlesScale;
            main.startSize        = curveSize;
            main.maxParticles     = (int)((int)MaxSplashParticlesBudget * 0.3f);
            main.simulationSpeed  = FlowSpeedMultiplier;

            var maxStartLifetime = main.startLifetime.constantMax;
            var maxRateParticles = main.maxParticles / maxStartLifetime;

            var emission     = _bakedSplashParticleSystem.emission;
            var rateOverTime = emission.rateOverTime;
            rateOverTime.constant = maxRateParticles * BakedSplashEmissionRate;
            emission.rateOverTime = rateOverTime;

            // Debug.Log("UpdateSplashParticlesParams");
        }

        internal void ReleaseBakedData()
        {
            KW_Extensions.SafeDestroy(_bakedMeshGO, _bakedMeshPreview, _bakedSplashParticleSystemGO, _bakedMeshPreviewMaterial);
            _bakedMeshPreviewMaterial    = null;
            _bakedMeshGO                 = null;
            _bakedMeshPreview            = null;
            _bakedSplashParticleSystemGO = null;
        }

        #endregion

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            transform.localScale = _getSafeScale;

            var angles                    = transform.rotation.eulerAngles;
            angles.x           = angles.z = 0;
            transform.rotation = Quaternion.Euler(angles);

            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = new Color(0.15f, 0.35f, 1, 0.99f);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

            Gizmos.color = new Color(0.15f, 0.35f, 1, 0.25f);
            Gizmos.DrawCube(Vector3.zero, Vector3.one);

            if ((ZoneType == SimulationZoneTypeMode.StaticZone      && SavedDynamicWavesSimulation == null)
             || (ZoneType == SimulationZoneTypeMode.BakedSimulation && !SavedMesh))
            {
                if (transform.hasChanged)
                {
                    transform.hasChanged = false;

                    if (!Application.isPlaying) UnityEditor.EditorApplication.delayCall += () => ForceUpdateZone(); //avoid Recursive rendering error
                }
            }
        }


        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = new Color(0.15f, 0.35f, 1, 0.99f);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }

#endif

    }
}
