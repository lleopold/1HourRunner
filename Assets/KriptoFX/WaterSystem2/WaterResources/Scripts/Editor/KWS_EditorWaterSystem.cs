#if UNITY_EDITOR

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static KWS.KWS_EditorUtils;
using static KWS.KWS_EditorVideoTipsSettings;
using Debug = UnityEngine.Debug;
using static KWS.WaterSystem;
using Description = KWS.KWS_EditorTextDescription;
using QualitySettings = UnityEngine.QualitySettings;

namespace KWS
{
    [System.Serializable]
    [CustomEditor(typeof(WaterSystem))]
    internal partial class KWS_EditorWaterSystem : Editor
    {
        private WaterSystem               _waterInstance;
        private WaterQualityLevelSettings _qualitySettings;

        private bool                     _isActive;
        private SceneView.SceneViewState _lastSceneView;


        void OnDestroy()
        {
            KWS_EditorUtils.Release();
        }


        public override void OnInspectorGUI()
        {
            _waterInstance = (WaterSystem)target;

            if (_waterInstance.enabled && _waterInstance.gameObject.activeSelf)
            {
                _isActive   = true;
                GUI.enabled = true;
            }
            else
            {
                _isActive   = false;
                GUI.enabled = false;
            }

            UpdateWaterGUI();
        }



        void UpdateWaterGUI()
        {
            _qualitySettings = WaterSystem.QualitySettings;
            if (_qualitySettings == null)
            {
                KWS_WaterSettingsRuntimeLoader.LoadWaterSettings();
                _qualitySettings = WaterSystem.QualitySettings;
                if (_qualitySettings == null)
                {
                    Debug.Log("empty settings?");
                    return;
                }
            }

            Undo.RecordObject(_waterInstance, "Changed main water parameters");
#if KWS_DEBUG
            
            #if KWS_BUILTIN
                EditorGUILayout.HelpBox("Current pipeline: Built-in",  MessageType.Info);
            #endif
            #if KWS_URP
                EditorGUILayout.HelpBox("Current pipeline: URP",  MessageType.Info);
            #endif
            #if KWS_HDRP
                EditorGUILayout.HelpBox("Current pipeline: HDRP",  MessageType.Info);
            #endif
            
            WaterSystem.Test4 = EditorGUILayout.Vector4Field("Test4", WaterSystem.Test4);
            if (KWS_CoreUtils.SinglePassStereoEnabled) VRScale = Slider("VR Scale", "", VRScale, 0.5f, 2.5f);
            WaterSystem.TestTexture = (Texture2D)EditorGUILayout.ObjectField(WaterSystem.TestTexture, typeof(Texture2D), true);
            if (WaterSystem.TestTexture != null)
            {
                Shader.SetGlobalTexture("KWS_TestTexture", WaterSystem.TestTexture);
            }
#endif
            //waterSystem.TestObj = (GameObject) EditorGUILayout.ObjectField(waterSystem.TestObj, typeof(GameObject), true);


            EditorGUI.BeginChangeCheck();
            EditorGUIUtility.labelWidth = 200;
            //CheckMessages();

            GUI.enabled = _isActive;

            EditorGUILayout.Space(20);
            CheckPlatformSpecificMessages_Reflection();

            if (KWS_UpdateManager.EditorRefreshScale < 0.99f)
            {
                EditorGUILayout.HelpBox("The scene view update mode \"always refresh\" is turned off, so the water rendering doesn’t update every frame", MessageType.Warning);
                EditorGUILayout.Space(20);  
            }
            
            KWS_Tab(ref _waterInstance.ShowColorSettings,            "Global Color Settings",                ColorSettings,              WaterSettingsCategory.ColorSettings);
            KWS_Tab(ref _waterInstance.ShowReflectionSettings,       "Reflection",                           ReflectionSettings,         WaterSettingsCategory.Reflection,         VideoTips.Reflection);
            KWS_Tab(ref _waterInstance.ShowRefractionSettings,       "Refraction (View Through Water)",      RefractionSetting,          WaterSettingsCategory.ColorRefraction,    VideoTips.RefractionMode);
            KWS_Tab(ref _waterInstance.ShowWetSettings,              "Wet Effect",                           WetSetting,                 WaterSettingsCategory.WetEffect,          VideoTips.WetEffect);
            KWS_Tab(ref _waterInstance.ShowVolumetricSettings,       "Volumetric Lighting",                  VolumetricLightingSettings, WaterSettingsCategory.VolumetricLighting, VideoTips.VolumetricLighting);
            KWS_Tab(ref _waterInstance.ShowCausticEffectSettings,    "Caustic (Light Patterns on Surfaces)", CausticSettings,            WaterSettingsCategory.Caustic,            VideoTips.Caustic);
            KWS_Tab(ref _waterInstance.ShowUnderwaterEffectSettings, "Underwater Effect",                    UnderwaterSettings,         WaterSettingsCategory.Underwater,         VideoTips.Underwater);
           // KWS_Tab(ref _waterInstance.ShowRenderingSettings,        "Rendering Settings",                   RenderingSettings,          WaterSettingsCategory.Rendering);

            if (EditorGUI.EndChangeCheck())
            {
                if (!Application.isPlaying)
                {
                    EditorUtility.SetDirty(_waterInstance);
                    EditorSceneManager.MarkSceneDirty(_waterInstance.gameObject.scene);
                }
            }
        }


