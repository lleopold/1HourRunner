using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KWS
{
    [ExecuteInEditMode]
    public class KWS_Ocean : MonoBehaviour
    {
        public static KWS_Ocean Instance { get; private set; }
        
        //Waves settings
        public float                                         WindSpeed        = 5.0f;
        public float                                         WindRotation     = 0;
        public float                                         WindTurbulence   = 0.25f;
        public float                                         TimeScale        = 1.0f;
        public WaterQualityLevelSettings.FftWavesQualityEnum FftWavesQuality  = WaterQualityLevelSettings.FftWavesQualityEnum.Ultra;
        public int                                           FftWavesCascades = 4;
        public float                                         WavesAreaScale   = 1;
       
        public                    WindZone                                      WindZone;
        public                    float                                         WindZoneSpeedMultiplier      = 1;
        public                    float                                         WindZoneTurbulenceMultiplier = 1;
        
        //Foam
        public WaterQualityLevelSettings.QualityOverrideEnum OceanFoam                   = WaterQualityLevelSettings.QualityOverrideEnum.UseQualitySettings;
        public WaterQualityLevelSettings.FoamTextureTypeEnum FoamTextureType             = WaterQualityLevelSettings.FoamTextureTypeEnum.Type1;
        public float                                         FoamTextureContrast         = 1.0f;
        public float                                         FoamTextureScaleMultiplier  = 1.5f;
        public float                                         OceanFoamStrength           = 0.4f;
        public float                                         OceanFoamLifetimeMultiplier = 0.9f;

        public float RainDropsIntensity = 0;

        internal Bounds OceanWorldSpaceBounds;
      
        
        #region editor variables


        internal bool DebugQuadtree      = false;
        internal bool DebugAABB          = false;
        internal bool DebugBuoyancy      = false;
        internal bool DebugDynamicWaves  = false;
        internal bool DebugUpdateManager = false;
        internal bool DebugUnderwater = false;

        #endregion

        
        private Transform _waterTransform;
        internal Transform WaterRootTransform
        {
            get
            {
                if (_waterTransform == null) _waterTransform = transform;
                return _waterTransform;
            }
        }
        
        
        private void OnEnable()
        {
            var allInstances = FindObjectsOfType<KWS_Ocean>(true);

            foreach (var instance in allInstances)
            {
                if (instance != this && instance.isActiveAndEnabled)
                {
                    Debug.LogWarning("Multiple 'Ocean' detected. Disabling extra instance.");
                    instance.gameObject.SetActive(false);
                }
            }
            Instance = this;
        }

        void Update()
        {
            ResetTransform();
            UpdateWindIfRequired();
        }
        
        
        void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            
        }
        
        
        void ResetTransform()
        {
            WaterRootTransform.rotation   = Quaternion.identity;
            WaterRootTransform.localScale = Vector3.one;
            if (WaterSystem.Instance != null) WaterSystem.Instance.transform.position = transform.position;
        }

        void UpdateWindIfRequired()
        {
            if (WindZone)
            {
                WindSpeed      = Mathf.Clamp(WindZone.windMain * WindZoneSpeedMultiplier, 0.1f, 50f);
                WindTurbulence = Mathf.Clamp01(WindZone.windTurbulence);
                
                var windDir = WindZone.transform.forward;
                WindRotation = Mathf.Atan2(windDir.z, windDir.x) * Mathf.Rad2Deg;
            }

        }
        
        
#if KWS_DEBUG

        private void OnDrawGizmos()
        {
            if (Instance == null) return;
            
            //if (KWS_Ocean.RenderOcean && KWS_Ocean.Instance.DebugAABB) Gizmos.DrawWireCube(WorldSpaceBounds.center, new Vector3(1000, WorldSpaceBounds.size.y, 1000));
            if (Instance.DebugQuadtree) DebugHelpers.DebugQuadtree(this);
            if (Instance.DebugBuoyancy && Application.isPlaying)
            {
                DebugHelpers.RequestBuoyancy();
                DebugHelpers.DebugBuoyancy();
            }
        }

        private void OnGUI()
        { 
            if (Instance == null) return;
            
            if (Instance.DebugDynamicWaves) DebugHelpers.DebugDynamicWaves();
        }

#endif
    }
}