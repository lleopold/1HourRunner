#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static KWS.KWS_DynamicWavesSimulationZone;
using static KWS.KWS_EditorUtils;
using static KWS.KWS_EditorVideoTipsSettings;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;


namespace KWS
{
    [CustomEditor(typeof(KWS_DynamicWavesSimulationZone))]
    public class KWS_EditorDynamicWavesSimulationZones : Editor
    {
        private KWS_DynamicWavesSimulationZone _target;

        private static string _usedSelectedCachePath;

        GUIStyle     _perfStyle;
        private bool _isZoneAllowed;


        public override void OnInspectorGUI()
        {
            _target = (KWS_DynamicWavesSimulationZone)target;

            if (WaterSystem.Instance == null)
            {
                WaterSystem.CreateWaterSystemManager();
            }
            
            Undo.RecordObject(_target, "Changed Dynamic Waves Simulation Zone");

            EditorGUI.BeginChangeCheck();
            EditorGUIUtility.labelWidth = 210;

            EditorGUILayout.Space(10);

            if (KWS_UpdateManager.EditorRefreshScale < 0.99f)
            {
                EditorGUILayout.HelpBox("The scene view update mode \"always refresh\" is turned off, so the simulation doesn’t update every frame", MessageType.Warning);
                EditorGUILayout.Space(20);
            }

            ZoneLimitSettings();

            EditorGUI.BeginChangeCheck();

            KWS2_Tab(ref _target.ShowSimulationSettings, "Simulation Settings", SimulationSettings, WaterSystem.WaterSettingsCategory.SimulationZone, null, foldoutSpace: 14);

            KWS2_TabWithEnabledToogle(ref _target.UseFoamTexture, ref _target.ShowFoamTextureSettings, "Foam Texture", FoamTextureSettings, WaterSystem.WaterSettingsCategory.SimulationZone, VideoTips.ZoneFoamTexture, foldoutSpace: 14);
            

#if KWS_HD_MODULE_INSTALLED
            if (_target.ZoneType != SimulationZoneTypeMode.BakedSimulation)
            {
                KWS2_TabWithEnabledToogle(ref _target.UseFoamParticles,                  ref _target.ShowFoamParticlesSettings, "Foam Particles", FoamParticlesSettings, WaterSystem.WaterSettingsCategory.SimulationZone, VideoTips.ZoneFoamParticles, foldoutSpace: 14);
                
            }
#endif

            KWS2_TabWithEnabledToogle(ref _target.UseSplashParticles, ref _target.ShowSplashSettings, "Splash Particles", SplashParticlesSettings, WaterSystem.WaterSettingsCategory.SimulationZone, VideoTips.ZoneSplashes, foldoutSpace: 14);
            if (EditorGUI.EndChangeCheck())
            {
                if (_target.ZoneType == SimulationZoneTypeMode.BakedSimulation)
                {
                    _target.UpdateBakedData();
                }
            }

            if (_target.ZoneType != SimulationZoneTypeMode.MovableZone) BakeSettings();

            if (EditorGUI.EndChangeCheck())
            {
                _target.ValueChanged();
                EditorUtility.SetDirty(_target);
            }
        }

        void ZoneLimitSettings()
        {
            _isZoneAllowed = _target.IsZoneAllowed();
            if (!_isZoneAllowed)
            {
                EditorGUILayout.HelpBox("Selected zone is too large and exceeds GPU memory limits and performance. Reduce zone size or Simulation Resolution", MessageType.Error);
                GUI.enabled = false;
                Line();
            }
        }