        void ColorSettings()
        {
            _waterInstance.Transparent    = Slider("Transparent (Meters)", Description.Color.Transparent, _waterInstance.Transparent, 1f, 100f, VideoTips.Transparent);
            _waterInstance.WaterColor     = ColorField("Water Color",     Description.Color.WaterColor,     _waterInstance.WaterColor,     false, false, false, VideoTips.WaterColor);
            _waterInstance.TurbidityColor = ColorField("Turbidity Color", Description.Color.TurbidityColor, _waterInstance.TurbidityColor, false, false, false, VideoTips.TurbidityColor);
        }


        void ReflectionSettings()
        {
            _waterInstance.ScreenSpaceReflection =
                (WaterQualityLevelSettings.QualityOverrideEnum)EnumPopup("Screen Space Reflection", Description.Reflection.UseScreenSpaceReflection, _waterInstance.ScreenSpaceReflection, VideoTips.ScreenSpaceReflection);
            _waterInstance.PlanarReflection = (WaterQualityLevelSettings.QualityOverrideEnum)EnumPopup("Planar Reflection", Description.Reflection.UsePlanarReflection, _waterInstance.PlanarReflection, VideoTips.PlanarReflection);

            if (_waterInstance.ScreenSpaceReflection != WaterQualityLevelSettings.QualityOverrideEnum.Off && WaterSystem.QualitySettings.UseScreenSpaceReflection
             || _waterInstance.PlanarReflection      != WaterQualityLevelSettings.QualityOverrideEnum.Off && WaterSystem.QualitySettings.UsePlanarReflection)
            {
                _waterInstance.AnisotropicReflectionsScale = Slider("Anisotropic Reflections Scale", Description.Reflection.AnisotropicReflectionsScale, _waterInstance.AnisotropicReflectionsScale, 0.1f, 1.0f, VideoTips.AnisotropicReflection);
            }


            Line();

            _waterInstance.OverrideSkyColor = Toggle("Override Sky Color", "", _waterInstance.OverrideSkyColor, VideoTips.OverrideSkyColor);
            if (_waterInstance.OverrideSkyColor)
            {
                _waterInstance.CustomSkyColor = ColorField("Custom Sky Color", "", _waterInstance.CustomSkyColor, false, false, false);
            }

            _waterInstance.ReflectSun = Toggle("Reflect Sunlight", Description.Reflection.ReflectSun, _waterInstance.ReflectSun, VideoTips.ReflectSun);
            if (_waterInstance.ReflectSun)
            {
                _waterInstance.ReflectedSunCloudinessStrength = Slider("Sun Cloudiness", Description.Reflection.ReflectedSunCloudinessStrength, _waterInstance.ReflectedSunCloudinessStrength, 0.03f, 0.25f, VideoTips.SunCloudiness);
                _waterInstance.ReflectedSunStrength           = Slider("Sun Strength",   Description.Reflection.ReflectedSunStrength,           _waterInstance.ReflectedSunStrength,           0f,    1f,    VideoTips.SunStrength);
            }


            //KWS_EditorProfiles.PerfomanceProfiles.Reflection.CheckDataChangesAnsSetCustomProfile(_waterInstance);
        }


        void RefractionSetting()
        {
           
            _waterInstance.RefractionMode = (WaterQualityLevelSettings.RefractionModeEnum)EnumPopup("Refraction Mode", " ", _waterInstance.RefractionMode, VideoTips.RefractionMode);
            if (_waterInstance.RefractionMode == WaterQualityLevelSettings.RefractionModeEnum.Simple)
            {
                _waterInstance.RefractionSimpleStrength = Slider("Strength", Description.Refraction.RefractionSimpleStrength, _waterInstance.RefractionSimpleStrength, 0.02f, 1);
            }

            _waterInstance.RefractionDispersion = (WaterQualityLevelSettings.QualityOverrideEnum)EnumPopup("Dispersion", Description.Refraction.UseRefractionDispersion, _waterInstance.RefractionDispersion, VideoTips.Dispersion);

            
            if (_waterInstance.RefractionDispersion != WaterQualityLevelSettings.QualityOverrideEnum.Off)
            {
                _waterInstance.RefractionDispersionStrength = Slider("Dispersion Strength", Description.Refraction.RefractionDispersionStrength, _waterInstance.RefractionDispersionStrength, 0.25f, 1);
            }
            
            
        }


