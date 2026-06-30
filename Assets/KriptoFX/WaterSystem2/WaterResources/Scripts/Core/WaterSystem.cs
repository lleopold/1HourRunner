using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

using System.IO;
using UnityEngine.Rendering;
using static KWS.WaterQualityLevelSettings;

#if UNITY_EDITOR
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("KriptoFX.KWS2.Ocean.Editor")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("KriptoFX.KWS2.Ocean.Runtime")]
#endif

namespace KWS
{
    [ExecuteAlways]
    [Serializable]
    [AddComponentMenu("")]
    public partial class WaterSystem : MonoBehaviour
    {
        public static WaterSystem Instance { get; private set; }
        public static WaterQualityLevelSettings QualitySettings => KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;

        public static Action<WaterSystem.WaterSettingsCategory> OnAnyWaterSettingsChanged;

  
        //Color settings
        public float Transparent = 10;
        public Color WaterColor = new Color(1, 1, 1, 1);
        public Color TurbidityColor = new Color(7 / 255.0f, 65 / 255.0f, 80 / 255.0f);

       
        //Reflection
        public QualityOverrideEnum ScreenSpaceReflection = QualityOverrideEnum.UseQualitySettings;
        public QualityOverrideEnum PlanarReflection      = QualityOverrideEnum.UseQualitySettings;
        
        public float AnisotropicReflectionsScale      = 0.5f;
  
        
        public bool  OverrideSkyColor               = false;
        public Color CustomSkyColor                 = Color.gray;
        
        public bool  ReflectSun                     = true;
        public float ReflectedSunCloudinessStrength = 0.04f;
        public float ReflectedSunStrength           = 1.0f;


        //Refraction
        public RefractionModeEnum  RefractionMode             = RefractionModeEnum.PhysicalAproximationIOR;
        public float                                         RefractionSimpleStrength   = 0.25f;
        public QualityOverrideEnum RefractionDispersion       = QualityOverrideEnum.UseQualitySettings;

        public float                                        RefractionDispersionStrength = 0.35f;


        //Wet
        public QualityOverrideEnum WetEffect               = QualityOverrideEnum.UseQualitySettings;
        public float               WetStrength             = 1.0f;
        public float               WetnessHeightAboveWater = 2.0f;
#if UNITY_6000_0_OR_NEWER && (KWS_URP)
                public RenderingLayerMask  WetDecalLayerMask       = RenderingLayerMask.defaultRenderingLayerMask;
#endif
        
#if UNITY_6000_0_OR_NEWER && (KWS_HDRP)
                public UnityEngine.Rendering.HighDefinition.RenderingLayerMask  WetDecalLayerMask       = (UnityEngine.Rendering.HighDefinition.RenderingLayerMask) (uint) UnityEngine.RenderingLayerMask.defaultRenderingLayerMask;
#endif
        
        //volumetric lighting
        public QualityOverrideEnum VolumetricLighting                                    = QualityOverrideEnum.UseQualitySettings;
        public float               VolumetricLightTemporalReprojectionAccumulationFactor = 0.35f;
        public bool                VolumetricLightUseBlur                                = true;
        public float               VolumetricLightBlurRadius                             = 2;
        public QualityOverrideEnum VolumetricLightCausticMode      = QualityOverrideEnum.UseQualitySettings;
    
        //Caustic
        public QualityOverrideEnum CausticEffect                  = QualityOverrideEnum.UseQualitySettings;
        public float               CausticStrength                = 2.5f;
        public float               OceanCausticDispersionStrength = 1;
        public bool                DisableCausticsInShadow        = false;
        
        //underwater effect
        public QualityOverrideEnum          UnderwaterEffect                   = QualityOverrideEnum.UseQualitySettings;
        public UnderwaterReflectionModeEnum UnderwaterReflectionMode           = UnderwaterReflectionModeEnum.PhysicalAproximatedReflection;
        public bool                         UseUnderwaterHalfLineTensionEffect = true;
        public bool                         UseWaterDropsEffect                = true;
        public float                        WaterDropsEffectTimeScale          = 1;
        public bool                         UseUnderwaterNoiseBlur                  = false;
        public float                        UnderwaterNoiseBlurRadius               = 10;
        
        public float UnderwaterHalfLineTensionScale = 0.5f;
        public bool  OverrideUnderwaterTransparent  = false;
        public float UnderwaterTransparentOffset    = 5;

        
        [SerializeField] internal bool ShowColorSettings      = true;
     