        void SimulationPerformanceInfo()
        {
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            EditorGUILayout.Space(-12);

            var   renderedPixels  = _target.TextureSize.x              * _target.TextureSize.y * 7; //data, addData, ColorData, Mask, DepthMask, MaskColor, Normals
            float referencePixels = _target.MaxSimulationTexturePixels * 7f;                        // baseline zone
            float fpsScale        = _target.SimulationFPS              / 60f;                     
            float relativeLoad    = fpsScale * renderedPixels             / referencePixels;

            GetLoadInfo(relativeLoad, "Simulation load");

            string FormatNumber(int value)
            {
                if (value >= 1_000_000)
                    return (value / 1_000_000f).ToString("0.#") + " million";
                if (value >= 1_000)
                    return (value / 1_000f).ToString("0.#") + " thousand";
                return value.ToString();
            }

            string displayValue = FormatNumber(renderedPixels);

            EditorGUILayout.LabelField("Rendered pixels:  " + displayValue, KWS_EditorUtils.NotesLabelStyleFade);

            GUILayout.EndHorizontal();
        }

        void FoamPerformanceInfo()
        {
            float maxParticles  = (int)FoamParticlesMaxLimitEnum._1Million;
            float particlesLoad = 1f * (int)_target.MaxFoamParticlesBudget / maxParticles;
            float emissionLoad  = _target.FoamEmissionRate;

            float minLoad      = 0.2f;
            float relativeLoad = minLoad + (1f - minLoad) * emissionLoad;
            relativeLoad *= particlesLoad;

            GetLoadInfo(relativeLoad, "Foam particle load");
        }

        void SplashPerformanceInfo()
        {
            float maxParticles  = (int)SplashParticlesMaxLimitEnum._50k;
            float particleRatio = (float)_target.MaxSplashParticlesBudget / maxParticles;

            float emissionRate      = Mathf.Clamp01(_target.SplashEmissionRate);
            float nonlinearEmission = Mathf.Pow(emissionRate, 0.5f);

            float minBaseLoad = 0.02f;
            float baseLoad    = Mathf.Lerp(minBaseLoad, 1f, emissionRate) * particleRatio;

            // ---------------- Receive Shadows ----------------
            float shadowReceiveMultiplier = _target.ReceiveShadowMode switch
            {
                SplashReceiveShadowModeEnum.Disabled               => 0f,
                SplashReceiveShadowModeEnum.DirectionalLowQuality  => 0.25f,
                SplashReceiveShadowModeEnum.DirectionalHighQuality => 2.5f,
                SplashReceiveShadowModeEnum.AllShadowsLowQuality   => 0.5f,
                SplashReceiveShadowModeEnum.AllShadowsHighQuality  => 4f,
                _                                                  => 0f
            };

            // ---------------- Cast Shadows ----------------
            float shadowCastMultiplier = _target.CastShadowMode switch
            {
                SplashCasticShadowModeEnum.Disabled    => 0f,
                SplashCasticShadowModeEnum.LowQuality  => 1.5f,
                SplashCasticShadowModeEnum.HighQuality => 4f,
                _                                      => 0f
            };

            // ---------------- Final load ----------------

            float shadowCastWeight    = 0.4f;
            float shadowReceiveWeight = 0.4f;

            float shadowCost  = baseLoad * shadowCastMultiplier    * shadowCastWeight;
            float receiveCost = baseLoad * shadowReceiveMultiplier * shadowReceiveWeight;

            float totalLoad = baseLoad + shadowCost + receiveCost;

            GetLoadInfo(totalLoad, "Splash particle load");
        }

        private void GetLoadInfo(float relativeLoad, string text)
        {
            GUILayout.Space(4);
            string performanceText;
            Color  color;

            if (relativeLoad < 0.25f)
            {
                performanceText = "Low";
                color           = Color.green;
            }
            else if (relativeLoad < 0.5f)
            {
                performanceText = "Medium";
                color           = Color.yellow;
            }
            else if (relativeLoad < 0.75f)
            {
                performanceText = "High";
                color           = new Color(1f, 0.55f, 0f);
            }
            else
            {
                performanceText = "Extreme";
                color           = new Color(1f, 0.1f, 0.1f);
            }

            if (_perfStyle == null) _perfStyle = new GUIStyle(EditorStyles.label);
            _perfStyle.normal.textColor = color;
            _perfStyle.fontStyle        = FontStyle.Bold;

            float labelWidth = GUI.skin.label.CalcSize(new GUIContent(text + ":")).x + 4f;


            GUILayout.BeginHorizontal();
            GUILayout.Space(15);
            GUILayout.Label(text + ": ",                                       EditorStyles.label, GUILayout.Width(labelWidth));
            GUILayout.Label($"{performanceText} ({(relativeLoad * 100f):0}%)", _perfStyle);

            GUILayout.EndHorizontal();
        }

