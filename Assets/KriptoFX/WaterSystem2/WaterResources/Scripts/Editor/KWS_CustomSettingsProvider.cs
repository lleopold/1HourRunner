using System.IO;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UIElements;


using Description = KWS.KWS_EditorTextDescription;
using link = KWS.KWS_EditorUrlLinks;
using static KWS.KWS_Settings;
using System;
using System.Linq;
using static KWS.KWS_EditorVideoTipsSettings;

#if UNITY_EDITOR
using UnityEditor;
using static KWS.KWS_EditorUtils;
#endif


namespace KWS
{
    

#if UNITY_EDITOR
    
    
    class KWS_CustomSettingsProvider : SettingsProvider 
    {
       
      
        internal static bool                       IsDecalFeatureUsed;

        private KWS_CustomSettingsProvider(string path, SettingsScope scope = SettingsScope.Project) : base(path, scope) { }
        
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            return new KWS_CustomSettingsProvider("Project/KWS Water Settings", SettingsScope.Project);
        }
        
        
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            KWS_WaterSettingsRuntimeLoader.GetSerializedSettings();
            KWS_WaterSettingsRuntimeLoader.SyncWithUnityQualityLevels();
        }

       
        public override void OnGUI(string searchContext)
        {
           
            var _settingsContainer = KWS_WaterSettingsRuntimeLoader._waterSystemQualitySettings;

            Undo.RecordObject(_settingsContainer, "Changed water quality parameters");

            EditorGUIUtility.labelWidth = 80;
            GUILayout.Space(10);
            UnityQualitySettings();
            GUILayout.Space(15);

            EditorGUI.BeginChangeCheck();
            EditorGUIUtility.labelWidth = 220;

            CheckWarnings();

            var _settings = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;

            KWS2_Tab(ref _settingsContainer.ShowReflectionSettings, "Reflection",                      ReflectionSettings, WaterSystem.WaterSettingsCategory.Reflection,      VideoTips.Reflection);
            KWS2_Tab(ref _settingsContainer.ShowRefractionSettings, "Refraction (View Through Water)", RefractionSetting,  WaterSystem.WaterSettingsCategory.ColorRefraction, VideoTips.RefractionMode);
           
            KWS2_TabWithEnabledToogle(ref _settings.UseDynamicWaves, ref _settingsContainer.ShowDynamicWavesSettings, "Dynamic Waves Simulation", DynamicWavesSettings, WaterSystem.WaterSettingsCategory.DynamicWaves, VideoTips.DynamicSimulation);
            KWS2_TabWithEnabledToogle(ref _settings.UseOceanFoam, ref _settingsContainer.ShowFoamSettings, "Ocean Foam", FoamSetting, WaterSystem.WaterSettingsCategory.Foam, VideoTips.OceanFoam);
            KWS2_TabWithEnabledToogle(ref _settings.UseWetEffect, ref _settingsContainer.ShowWetSettings, "Wet Effect", WetSetting, WaterSystem.WaterSettingsCategory.WetEffect, VideoTips.WetEffect);
            KWS2_TabWithEnabledToogle(ref _settings.UseVolumetricLight, ref _settingsContainer.ShowVolumetricLightSettings, "Volumetric Lighting", VolumetricLightingSettings, WaterSystem.WaterSettingsCategory.VolumetricLighting, VideoTips.VolumetricLighting);
            KWS2_TabWithEnabledToogle(ref _settings.UseCausticEffect, ref _settingsContainer.ShowCausticEffectSettings,  "Caustic (Light Patterns on Surfaces)", CausticSettings, WaterSystem.WaterSettingsCategory.Caustic, VideoTips.Caustic);
            KWS2_TabWithEnabledToogle(ref _settings.UseUnderwaterEffect, ref _settingsContainer.ShowUnderwaterEffectSettings, "Underwater Effects", UnderwaterSettings, WaterSystem.WaterSettingsCategory.Underwater, VideoTips.Underwater);
            KWS2_Tab(ref _settingsContainer.ShowMeshSettings, "Mesh Settings",      MeshSettings,     WaterSystem.WaterSettingsCategory.Mesh, VideoTips.QuadTree);
            KWS2_Tab(ref _settingsContainer.ShowRendering,    "Rendering Settings", RenderingSetting, WaterSystem.WaterSettingsCategory.Rendering);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_settingsContainer);
                // AssetDatabase.SaveAssets();
                KWS_WaterSettingsRuntimeLoader.UpdateHash();
            }

            var _settingsObject = KWS_WaterSettingsRuntimeLoader._settingsObject;
            if (_settingsObject != null && _settingsObject.targetObject)
            {
                _settingsObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }


        void CheckWarnings()
        {
#if KWS_URP
            //if (!IsDecalFeatureUsed)
            {
                EditorGUILayout.HelpBox("To make the \"Wet Effect\" work, you need to enable the URP Decal Rendering Feature." + Environment.NewLine +
                                        "Check the documentation for how to set it up.", MessageType.Info);
            }
                
#endif

        }
        
        
        void UnityQualitySettings()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("Unity Quality Levels", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(3);

            var qualityLevels = QualitySettings.names;
            var currentQualityLevel = QualitySettings.GetQualityLevel();
            var selectedLevel = currentQualityLevel;

            for (int idx = 0; idx < qualityLevels.Length; idx++)
            {
                string level                        = qualityLevels[idx];
                var    newValue                     = Toggle(level, "", selectedLevel == idx);
                if (newValue == true) selectedLevel = idx;
            }
            QualitySettings.SetQualityLevel(selectedLevel);
            EditorGUI.indentLevel--;

            var _settingsContainer = KWS_WaterSettingsRuntimeLoader._waterSystemQualitySettings;
            var levelName          = qualityLevels[selectedLevel];
            var levelSettings      = _settingsContainer.qualityLevelSettings.Find(x => x.levelName == levelName);
            if (levelSettings == null)
            {
                levelSettings = new WaterQualityLevelSettings { levelName = levelName };
                _settingsContainer.qualityLevelSettings.Add(levelSettings);
                
                EditorUtility.SetDirty(_settingsContainer);
                AssetDatabase.SaveAssets();
                
                KWS_WaterSettingsRuntimeLoader.UpdateHash();
            }
            KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings = levelSettings;

            if (currentQualityLevel != selectedLevel)
            {
                WaterSystem.OnAnyWaterSettingsChanged?.Invoke(WaterSystem.WaterSettingsCategory.All);
            }

        }

        Dictionary<int, int> _actualLayers = new Dictionary<int, int>();
        void ReflectionSettings()
        {
            //KWS_EditorProfiles.PerfomanceProfiles.Reflection.ReadDataFromProfile(_waterSystem);
            var _settings          = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;
            var _settingsContainer = KWS_WaterSettingsRuntimeLoader._waterSystemQualitySettings;
            _settings.UseScreenSpaceReflection = Toggle("Use Screen Space Reflection", Description.Reflection.UseScreenSpaceReflection, _settings.UseScreenSpaceReflection, VideoTips.ScreenSpaceReflection);

            if (_settings.UseScreenSpaceReflection)
            {
                _settings.ScreenSpaceReflectionResolutionQuality = (WaterQualityLevelSettings.ScreenSpaceReflectionResolutionQualityEnum)EnumPopup("Screen Space Resolution Quality",
                                                                    Description.Reflection.ScreenSpaceReflectionResolutionQuality, _settings.ScreenSpaceReflectionResolutionQuality, VideoTips.ReflectionResolutionQuality);
                
                _settings.UseScreenSpaceReflectionSky  = Toggle("Use Screen Space Skybox", "", _settings.UseScreenSpaceReflectionSky, VideoTips.ScreenSpaceReflectionSkybox);
                _settings.ScreenSpaceBordersStretching = Slider("Borders Stretching", "", _settings.ScreenSpaceBordersStretching, 0f, 0.05f, VideoTips.ScreenSpaceReflectionBorderStretching);
                Line();
            }



            _settings.UsePlanarReflection = Toggle("Use Planar Reflection", Description.Reflection.UsePlanarReflection, _settings.UsePlanarReflection, VideoTips.PlanarReflection);
            if (_settings.UsePlanarReflection)
            {
                EditorGUILayout.HelpBox(Description.Warnings.PlanarReflectionUsed, MessageType.Warning);
                _settings.RenderPlanarShadows = Toggle("Planar Shadows", "", _settings.RenderPlanarShadows, VideoTips.PlanarReflectionShadows);

                if (Reflection.IsVolumetricsAndFogAvailable)
                    _settings.RenderPlanarVolumetricsAndFog = Toggle("Planar Volumetrics and Fog", "", _settings.RenderPlanarVolumetricsAndFog);
                if (Reflection.IsCloudRenderingAvailable) _settings.RenderPlanarClouds = Toggle("Planar Clouds", "", _settings.RenderPlanarClouds);

                _settings.PlanarReflectionResolutionQuality =
                    (WaterQualityLevelSettings.PlanarReflectionResolutionQualityEnum)EnumPopup("Planar Resolution Quality", Description.Reflection.PlanarReflectionResolutionQuality, _settings.PlanarReflectionResolutionQuality, VideoTips.ReflectionResolutionQuality);

                var planarCullingMask = MaskField("Planar Layers Mask", Description.Reflection.PlanarCullingMask, _settings.PlanarCullingMask);
                _settings.PlanarCullingMask = planarCullingMask & ~(1 << Water.WaterLayer);

            }

            if (_settings.UsePlanarReflection)
            {
                _settings.ReflectionClipPlaneOffset = Slider("Clip Plane Offset", Description.Reflection.ReflectionClipPlaneOffset, _settings.ReflectionClipPlaneOffset, 0, 0.07f, VideoTips.ClipPlaneOffset);
            }

            if (_settings.UseScreenSpaceReflection || _settings.UsePlanarReflection)
            {
                Line();
                _settings.UseAnisotropicReflections = Toggle("Use Anisotropic Reflections", Description.Reflection.UseAnisotropicReflections, _settings.UseAnisotropicReflections, VideoTips.AnisotropicReflection);

                if (_settings.UseAnisotropicReflections)
                {
                   
                    _settings.AnisotropicReflectionsHighQuality = Toggle("High Quality Anisotropic", Description.Reflection.AnisotropicReflectionsHighQuality, _settings.AnisotropicReflectionsHighQuality);
                }

            }


        }

        void RefractionSetting()
        {
            var _settings                                                                  = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;
            if (Refraction.IsRefractionDownsampleAvailable) _settings.RefractionResolution = (WaterQualityLevelSettings.RefractionResolutionEnum)EnumPopup("Resolution", "", _settings.RefractionResolution, VideoTips.RefractionMode);
            _settings.UseRefractionDispersion = Toggle("Use Dispersion", Description.Refraction.UseRefractionDispersion, _settings.UseRefractionDispersion, VideoTips.Dispersion);
        }


        void DynamicWavesSettings()
        {  
            var _settings = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;
            _settings.DynamicWavesMapResolution = (WaterQualityLevelSettings.DynamicWavesMapResolutionEnum)EnumPopup("Cascade Map Resolution",  "", _settings.DynamicWavesMapResolution); //todo add video
        }

        void FoamSetting()
        {  
            var _settings = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;
            if (_settings.UseOceanFoam)
            {
                //if (_waterInstance.Settings.CurrentWindSpeed < 7.1f) EditorGUILayout.HelpBox("Foam appears during strong winds (from ~8 meters and above)", MessageType.Info);
            }

        }

        void WetSetting()
        {
#if KWS_URP
            EditorGUILayout.HelpBox("To make the \"Wet Effect\" work, you need to enable the URP Decal Rendering Feature." + Environment.NewLine +
                                    "Check the documentation for how to set it up.", MessageType.Info);
                
#endif
        }


        void VolumetricLightingSettings()
        {  
            var _settings = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;
            
            _settings.VolumetricLightResolutionQuality =
                (WaterQualityLevelSettings.VolumetricLightResolutionQualityEnum)EnumPopup("Resolution Quality", Description.VolumetricLight.ResolutionQuality, _settings.VolumetricLightResolutionQuality, VideoTips.VolumetricLightingResolution);
            _settings.VolumetricLightIteration = IntSlider("Iterations", Description.VolumetricLight.Iterations, _settings.VolumetricLightIteration, 2, KWS_Settings.VolumetricLighting.MaxIterations, VideoTips.VolumetricLightingIterations);

            if ( _settings.VolumetricLightCausticMode == WaterQualityLevelSettings.VolumetricLightCausticModeEnum.AllLights) EditorGUILayout.HelpBox("Caustics from all lights can cause a performance drop.", MessageType.Warning);
            _settings.VolumetricLightCausticMode = (WaterQualityLevelSettings.VolumetricLightCausticModeEnum)EnumPopup("Volumetric Caustic Mode", "", _settings.VolumetricLightCausticMode, VideoTips.VolumetricLightingFullCaustic);
        }

        void CausticSettings()
        {
            var   _settings             = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;
            var   size                  = (int)_settings.OceanCausticTextureResolutionQuality;
            float currentRenderedPixels = size * size;
            currentRenderedPixels *= _settings.UseOceanCausticHighQualityFiltering ? 2 : 1;
            currentRenderedPixels = (currentRenderedPixels / 1000000f);
            EditorGUILayout.LabelField("Simulation rendered pixels (less is better): " + currentRenderedPixels.ToString("0.0") + " millions", KWS_EditorUtils.NotesLabelStyleFade);

            _settings.OceanCausticTextureResolutionQuality = (WaterQualityLevelSettings.CausticTextureResolutionQualityEnum)EnumPopup("Ocean Caustic Resolution", "", _settings.OceanCausticTextureResolutionQuality, VideoTips.CausticResolution);
            _settings.UseOceanCausticHighQualityFiltering  = Toggle("Ocean Caustic Filtering", "",                                       _settings.UseOceanCausticHighQualityFiltering, VideoTips.CausticFiltering);
            _settings.UseOceanCausticDispersion            = Toggle("Ocean Caustic Dispersion",        Description.Caustic.UseCausticDispersion, _settings.UseOceanCausticDispersion, VideoTips.CausticDispersion);
        }

        void UnderwaterSettings()
        {
            var _settings = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;
            _settings.DisableWaterDropsInScene = Toggle("Disable Water Drops In Scene",  "", _settings.DisableWaterDropsInScene, VideoTips.WaterDropsEffect);
        }

        void MeshSettings()
        {
            var _settings = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;
            _settings.WaterMeshDetailing       = (WaterQualityLevelSettings.WaterMeshQualityEnum)EnumPopup("Mesh Detailing", "", _settings.WaterMeshDetailing, VideoTips.QuadTreeMeshDetailing);
            _settings.MeshDetailingFarDistance = IntSlider("Mesh Detailing Far Distance", "", _settings.MeshDetailingFarDistance, 250, 10000, VideoTips.QuadTreeMeshDetailingFarDistance);

        }

        void RenderingSetting()
        {
            var _settings          = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;
            var _settingsContainer = KWS_WaterSettingsRuntimeLoader._waterSystemQualitySettings;
            
            ReadSelectedThirdPartyFog();
            var selectedThirdPartyFogMethod = WaterSystem.ThirdPartyFogAssetsDescriptions[_settingsContainer.SelectedThirdPartyFogMethod];


            if (selectedThirdPartyFogMethod.CustomQueueOffset != 0)
            {
                EditorGUILayout.LabelField($"Min TransparentSortingPriority overrated by {selectedThirdPartyFogMethod.EditorName}", KWS_EditorUtils.NotesLabelStyleFade);
                _settings.WaterTransparentSortingPriority = IntSlider("Transparent Sorting Priority", "", _settings.WaterTransparentSortingPriority, selectedThirdPartyFogMethod.CustomQueueOffset, 50, VideoTips.TransparentSortingPriority);
            }
            else
            {
                _settings.WaterTransparentSortingPriority = IntSlider("Transparent Sorting Priority", "", _settings.WaterTransparentSortingPriority, -50, 50, VideoTips.TransparentSortingPriority);
            }

            //_settings.EnabledMeshRendering       = Toggle("Enabled Mesh Rendering", "", _settings.EnabledMeshRendering, link.EnabledMeshRendering), false);

            if (selectedThirdPartyFogMethod.DrawToDepth)
            {
                EditorGUILayout.LabelField($"Draw To Depth override by {selectedThirdPartyFogMethod.EditorName}", KWS_EditorUtils.NotesLabelStyleFade);
                GUI.enabled                      = false;
                _settings.DrawToPosteffectsDepth = Toggle("Draw To Depth", Description.Rendering.DrawToPosteffectsDepth, true, VideoTips.DrawToDepth);
                GUI.enabled                      = true;
            }
            else
            {
                _settings.DrawToPosteffectsDepth = Toggle("Draw To Depth", Description.Rendering.DrawToPosteffectsDepth, _settings.DrawToPosteffectsDepth, VideoTips.DrawToDepth);
            }


            _settings.WideAngleCameraRenderingMode = Toggle("Wide-Angle Camera Mode", "", _settings.WideAngleCameraRenderingMode, VideoTips.WideAngleCameraRenderingMode);
            //if (_waterSystem.UseTesselation)
            //{
            //    _waterSystem.WireframeMode = false;
            //    EditorGUILayout.LabelField($"Wireframe mode doesn't work with tesselation (water -> mesh -> use tesselation)", KWS_EditorUtils.NotesLabelStyleFade);
            //    GUI.enabled                           = false;
            //    _waterSystem.WireframeMode = Toggle("Wireframe Mode", "", _waterSystem.WireframeMode, nameof(_waterSystem.WireframeMode));
            //    GUI.enabled = _isActive;
            //}
            //else
            //{
            //    _waterSystem.WireframeMode = Toggle("Wireframe Mode", "", _waterSystem.WireframeMode, nameof(_waterSystem.WireframeMode));
            //}

            var assets = WaterSystem.ThirdPartyFogAssetsDescriptions;
            var fogDisplayedNames = new string[assets.Count + 1];
            for (var i = 0; i < assets.Count; i++)
            {
                fogDisplayedNames[i] = assets[i].EditorName;
            }
            EditorGUI.BeginChangeCheck();
            _settingsContainer.SelectedThirdPartyFogMethod = EditorGUILayout.Popup("Third-Party Fog Support", _settingsContainer.SelectedThirdPartyFogMethod, fogDisplayedNames);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateThirdPartyFog();
            }
            if (_settingsContainer.SelectedThirdPartyFogMethod != 0 && !_settingsContainer.IsThirdPartyFogAvailable)
            {
                EditorGUILayout.HelpBox($"Can't find the asset {WaterSystem.ThirdPartyFogAssetsDescriptions[_settingsContainer.SelectedThirdPartyFogMethod].EditorName}", MessageType.Error);
            }