        [SerializeField] internal bool ShowReflectionSettings = false;
        [SerializeField] internal bool ShowRefractionSettings = false;

        [SerializeField] internal bool ShowWetSettings              = false;
        [SerializeField] internal bool ShowVolumetricSettings       = false;
        [SerializeField] internal bool ShowCausticEffectSettings    = false;
        [SerializeField] internal bool ShowUnderwaterEffectSettings = false;
        [SerializeField] internal bool ShowRenderingSettings = false;
        [SerializeField] internal bool AutoUpdateIntersections      = true;


        #region public API methods

        
        public static bool IsCameraPartialUnderwater { get;     private set; }
        public static   bool   IsCameraFullUnderwater    { get; private set; }
        internal static bool   IsCameraRequireWaterDrops { get; private set; }
       

        /// <summary>
        /// You must invoke this method every time you change any water parameter.
        /// For example
        /// waterInstance.WindSpeed = 5;
        /// waterInstance.ForceUpdateWaterSettings();
        /// 
        /// A faster option is when you indicate which parameters tab has been changed
        /// for example
        /// waterInstance.ForceUpdateWaterSettings(WaterSettingsCategory.Ocean);
        /// or waterInstance.ForceUpdateWaterSettings(WaterSettingsCategory.Ocean | WaterSettingsCategory.VolumetricLighting); 
        /// 
        /// </summary>
        public static void ForceUpdateWaterSettings(WaterSettingsCategory waterSettingsCategory)
        {
            OnAnyWaterSettingsChanged?.Invoke(waterSettingsCategory);
        }
        
        ///// <summary>
        ///// Check if the current world space position is under water. For example, you can detect if your character enters the water to like triggering a swimming state.
        ///// </summary>
        ///// <param name="worldPos"></param>
        ///// <returns></returns>
        public static bool IsPositionUnderWater(Vector3 worldPos)
        {
            _worldPointRequest.SetNewPosition(worldPos);
            WaterSystem.TryGetWaterSurfaceData(_worldPointRequest);

            if (_worldPointRequest.IsDataReady)
            {
                return worldPos.y < _worldPointRequest.Result.Position.y;
            }
            return false;
        }


        /// <summary>
        /// Retrieves water position, normal, and velocity at a given world position.
        /// Works only with global wind.
        /// </summary>
        /// <param name="surfaceRequest">
        /// A reference to a <see cref="WaterSurfaceRequest"/> instance, which must be created beforehand.
        /// This request contains an array of data necessary to handle updates from different scripts and rendering queue 
        /// (such as Update, FixedUpdate, OnGUI, etc.).
        /// 
        /// Example usage:
        /// <code>
        /// WaterSurfaceRequest request = new WaterSurfaceRequest();
        /// request.SetNewPositions(positionsArray);
        /// var result = request.Result[index];
        /// </code>
        /// </param>
        /// <returns>Returns <c>true</c> if data retrieval was successful, otherwise <c>false</c>.</returns>
        public static void TryGetWaterSurfaceData(IWaterSurfaceRequest surfaceRequest)
        {
            BuoyancyPass.TryGetWaterSurfaceData(surfaceRequest);
        }

       
        /// <summary>
        /// Activate this option if you want to manually synchronize the time for all clients over the network
        /// </summary>
        public static bool UseNetworkTime;
        public static float NetworkTime;
        public static bool UseNetworkBuoyancy;


        public static bool ForceDisableWaterRendering;

        public static void ForceShiftOrigin(Vector3 shift)
        {
            if (shift.sqrMagnitude < 0.000001f) return;
            OnOriginShifted?.Invoke(shift);
            
            var domainSize              = KWS_Settings.FFT.FftDomainSizes.w;
            _accumulatedOriginOffset   =  (_accumulatedOriginOffset + shift);
            _accumulatedOriginOffset.x %= domainSize;
            _accumulatedOriginOffset.y %= domainSize;
            _accumulatedOriginOffset.z %= domainSize;
            
            Shader.SetGlobalVector("KWS_WaterWorldPosOffset", _accumulatedOriginOffset);
          
        }