        private void BakeSettings()
        {
            EditorGUILayout.Space(20);

            if (_target.ResetCachePath)
            {
                SetSaveFolderPath(string.Empty);
                _target.ResetCachePath = false;
            }

            var isHorizontalUsed = false;
            EditorGUILayout.BeginHorizontal();
            isHorizontalUsed = true;

            EditorGUI.BeginChangeCheck();

            GUI.enabled = (_target._isBakeMode == false);
            var isStartBakingPressed = GUILayout.Toggle(false, "Start Cache", "Button");
            GUI.enabled = true;

            if (EditorGUI.EndChangeCheck())
            {
                if (isStartBakingPressed)
                {
                    if (EditorUtility.DisplayDialog("Start Precomputation?", "This will overwrite the existing simulation cache. Do you want to continue?",
                                                    "Start", "Cancel"))
                    {
                        ClearSimulationCache();
                        StartBaking(_target);
                    }
                }
            }

            EditorGUI.BeginChangeCheck();

            GUI.enabled = (_target._isBakeMode == true);
            var isEndBakingPresset = GUILayout.Toggle(false, "Stop & Save", "Button");
            GUI.enabled = true;

            if (EditorGUI.EndChangeCheck())
            {
                if (isEndBakingPresset)
                {
                    StopBaking(_target);
                }
            }


            EditorGUI.BeginChangeCheck();
            var isClearCache = GUILayout.Toggle(false, "Clear Cache", "Button");
            if (EditorGUI.EndChangeCheck())
            {
                if (isClearCache && EditorUtility.DisplayDialog("Confirm Deletion",
                                                                "Are you sure you want to delete the precomputed cache textures?",
                                                                "Yes",
                                                                "Cancel"))
                {
                    ClearSimulationCache();
                    _target.ForceUpdateZone();
                }
            }

            if (WaterSystem.Instance)
            {
                WaterSystem.Instance.AutoUpdateIntersections = GUILayout.Toggle(WaterSystem.Instance.AutoUpdateIntersections, "Auto Update Intersections", "Button");
            }
            else
            {
                GUI.enabled = false;
                GUILayout.Toggle(false, "Auto Update Intersections", "Button");
                GUI.enabled = true;
            }

            if (isHorizontalUsed) EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Cache Path:  " + _usedSelectedCachePath, KWS_EditorUtils.NotesLabelStyleFade);
        }