        void WetSetting()
        {
            _waterInstance.WetEffect = (WaterQualityLevelSettings.QualityOverrideEnum)EnumPopup("Wet Effect", "", _waterInstance.WetEffect, VideoTips.WetEffect);

#if KWS_URP
            //if (!KWS_CustomSettingsProvider.IsDecalFeatureUsed)
            {
                EditorGUILayout.HelpBox("To make the \"Wet Effect\" work, you need to enable the URP Decal Rendering Feature." + Environment.NewLine +
                                        "Check the documentation for how to set it up.", MessageType.Info);
            }

#endif

#if UNITY_6000_0_OR_NEWER && (KWS_URP)
            
            var layers = UnityEngine.RenderingLayerMask.GetDefinedRenderingLayerNames();
            _waterInstance.WetDecalLayerMask      =  EditorGUILayout.MaskField(new GUIContent("WetDecalLayerMask", ""), _waterInstance.WetDecalLayerMask, layers);
            
#endif
            
#if UNITY_6000_0_OR_NEWER && (KWS_HDRP)
            
            var layers       = UnityEngine.RenderingLayerMask.GetDefinedRenderingLayerNames();
            _waterInstance.WetDecalLayerMask      =   (UnityEngine.Rendering.HighDefinition.RenderingLayerMask) (uint)EditorGUILayout.MaskField(new GUIContent("WetDecalLayerMask", ""),  (RenderingLayerMask)(uint)_waterInstance.WetDecalLayerMask, layers);
            
#endif
            _waterInstance.WetStrength             = Slider("Wet Strength",           "", _waterInstance.WetStrength,             0.1f, 1.0f, VideoTips.WetStrength);
            _waterInstance.WetnessHeightAboveWater = Slider("Wet Height Above Water", "", _waterInstance.WetnessHeightAboveWater, 0,    3.0f, VideoTips.WetnessHeightAboveWater);
            
        }

        void VolumetricLightingSettings()
        {
            CheckPlatformSpecificMessages_VolumeLight();
            _waterInstance.VolumetricLighting = (WaterQualityLevelSettings.QualityOverrideEnum)EnumPopup("Volumetric Lighting", "", _waterInstance.VolumetricLighting, VideoTips.VolumetricLighting);

            _waterInstance.VolumetricLightTemporalReprojectionAccumulationFactor =
                Slider("Temporal Accumulation Factor", "", _waterInstance.VolumetricLightTemporalReprojectionAccumulationFactor, 0.1f, 0.75f, VideoTips.VolumetricLightTemporalAccumulationFactor);
            _waterInstance.VolumetricLightUseBlur = Toggle("Use Blur", "", _waterInstance.VolumetricLightUseBlur, VideoTips.VolumetricLightingBlur);
            if (_waterInstance.VolumetricLightUseBlur) _waterInstance.VolumetricLightBlurRadius = Slider("Blur Radius", "", _waterInstance.VolumetricLightBlurRadius, 1f, 3f);
            Line();

            var qualityQausticMode =  WaterSystem.QualitySettings.VolumetricLightCausticMode;
            var allLightCaustic    = _waterInstance.VolumetricLightCausticMode == WaterQualityLevelSettings.QualityOverrideEnum.UseQualitySettings 
                                  && qualityQausticMode == WaterQualityLevelSettings.VolumetricLightCausticModeEnum.AllLights;
            
            if (allLightCaustic) EditorGUILayout.HelpBox("Caustics from all lights can cause a performance drop.", MessageType.Warning);
            _waterInstance.VolumetricLightCausticMode = (WaterQualityLevelSettings.QualityOverrideEnum)EnumPopup("Volumetric Caustic", "", _waterInstance.VolumetricLightCausticMode, VideoTips.VolumetricLightingFullCaustic);
        }


        void CausticSettings()
        {
            _waterInstance.CausticEffect                  = (WaterQualityLevelSettings.QualityOverrideEnum)EnumPopup("Caustic Effect", "", _waterInstance.CausticEffect, VideoTips.Caustic);
            _waterInstance.CausticStrength                = Slider("Caustic Strength",         Description.Caustic.CausticStrength, _waterInstance.CausticStrength,                0.25f, 5, VideoTips.CausticStrength);
            _waterInstance.OceanCausticDispersionStrength = Slider("Ocean Caustic Dispersion", "",                                  _waterInstance.OceanCausticDispersionStrength, 0.25f, 2, VideoTips.CausticDispersion);
            _waterInstance.DisableCausticsInShadow = Toggle("Disable Caustics in Shadow", "", _waterInstance.DisableCausticsInShadow, null);
        }