        internal static float _globalWaterScaleFactor = 1;
        public static   float GlobalWaterScaleFactor 
        { 
            get => _globalWaterScaleFactor;
            set => _globalWaterScaleFactor = MathF.Max(0.0001f, value);
        }
        
    
        #endregion

     
        #region internal variables


        //internal Vector3 WaterRelativeWorldPosition
        //{
        //    get
        //    {
        //        var pos = KWS_UpdateManager.CurrentRenderedCameraTransform.position;
        //        pos.y = WaterPivotWorldPosition.y;
        //        return pos;
        //    }
        //}

        //internal Vector3 WaterPivotWorldPosition => KWS_Ocean.RenderOcean ? KWS_Ocean.Instance.WaterRootTransform.position :  WaterRootTransform.position;
        

        

        internal bool   IsWaterVisible            { get; private set; }
        public   float  WaterLevel                => transform.position.y;
        internal Bounds WorldSpaceBoundsWithZones => KWS_Ocean.Instance != null ? KWS_Ocean.Instance.OceanWorldSpaceBounds : KWS_TileZoneManager.VisibleDynamicWavesZonesBounds;
        
        internal float CurrentMaxOceanWaveHeight;
        internal float OceanWavesPredictionOffset;
        internal float CurrentMaxHeightOffsetRelativeToWaterLevel => Mathf.Max(CurrentMaxOceanWaveHeight, KWS_TileZoneManager.MaxZoneHeight - WaterLevel);
    
        internal static float GlobalTimeScale = 1;

        #endregion

        #region private variables


#if KWS_DEBUG
        public static Vector4 Test4 = Vector4.zero;
        public static float VRScale = 1;
        public static Texture2D TestTexture;
#endif

        internal static GameObject UpdateManagerObject;
        internal static KWS_UpdateManager UpdateManagerInstance;

        internal bool IsWaterInitialized { get; private set; }
        private bool _isWaterPlatformSpecificResourcesInitialized;

        #endregion

        #region properties
        
        internal bool RequireReinitializeMesh = true;
        internal bool RequireUpdateMesh = true;

        internal static event Action<Vector3> OnOriginShifted;
        private static Vector3                _accumulatedOriginOffset;
            
        #endregion

      

        private void Awake()
        {

        }

        private void OnEnable()
        {
            KWS_WaterSettingsRuntimeLoader.LoadWaterSettings();
            if (WaterSystem.QualitySettings == null)
            {
                Debug.LogError("WaterQualitySettings.asset not found in Resources.");
                return;
            }
            
            var allInstances = FindObjectsOfType<WaterSystem>(true);

            foreach (var instance in allInstances)
            {
                if (instance != this && instance.isActiveAndEnabled)
                {
                    Debug.LogWarning("Multiple active WaterSystems detected. Disabling extra instance.");
                    instance.gameObject.SetActive(false);
                }
            }
            Instance = this;
            
            WaterSharedResources.UpdateReflectionProbeCache();
            LoadPerFrameTextures();

            var updateManager = FindObjectsByType<KWS_UpdateManager>(FindObjectsSortMode.None);
            KW_Extensions.SafeDestroy(updateManager);

            UpdateManagerObject = KW_Extensions.CreateHiddenGameObject("KWS_UpdateManager");
            UpdateManagerInstance = UpdateManagerObject.AddComponent<KWS_UpdateManager>();
            //UpdateManagerObject.transform.parent = transform;
            
            OnAnyWaterSettingsChanged += OnAnyWaterSettingsChangedEvent;
            UpdateWaterInstance(WaterSettingsCategory.All);

            IsWaterVisible = true;
        }

        void OnDestroy()
        {
            if (IsWaterInitialized) OnDisable();
            LoadPerFrameTextures();
        }

        void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            if (Instance == null)
            {
                KW_Extensions.SafeDestroy(UpdateManagerObject);
                UpdateManagerObject = null;
                UnloadResources();
            }
            OnAnyWaterSettingsChanged -= OnAnyWaterSettingsChangedEvent;

            Release();
        }


        void Release()
        {
            IsWaterInitialized = false;
         
        
            _isFluidsSimBakedMode = false;

            _isWaterPlatformSpecificResourcesInitialized = false;
            IsWaterVisible                               = true;
            IsCameraPartialUnderwater                    = false;
            IsCameraFullUnderwater                       = false;
            GlobalTimeScale                              = 1;
            //CameraDatas.Clear();


            _underwaterStateCameras.Clear();
        }
    }
}