        void SimulationSettings()
        {
            var isBakedSim   = _target.SavedDepth != null;
            var isBakedZone = _target.ZoneType   == SimulationZoneTypeMode.BakedSimulation;

            if (isBakedSim == false || isBakedZone == false)
            {
                SimulationPerformanceInfo();
                Line();

            }
           
            if (isBakedSim)
            {
                EditorGUILayout.HelpBox("You can't change some parameters of a precomputed simulation. " + Environment.NewLine +
                                        "Clear the simulation or recompute it again with new parameters", MessageType.Info);
            }

            EditorGUI.BeginChangeCheck();
            _target.ZoneType = (SimulationZoneTypeMode)EnumPopup("Zone Type", "", _target.ZoneType, VideoTips.ZoneType);
            if (EditorGUI.EndChangeCheck())
            {
                if ( _target.ZoneType   == SimulationZoneTypeMode.BakedSimulation)
                {
                    _target.UpdateBakedData();
                }
                else
                {
                    _target.ReleaseBakedData();
                }
            }


            if (isBakedSim) GUI.enabled = false;


            if (_target.ZoneType == SimulationZoneTypeMode.MovableZone)
            {
                _target.FollowTarget = (GameObject)EditorGUILayout.ObjectField(_target.FollowTarget, typeof(GameObject), true);
            }

            _target.IntersectionLayerMask = MaskField("Intersection Layer Mask", "", _target.IntersectionLayerMask, VideoTips.ZoneIntersectionLayerMask);

            if (!_isZoneAllowed) GUI.enabled = true;
            _target.SimulationResolutionPerMeter = Slider("Simulation Resolution Per Meter", "", _target.SimulationResolutionPerMeter, 2, 10, VideoTips.ZoneSimulationResolutionPerMeter);
            if (!_isZoneAllowed) GUI.enabled = false;

            if (isBakedSim)
            {
                GUI.enabled = true;
            }

            _target.FlowSpeedMultiplier           = Slider("Flow Speed Multiplier",            "", _target.FlowSpeedMultiplier,           0.5f, 2.0f, VideoTips.ZoneFlowSpeedMultiplier);
            _target.SimulationWaveSpeedMultiplier = Slider("Simulation Wave Speed Multiplier", "", _target.SimulationWaveSpeedMultiplier, 0.25f, 1.0f, VideoTips.ZoneFlowSpeedMultiplier);//add video
            _target.SimulationFPS                 = IntSlider("Simulation FPS", KWS_EditorTextDescription.Waves.TimeScale, _target.SimulationFPS,    20, 120, VideoTips.SimulationFPSMultiplier);

            if (!isBakedZone)
            {
                _target.OceanWavesInfluenceStrength = Slider("Ocean Waves Influence Strength", "", _target.OceanWavesInfluenceStrength, 0.2f,  3f, VideoTips.ZoneEvaporationRate); //add video
                //_target.ShorelineDistance           = Slider("Shoreline Distance",             "", _target.ShorelineDistance,           10,  50, VideoTips.ZoneEvaporationRate); //add video
                _target.EvaporationRate    = Slider("Evaporation Rate",     "", _target.EvaporationRate,    0.01f, 1f, VideoTips.ZoneEvaporationRate);
                _target.RainDropsIntensity = Slider("Rain Drops Intensity", "", _target.RainDropsIntensity, 0.0f, 1f, VideoTips.ZoneEvaporationRate); //add video
            }
            _target.ReceiveWaterFlowFromOtherZones = Toggle("Receive Flow from Other Zones", "Allow this simulation zone to receive water flow from intersected neighboring zones.", _target.ReceiveWaterFlowFromOtherZones);
        }

        void FoamTextureSettings()
        {
            if (_target.ZoneType == SimulationZoneTypeMode.MovableZone)
            {
                _target.FoamType = FoamTypeEnum.FlowMap;
            }
            else
            {
                _target.FoamType = (FoamTypeEnum)EnumPopup("Foam Type", "", _target.FoamType, VideoTips.ZoneFlowType);
            }


            _target.FoamTextureType            = (WaterQualityLevelSettings.FoamTextureTypeEnum)EnumPopup("Foam Texture Type", "", _target.FoamTextureType, VideoTips.FoamType);
            _target.FoamTextureContrast        = Slider("Foam Texture Contrast",         "", _target.FoamTextureContrast,        0.25f, 5,     VideoTips.FoamContrast);
            _target.FoamTextureScaleMultiplier = Slider("Foam Texture Scale Multiplier", "", _target.FoamTextureScaleMultiplier, 0.1f,  10.0f, VideoTips.FoamScale);
            _target.FoamAlpha                  = Slider("Foam Alpha",                    "", _target.FoamAlpha, 0.01f,  1.0f, VideoTips.ZoneFoamAlpha);
            Line();

            if (_target.ZoneType != SimulationZoneTypeMode.BakedSimulation)
            {
                EditorGUILayout.LabelField("Also affects foam particle spawning", KWS_EditorUtils.NotesLabelStyleFade);
                _target.TurbulenceFoamStrength = Slider("Turbulence Foam Strength", "", _target.TurbulenceFoamStrength, 0.0f, 1.0f, VideoTips.ZoneFoamStrength); //todo video
                _target.WaveCrestFoamStrength  = Slider("Wave Crest Foam Strength", "", _target.WaveCrestFoamStrength,  0.0f, 1.0f, VideoTips.ZoneFoamStrength); //todo video
                _target.ShorelineFoamStrength  = Slider("Shoreline Foam Strength",  "", _target.ShorelineFoamStrength,  0.0f, 1.0f, VideoTips.ZoneFoamStrength); //todo video
                _target.FoamDisappearSpeed     = Slider("Foam Disappear Speed",     "", _target.FoamDisappearSpeed,     0.1f,   1.0f, VideoTips.ZoneFoamDisappearSpeed);
            }
        }