        void UnderwaterSettings()
        {
            _waterInstance.UnderwaterEffect = (WaterQualityLevelSettings.QualityOverrideEnum)EnumPopup("Underwater Effect", "", _waterInstance.UnderwaterEffect, VideoTips.Underwater);

            _waterInstance.UnderwaterReflectionMode = (WaterQualityLevelSettings.UnderwaterReflectionModeEnum)EnumPopup("Internal Reflection Mode", "", _waterInstance.UnderwaterReflectionMode, VideoTips.UnderwaterReflectionMode);
            Line();
            
            _waterInstance.UseUnderwaterHalfLineTensionEffect = Toggle("Use Half Line Tension Effect", "", _waterInstance.UseUnderwaterHalfLineTensionEffect, VideoTips.UnderwaterHalfLineTensionEffect);
            if (_waterInstance.UseUnderwaterHalfLineTensionEffect)
            {
                _waterInstance.UnderwaterHalfLineTensionScale = Slider("Tension Scale", "", _waterInstance.UnderwaterHalfLineTensionScale, 0.2f, 1f, VideoTips.UnderwaterHalfLineTensionScale);
                Line();
            }
            _waterInstance.UseWaterDropsEffect       = Toggle("Use Water Drops Effect", "", _waterInstance.UseWaterDropsEffect, VideoTips.WaterDropsEffect);

            if (_waterInstance.UseWaterDropsEffect)
            {
                _waterInstance.WaterDropsEffectTimeScale = Slider("Drops Effect Time Scale", "", _waterInstance.WaterDropsEffectTimeScale, 0.25f, 4);
                Line();
            }
           
            _waterInstance.UseUnderwaterNoiseBlur = Toggle("Use Underwater Noise Blur", "",  _waterInstance.UseUnderwaterNoiseBlur);
            if (_waterInstance.UseUnderwaterNoiseBlur)
            {
                _waterInstance.UnderwaterNoiseBlurRadius = Slider("Underwater Blur Radius", "",  _waterInstance.UnderwaterNoiseBlurRadius, 1, 100);
            }
           
            //_waterInstance.OverrideUnderwaterTransparent = Toggle("Override Transparent", "", _waterInstance.OverrideUnderwaterTransparent, link.OverrideUnderwaterTransparent);
            //if (_waterInstance.OverrideUnderwaterTransparent)
            //{
            //    _waterInstance.UnderwaterTransparentOffset = Slider("Transparent Offset", Description.Color.Transparent, _waterInstance.UnderwaterTransparentOffset, -100, 100, link.Transparent);
            //}
        }

        // void RenderingSettings()
        // {
        //     _waterInstance.SimulationFPSMultiplier = Slider("Simulation FPS Multiplier", Description.Waves.TimeScale, _waterInstance.SimulationFPSMultiplier, 0.0f, 2.0f, VideoTips.SimulationFPSMultiplier);
        //
        // }

        void CheckPlatformSpecificMessages_Reflection()
        {
            if (Instance == null) return;

#if KWS_BUILTIN
            if (Instance.ReflectSun)
            {
                if (KWS_WaterLights.Lights.Count == 0 || KWS_WaterLights.Lights.Any(l => l.Light.type == LightType.Directional) == false)
                {
                    EditorGUILayout.HelpBox("'Water->Reflection->Reflect Sunlight' doesn't work because no directional light has been added for water rendering! Add the script 'AddLightToWaterRendering' to your directional light!",
                                            MessageType.Error);
                }
            }

            var volumetricLighting = WaterQualityLevelSettings.ResolveQualityOverride(WaterSystem.Instance.VolumetricLighting, WaterSystem.QualitySettings.UseVolumetricLight);
            if (volumetricLighting && KWS_WaterLights.Lights.Count == 0)
                EditorGUILayout.HelpBox("Water->'Volumetric lighting' doesn't work because no lights has been added for water rendering! Add the script 'AddLightToWaterRendering' to your light.", MessageType.Error);
#endif

#if KWS_BUILTIN || KWS_URP
            if (ReflectionProbe.defaultTexture.width == 1 && _waterInstance.OverrideSkyColor == false)
            {
                EditorGUILayout.HelpBox("Sky reflection doesn't work in this scene, you need to generate scene lighting! " + Environment.NewLine +
                                        "Open the \"Lighting\" window -> select the Generate Lighting option Reflection Probes", MessageType.Error);
            }
#endif
        }

        void CheckPlatformSpecificMessages_VolumeLight()
        {
            //if (_waterInstance.Settings.UseVolumetricLight && KWS_WaterLights.Lights.Count == 0) EditorGUILayout.HelpBox("Water->'Volumetric lighting' doesn't work because no lights has been added for water rendering! Add the script 'AddLightToWaterRendering' to your light.", MessageType.Error);
        }
        
      
    }
}

#endif