#if KWS_HD_MODULE_INSTALLED

#if UNITY_EDITOR


using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static KWS.KWS_EditorUtils;
using static KWS.KWS_EditorVideoTipsSettings;
//using static KWS.KWS_EditorUtils;
using static KWS.WaterSystem;
using Description = KWS.KWS_EditorTextDescription;
using link = KWS.KWS_EditorUrlLinks;
using static KWS.KWS_Settings;


namespace KWS
{
    [System.Serializable]
    [CustomEditor(typeof(KWS_Ocean))]
    internal partial class KWS_EditorOcean : Editor
    {
        private KWS_Ocean                 _oceanInstance;
        private WaterQualityLevelSettings _qualitySettings;

        private bool                     _isActive;
        private SceneView.SceneViewState _lastSceneView;


        void OnDestroy()
        {
            Release();
        }


        public override void OnInspectorGUI()
        {
            _oceanInstance = (KWS_Ocean)target;

            if (WaterSystem.Instance == null)
            {
                WaterSystem.CreateWaterSystemManager();
            }


            if (_oceanInstance.enabled && _oceanInstance.gameObject.activeSelf)
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
            Undo.RecordObject(_oceanInstance, "Changed ocean water parameters");

            EditorGUI.BeginChangeCheck();
            EditorGUIUtility.labelWidth = 200;

            GUI.enabled = _isActive;
            bool defaultValActive = true;

            EditorGUILayout.Space(20);
            if (KWS_UpdateManager.EditorRefreshScale < 0.99f)
            {
                EditorGUILayout.HelpBox("The scene view update mode \"always refresh\" is turned off, so the water rendering doesn’t update every frame", MessageType.Warning);
                EditorGUILayout.Space(20);
            }

            KWS_Tab(ref defaultValActive, "Ocean Settings", OceanSetting, WaterSettingsCategory.Ocean);


#if KWS_DEBUG
            EditorGUILayout.Space(20);
            _oceanInstance.DebugQuadtree      = Toggle("Debug Quadtree",       "", _oceanInstance.DebugQuadtree);
            _oceanInstance.DebugAABB          = Toggle("Debug AABB",           "", _oceanInstance.DebugAABB);
            _oceanInstance.DebugDynamicWaves  = Toggle("Debug Dynamic Waves",  "", _oceanInstance.DebugDynamicWaves);
            _oceanInstance.DebugBuoyancy      = Toggle("Debug Buoyancy",       "", _oceanInstance.DebugBuoyancy);
            _oceanInstance.DebugUpdateManager = Toggle("Debug Update Manager", "", _oceanInstance.DebugUpdateManager);
            _oceanInstance.DebugUnderwater = Toggle("Debug Underwater", "", _oceanInstance.DebugUnderwater);
            SaveFFT();

#endif

            if (EditorGUI.EndChangeCheck())
            {
                if (!Application.isPlaying)
                {
                    EditorUtility.SetDirty(_oceanInstance);
                    EditorSceneManager.MarkSceneDirty(_oceanInstance.gameObject.scene);
                }
            }
        }


        private void SaveFFT()
        {
#if KWS_DEBUG
            EditorGUI.BeginChangeCheck();
            var isSaveFFTPressed0 = GUILayout.Toggle(false, "SaveFFT", "Button");
            if (EditorGUI.EndChangeCheck())
            {
                if (isSaveFFTPressed0)
                {
                    //var usedSelectedCachePath = UnityEditor.EditorUtility.SaveFolderPanel("Save texture location", @"C:\Users\kript\Documents\GitHub\KWS2\KWS2\Assets\FFT\FFT_5meters\Turbulence_15%", "");
                    var usedSelectedCachePath = UnityEditor.EditorUtility.SaveFolderPanel("Save texture location", @"C:\Users\kript\Documents\GitHub\KWS2\KWS2\Assets\KriptoFX\WaterSystem2\WaterResources\BakedResources", "");
                   
                    System.IO.Directory.CreateDirectory(System.IO.Path.Combine(usedSelectedCachePath.GetRelativeToAssetsPath(), "Ocean"));
                    if (string.IsNullOrEmpty(usedSelectedCachePath)) return;

                    SaveFFTCascades(usedSelectedCachePath);
                }
            }
#endif
        }