        void FoamParticlesSettings()
        {
            FoamPerformanceInfo();
            Line();

            _target.MaxFoamParticlesBudget   = (FoamParticlesMaxLimitEnum)EnumPopup("Max Particles", "", _target.MaxFoamParticlesBudget, VideoTips.ZoneMaxFoamParticles);
            _target.MaxFoamRenderingDistance = Slider("Max Rendering Distance", "", _target.MaxFoamRenderingDistance, 100, 500, VideoTips.ZoneFoamRenderingDistance);
            Line();

            _target.FoamParticlesScale           = Slider("Particles Scale",            "", _target.FoamParticlesScale,           0.1f,  2f, VideoTips.ZoneFoamParticlesScale);
            _target.FoamParticlesAlphaMultiplier = Slider("Particles Alpha Multiplier", "", _target.FoamParticlesAlphaMultiplier, 0.1f,  1f, VideoTips.ZoneFoamParticlesAlpha);
            _target.FoamParticleLifetime         = Slider("Particle Lifetime",          "", _target.FoamParticleLifetime,         0.25f, 6,  VideoTips.ZoneFoamParticlesAlpha);  //todo add video
            Line();

            EditorGUILayout.LabelField("Foam particle spawning is also affected by foam strength settings", KWS_EditorUtils.NotesLabelStyleFade);
            _target.FoamEmissionRate    = Slider("Emission Rate",      "", _target.FoamEmissionRate,    0f,   1f, VideoTips.ZoneEmissionRate);
            _target.FoamClumping        = Slider("Foam Clumping",      "", _target.FoamClumping,        0.1f, 0.6f,  VideoTips.ZoneEmissionRate); //todo add video

            Line();
            _target.UsePhytoplanktonEmission  = Toggle("Phytoplankton Glow", "", _target.UsePhytoplanktonEmission, VideoTips.ZonePhytoplanktonEmission);
        }

        void SplashParticlesSettings()
        {
            SplashPerformanceInfo();
            Line();

            if (_target.ZoneType   == SimulationZoneTypeMode.BakedSimulation)
            {
                _target.MaxSplashParticlesBudget       = (SplashParticlesMaxLimitEnum)EnumPopup("Max Particles", "", _target.MaxSplashParticlesBudget, VideoTips.ZoneMaxSplashesParticles);
                _target.SplashParticlesScale           = Slider("Particles Scale",            "", _target.SplashParticlesScale,           0.1f, 2f, VideoTips.ZoneSplashesParticlesScale);
                _target.SplashParticlesAlphaMultiplier = Slider("Particles Alpha Multiplier", "", _target.SplashParticlesAlphaMultiplier, 0.1f, 1f, VideoTips.ZoneSplashesParticlesAlpha);
                _target.BakedSplashEmissionRate        = Slider("Splash Emission Rate",       "", _target.BakedSplashEmissionRate,        0f,   1f, VideoTips.ZoneEmissionRate);
            }
            else
            {
                _target.MaxSplashParticlesBudget   = (SplashParticlesMaxLimitEnum)EnumPopup("Max Particles", "", _target.MaxSplashParticlesBudget, VideoTips.ZoneMaxSplashesParticles);
                _target.MaxSplashRenderingDistance = Slider("Max Rendering Distance", "", _target.MaxSplashRenderingDistance, 100, 1000, VideoTips.ZoneSplashesRenderingDistance);
                Line();
                
                _target.SplashParticlesScale           = Slider("Particles Scale",            "", _target.SplashParticlesScale,           0.1f, 2f, VideoTips.ZoneSplashesParticlesScale);
                _target.SplashParticlesAlphaMultiplier = Slider("Particles Alpha Multiplier", "", _target.SplashParticlesAlphaMultiplier, 0.1f, 1f, VideoTips.ZoneSplashesParticlesAlpha);
                Line();
                
                _target.SplashEmissionRate     = Slider("Splash Emission Rate", "", _target.SplashEmissionRate,     0f, 1f, VideoTips.ZoneEmissionRate);
            }
           
            Line();
            _target.ReceiveShadowMode = (SplashReceiveShadowModeEnum)EnumPopup("Receive Shadow Mode", "", _target.ReceiveShadowMode);
            _target.CastShadowMode    = (SplashCasticShadowModeEnum)EnumPopup("Cast Shadow Mode",     "", _target.CastShadowMode);
        }