#if KWS_DEBUG
            Line();

            //if (_settings.WaterMeshType == WaterMeshTypeEnum.InfiniteOcean || _settings.WaterMeshType == WaterMeshTypeEnum.FiniteBox)
            //{
            //    WaterSystem.DebugQuadtree = Toggle("Debug Quadtree", "", WaterSystem.DebugQuadtree, "");
            //}
            //_waterInstance.DebugAABB = Toggle("Debug AABB", "", _waterInstance.DebugAABB, "");
            //_waterInstance.DebugFft = Toggle("Debug Fft", "", _waterInstance.DebugFft, "");
            //_waterInstance.DebugDynamicWaves = Toggle("Debug Dynamic Waves", "", _waterInstance.DebugDynamicWaves, "");
            //_waterInstance.DebugOrthoDepth = Toggle("Debug Ortho Depth", "", _waterInstance.DebugOrthoDepth, "");
            //_waterInstance.DebugBuoyancy = Toggle("Debug Buoyancy", "", _waterInstance.DebugBuoyancy, "");
            //WaterSystem.DebugUpdateManager = Toggle("Debug Update Manager", "", WaterSystem.DebugUpdateManager, "");
            //Line();
#endif

        }

        void ReadSelectedThirdPartyFog()
        { 
            var _settingsContainer = KWS_WaterSettingsRuntimeLoader._waterSystemQualitySettings;
            //load enabled third-party asset for all water instances
            if (_settingsContainer.SelectedThirdPartyFogMethod == -1)
            {
                var defines = WaterSystem.ThirdPartyFogAssetsDescriptions.Select(n => n.ShaderDefine).ToList<string>();
                _settingsContainer.SelectedThirdPartyFogMethod = KWS_EditorUtils.GetEnabledDefineIndex(ShaderPaths.KWS_PlatformSpecificHelpers, defines);
            }

        }

       
        void UpdateThirdPartyFog()
        {  
            var _settings          = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;
            var _settingsContainer = KWS_WaterSettingsRuntimeLoader._waterSystemQualitySettings;
            
            if (_settingsContainer.SelectedThirdPartyFogMethod > 0)
            {
                var selectedMethod = WaterSystem.ThirdPartyFogAssetsDescriptions[_settingsContainer.SelectedThirdPartyFogMethod];
                if (!selectedMethod.IgnoreInclude)
                {
                    var inlcudeFileName = KW_Extensions.GetAssetsRelativePathToFile(selectedMethod.ShaderInclude, selectedMethod.AssetNameSearchPattern);
                    if (String.IsNullOrEmpty(inlcudeFileName))
                    {
                        _settingsContainer.IsThirdPartyFogAvailable = false;
                        Debug.LogError($"Can't find the asset {WaterSystem.ThirdPartyFogAssetsDescriptions[_settingsContainer.SelectedThirdPartyFogMethod].EditorName}");
                        return;
                    }
                    else _settingsContainer.IsThirdPartyFogAvailable = true;
                }
            }
            else
            {
                _settings.WaterTransparentSortingPriority   = -2;
                _settings.DrawToPosteffectsDepth            = false;
                _settingsContainer.IsThirdPartyFogAvailable = true;
            }

            //replace defines
            for (int i = 1; i < WaterSystem.ThirdPartyFogAssetsDescriptions.Count; i++)
            {
                var selectedMethod = WaterSystem.ThirdPartyFogAssetsDescriptions[i];
                SetShaderTextDefine(ShaderPaths.KWS_PlatformSpecificHelpers, false, selectedMethod.ShaderDefine, _settingsContainer.SelectedThirdPartyFogMethod == i);
            }

            //replace paths to assets
            if (_settingsContainer.SelectedThirdPartyFogMethod > 0)
            {
                var selectedMethod = WaterSystem.ThirdPartyFogAssetsDescriptions[_settingsContainer.SelectedThirdPartyFogMethod];
                if (!selectedMethod.IgnoreInclude)
                {
                    var inlcudeFileName = KW_Extensions.GetAssetsRelativePathToFile(selectedMethod.ShaderInclude, selectedMethod.AssetNameSearchPattern);
                    KWS_EditorUtils.ChangeShaderTextIncludePath(KWS_Settings.ShaderPaths.KWS_PlatformSpecificHelpers, selectedMethod.ShaderDefine, inlcudeFileName);
                }
            }
        
            var thirdPartySelectedFog = WaterSystem.ThirdPartyFogAssetsDescriptions[_settingsContainer.SelectedThirdPartyFogMethod];
            if (thirdPartySelectedFog.DrawToDepth) _settings.DrawToPosteffectsDepth = true;

            AssetDatabase.Refresh();

            var go = WaterSystem.Instance?.GetComponent<GameObject>();
            if (go)
            {
               
                go.SetActive(false);
                go.SetActive(true);
            }
        }


    }
#endif
}