        private void SaveFFTCascades(string folder)
        {
            var fps      = 12;
            var loopTime = 5;
            
            var frames   = loopTime * fps;
          
            var cascades                 = KWS_Ocean.Instance.FftWavesCascades;
            var displacementCausticScale = 0.125f;
            var sizes = new int[4]{128, 128, 64, 64};

            for (int cascadeIdx = 0; cascadeIdx < cascades; cascadeIdx++)
            {
                OceanFftWavesPass._instance.BakeFFT(frames, loopTime, sizes[cascadeIdx], cascades, cascadeIdx, out var fftData, out var normalsData);
                KWS_TextureArrayImporter.Write(fftData,     System.IO.Path.Combine(folder.GetRelativeToAssetsPath(), "Ocean", "FFT_"     + cascadeIdx));
                KWS_TextureArrayImporter.Write(normalsData, System.IO.Path.Combine(folder.GetRelativeToAssetsPath(), "Ocean", "Normals_" + cascadeIdx));
                
                Debug.Log($"FFT cascade {cascadeIdx + 1}/{cascades} saved");
                
                if (cascadeIdx == 1)
                {
                    var causticTex = OceanCausticPrePass.BakeCaustic(fftData, displacementCausticScale, cascadeIdx, frames);
                    KWS_TextureArrayImporter.Write(causticTex,     System.IO.Path.Combine(folder.GetRelativeToAssetsPath(), "Ocean", "Caustic"));
                    //causticTex.SaveRenderTextureToPNG(System.IO.Path.Combine(folder, "Caustic_Textures", "Caustic_" + i.ToString("D3")), useMips: false, autoRefresh: false);
                    //causticTex.Release();
                }
                fftData.Release();
                normalsData.Release();
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

            }

            Debug.Log("FFT bake complete");
        }

       
        void OceanSetting()
        {
            Line();

            _oceanInstance.WindZone = (WindZone)EditorGUILayout.ObjectField(_oceanInstance.WindZone, typeof(WindZone), true);
            if (_oceanInstance.WindZone != null)
            {
                _oceanInstance.WindZoneSpeedMultiplier      = Slider("Wind Speed Multiplier",      "", _oceanInstance.WindZoneSpeedMultiplier,      1,     50);
                _oceanInstance.WindZoneTurbulenceMultiplier = Slider("Wind Turbulence Multiplier", "", _oceanInstance.WindZoneTurbulenceMultiplier, 0.01f, 1.0f);
            }
            else
            {
                _oceanInstance.WindSpeed      = Slider("Wind Speed",      Description.Waves.WindSpeed,      _oceanInstance.WindSpeed,      0.1f, FFT.MaxWindSpeed, VideoTips.WindSpeed);
                _oceanInstance.WindRotation   = Slider("Wind Rotation",   Description.Waves.WindRotation,   _oceanInstance.WindRotation,   0.0f, 360.0f,           VideoTips.WindRotation);
                _oceanInstance.WindTurbulence = Slider("Wind Turbulence", Description.Waves.WindTurbulence, _oceanInstance.WindTurbulence, 0.0f, 1.0f,             VideoTips.WindTurbulence);
                _oceanInstance.TimeScale      = Slider("Time Scale",       Description.Waves.TimeScale,      _oceanInstance.TimeScale, 0.0f, 2.0f,             VideoTips.SimulationFPSMultiplier);
            }

            Line();
            _oceanInstance.FftWavesQuality  = (WaterQualityLevelSettings.FftWavesQualityEnum)EnumPopup("Waves Quality", Description.Waves.FftWavesQuality, _oceanInstance.FftWavesQuality, VideoTips.FftWavesQuality);
            _oceanInstance.FftWavesCascades = IntSlider("Simulation Cascades", "", _oceanInstance.FftWavesCascades, 1, 4, VideoTips.FftWavesCascades);
            _oceanInstance.WavesAreaScale   = Slider("Area Scale", "", _oceanInstance.WavesAreaScale, 0.2f, KWS_Settings.FFT.MaxWavesAreaScale, VideoTips.WavesAreaScale);

            Line();
            _oceanInstance.OceanFoam                   = (WaterQualityLevelSettings.QualityOverrideEnum)EnumPopup("Render Foam", "", _oceanInstance.OceanFoam, VideoTips.OceanFoam);
            _oceanInstance.OceanFoamStrength           = Slider("Ocean Foam Strength",            "", _oceanInstance.OceanFoamStrength,           0.0f,  1,  VideoTips.OceanFoamStrength);
            _oceanInstance.OceanFoamLifetimeMultiplier = Slider("Ocean Foam Lifetime Multiplier", "", _oceanInstance.OceanFoamLifetimeMultiplier, 0.25f, 1f, VideoTips.OceanFoamLifetimeMultiplier);

            Line();
            _oceanInstance.FoamTextureType            = (WaterQualityLevelSettings.FoamTextureTypeEnum)EnumPopup("Foam Texture Type", "", _oceanInstance.FoamTextureType, VideoTips.FoamType);
            _oceanInstance.FoamTextureContrast        = Slider("Foam Texture Contrast",         "", _oceanInstance.FoamTextureContrast,        0.25f, 5,     VideoTips.FoamContrast);
            _oceanInstance.FoamTextureScaleMultiplier = Slider("Foam Texture Scale Multiplier", "", _oceanInstance.FoamTextureScaleMultiplier, 0.1f,  10.0f, VideoTips.FoamScale);
            Line();

            _oceanInstance.RainDropsIntensity = Slider("Rain Drops Intensity", "", _oceanInstance.RainDropsIntensity, 0, 1, VideoTips.FoamScale); //todo add video
        }


    }
}

#endif

#endif