        public static void StartBaking(KWS_DynamicWavesSimulationZone zone)
        {
            if (!zone.transform) return;
            //save textures to disk

            zone._isBakeMode                                   = true;
            KWS_DynamicWavesSimulationZone.IsAnyZoneInBakeMode = true;

            zone.ForceUpdateZone();
        }

        public static void StopBaking(KWS_DynamicWavesSimulationZone zone)
        {
            zone._isBakeMode                                   = false;
            KWS_DynamicWavesSimulationZone.IsAnyZoneInBakeMode = false;

            WaterSystem.GlobalTimeScale = 1;
            if (zone._bakeDepthRT)
            {
                SaveBakedTextures(zone);
            }
        }

        public static void SetSaveFolderPath(string assetRelativePath)
        {
            _usedSelectedCachePath = assetRelativePath;
        }

        public void ClearSimulationCache()
        {
            TryDelete(_target.SavedDepth);
            TryDelete(_target.SavedDistanceField);
            TryDelete(_target.SavedDynamicWavesSimulation);
            TryDelete(_target.SavedDynamicWavesAdditionalDataSimulation);

            TryDelete(_target.SavedMesh);
            TryDelete(_target.SavedMeshMaterial);

            TryDelete(_target.SavedSplashMesh);
            TryDelete(_target.SavedSplashMaterial);

            _target.ReleaseBakedData();

            _target.SavedDepth                  = null;
            _target.SavedDistanceField          = null;
            _target.SavedDynamicWavesSimulation = null;
            _target.SavedMesh                   = null;
            _target.SavedMeshMaterial           = null;
            _target.SavedSplashMesh             = null;
            _target.SavedSplashMaterial         = null;

            UnityEditor.AssetDatabase.Refresh();
        }

        void TryDelete(Object texture)
        {
            if (texture == null) return;

            var path = UnityEditor.AssetDatabase.GetAssetPath(texture);
            if (!string.IsNullOrEmpty(path))
            {
                UnityEditor.AssetDatabase.DeleteAsset(path);
            }
        }


        static void UpdateSaveFolderPath(KWS_DynamicWavesSimulationZone zone, bool requireSaveFolderPanel)
        {
            if (zone && zone.SavedDepth)
            {
                if (String.IsNullOrEmpty(_usedSelectedCachePath))
                {
                    _usedSelectedCachePath = UnityEditor.AssetDatabase.GetAssetPath(zone.SavedDepth);
                    if (!String.IsNullOrEmpty(_usedSelectedCachePath))
                    {
                        _usedSelectedCachePath = Path.GetDirectoryName(Path.GetRelativePath("Assets", Path.Combine("Assets", _usedSelectedCachePath)));
                    }
                }
            }

            if (requireSaveFolderPanel && String.IsNullOrEmpty(_usedSelectedCachePath))
            {
                _usedSelectedCachePath = UnityEditor.EditorUtility.SaveFolderPanel("Save texture location", _usedSelectedCachePath, "");
            }
        }


        private static void SaveBakedTextures(KWS_DynamicWavesSimulationZone zone)
        {
            UpdateSaveFolderPath(zone, requireSaveFolderPanel: true);

            if (String.IsNullOrEmpty(_usedSelectedCachePath))
            {
                return;
            }

            var randFileName = Path.GetRandomFileName().Substring(0, 5).ToUpper();

            var depthPath    = Path.Combine(_usedSelectedCachePath, "DepthTexture_"           + randFileName);
            var sdfDepthPath = Path.Combine(_usedSelectedCachePath, "DistanceFieldTexture_"   + randFileName);
            var simDataPath  = Path.Combine(_usedSelectedCachePath, "DynamicWavesSimulation_" + randFileName);


            zone._simulationData.PostprocessPrebakedSimData(zone);

            zone._bakeDepthRT.SaveRenderTextureDepth32(depthPath, autoRefresh: false);
            zone._bakeDepthSdfRT.SaveRenderTexture(sdfDepthPath, autoRefresh: false);
            zone._simulationData.GetTarget.rt.SaveRenderTexture(simDataPath, autoRefresh: false);
            AssetDatabase.Refresh();

            zone.SavedDepth                  = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(depthPath.GetRelativeToAssetsPath()    + ".kwsTexture");
            zone.SavedDistanceField          = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(sdfDepthPath.GetRelativeToAssetsPath() + ".kwsTexture");
            zone.SavedDynamicWavesSimulation = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(simDataPath.GetRelativeToAssetsPath()  + ".kwsTexture");


            if (zone.ZoneType == SimulationZoneTypeMode.BakedSimulation)
            {
                var zoneHeight = zone.Position.y + zone.Size.y * 0.5f - zone.bakedWaterLevel;

                float GetHeight(Color    px) => (px.a * zoneHeight + px.b * 22.0f - 2.0f);
                bool  GetAlphaMask(Color px) => (px.a > 0.0001f || (px.b * 22.0f - 2.0f) > 0.001);
                var savedMesh = MeshUtils.CreateZoneMesh(zone.SavedDynamicWavesSimulation, zone.BakedAreaSize, GetAlphaMask, GetHeight);

                var meshPath         = Path.Combine(_usedSelectedCachePath, "BakedMesh_"     + randFileName);
                var meshMaterialPath = Path.Combine(_usedSelectedCachePath, "BakedMaterial_" + randFileName);

                AssetDatabase.CreateAsset(savedMesh, meshPath.GetRelativeToAssetsPath() + ".asset");
                zone.SavedMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(meshPath.GetRelativeToAssetsPath() + ".asset");

                zone.SavedMeshMaterial = KWS_CoreUtils.CreateSerializedMaterial(WaterSystem.Instance, KWS_ShaderConstants.ShaderNames.WaterZoneInstanceShaderName, useWaterStencilMask: true);
                AssetDatabase.CreateAsset(zone.SavedMeshMaterial, meshMaterialPath.GetRelativeToAssetsPath() + ".asset");

                bool GetSpawnThreshold(Color center, Color right, Color top, Color additionalDataColor, out Vector3 direction)
                {
                    var centerVelocity = new Vector2((center.r - 0.5f) * 20.0f, (center.g - 0.5f) * 20.0f);
                    var rightVelocity  = new Vector2((right.r  - 0.5f) * 20.0f, (right.g  - 0.5f) * 20.0f);
                    var topVelocity    = new Vector2((top.r    - 0.5f) * 20.0f, (top.g    - 0.5f) * 20.0f);

                    float divergence = (centerVelocity - rightVelocity).magnitude + (centerVelocity - topVelocity).magnitude;
                    float waveSpeed  = centerVelocity.magnitude;
                    float height     = center.b * 22.0f - 2.0f;

                    direction = new Vector3((centerVelocity.x + rightVelocity.x + topVelocity.x) * 0.33f * waveSpeed, 0.1f, (centerVelocity.y + rightVelocity.y + topVelocity.y) * 0.33f * waveSpeed).normalized;

                    //float SPLASH_DIVERGENCE_SHORELINE_MUL = 0.5f;
                    //float SPLASH_MIN_DIVERGENCE_CHANCE = 0.94f;

                    //float shorelineMask = additionalDataColor.g;
                    //float divergenceStrength = Mathf.Lerp(1 - zone.RiverEmissionRateSplash, 1 - zone.ShorelineEmissionRateSplash, shorelineMask);
                    float divergenceStrength = 0.9f;
                    //divergenceStrength = Mathf.Lerp(divergenceStrength, divergenceStrength * SPLASH_DIVERGENCE_SHORELINE_MUL, shorelineMask);

                    float randomValue = Random.value;
                    if (height > 0.5 && waveSpeed > 2 && divergence > 0.6 && randomValue > divergenceStrength) return true;

                    return false;
                }

                var simAdditionalDataPath = Path.Combine(_usedSelectedCachePath, "DynamicWavesSimulationAdditionalData_" + randFileName);
                zone._simulationData.GetAdditionalTarget.rt.SaveRenderTexture(simAdditionalDataPath, autoRefresh: true);
                zone.SavedDynamicWavesAdditionalDataSimulation = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(simAdditionalDataPath.GetRelativeToAssetsPath() + ".kwsTexture");

                var savedSplashMesh = MeshUtils.CreateZoneSplashPointCloud(zone.SavedDynamicWavesSimulation, zone.SavedDynamicWavesAdditionalDataSimulation, zone.BakedAreaSize, GetSpawnThreshold, GetHeight, 0.0f, 2.0f);

                meshPath = Path.Combine(_usedSelectedCachePath, "BakedSplashMesh_" + randFileName);
                AssetDatabase.CreateAsset(savedSplashMesh, meshPath.GetRelativeToAssetsPath() + ".asset");
                zone.SavedSplashMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(meshPath.GetRelativeToAssetsPath() + ".asset");

                meshPath = Path.Combine(_usedSelectedCachePath, "BakedSplashMaterial_" + randFileName);
                var splashMaterial = Instantiate(Resources.Load<Material>("Material/SplashParticles"));
                AssetDatabase.CreateAsset(splashMaterial, meshPath.GetRelativeToAssetsPath() + ".asset");
                zone.SavedSplashMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(meshPath.GetRelativeToAssetsPath() + ".asset");


                zone.UpdateBakedData();
            }
        }


        void OnEnable()
        {
            foreach (var iZone in KWS_TileZoneManager.DynamicWavesZones)
            {
                var zone = (KWS_DynamicWavesSimulationZone)iZone;
                if (zone.SavedDepth) UpdateSaveFolderPath(zone, false);
            }
        }
    }


    [InitializeOnLoad]
    static class MoveDetector
    {
        static HashSet<KWS_TileZoneManager.IWaterZone> pendingZones = new();
        static double                                  lastUpdateTime;

        static MoveDetector()
        {
            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.postprocessModifications += OnPostprocessModifications;

            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
        }

        private static UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            if (!WaterSystem.Instance || !WaterSystem.Instance.AutoUpdateIntersections) return modifications;

            foreach (var mod in modifications)
            {
                var prop = mod.currentValue;
                if (prop == null || prop.target == null) continue;

                if (prop.target is not Transform tr) continue;

                foreach (var dynamicWavesZone in KWS_TileZoneManager.DynamicWavesZones)
                {
                    if (dynamicWavesZone.OrientedBounds.Contains(tr.position))
                    {
                        pendingZones.Add(dynamicWavesZone);
                    }
                }
            }


            return modifications;
        }

        private static void EditorUpdate()
        {
            if (pendingZones.Count == 0) return;

            double time = EditorApplication.timeSinceStartup;
            if (time - lastUpdateTime < 0.5) return;

            foreach (var iZone in pendingZones)
            {
                var zone = (KWS_DynamicWavesSimulationZone)iZone;
                if (zone && zone.ZoneType != SimulationZoneTypeMode.MovableZone)
                {
                    zone.ForceUpdateZone(false);
                }
            }

            pendingZones.Clear();
            lastUpdateTime = time;
        }
    }
}

#endif