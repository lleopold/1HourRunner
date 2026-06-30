#if KWS_URP

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace KWS
{
    internal class KWS_WaterPassHandler
    {

        List<WaterPass> _waterPasses;


        BuoyancyPass     _buoyancyPass     = new();
        DynamicWavesPass _dynamicWavesPass = new();
        WetnessPass      _wetnessPass      = new();

        WaterPrePass           _waterPrePass           = new();
        VolumetricLightingPass _volumetricLightingPass = new();
        CopyWaterColorPass     _copyWaterColorPass     = new();

        ScreenSpaceReflectionPass    _ssrPass              = new();
        private PlanarReflectionPass _planarReflectionPass = new();
        ReflectionFinalPass          _reflectionFinalPass  = new();
        DrawMeshPass                 _drawMeshPass         = new();
        UnderwaterPass               _underwaterPass       = new();
        DrawToPosteffectsDepthPass   _drawToDepthPass      = new();
        
#if KWS_HD_MODULE_INSTALLED
        OceanRenderPass     _oceanRenderPass = new();
        OceanFftWavesPass   _fftWavesPass    = new();
        OceanCausticPrePass _causticPrePass  = new();
#endif


        private Dictionary<WaterQualityLevelSettings.RefractionResolutionEnum, Downsampling> _downsamplingQuality = new ()
        {
            { WaterQualityLevelSettings.RefractionResolutionEnum.Full, Downsampling.None },
            { WaterQualityLevelSettings.RefractionResolutionEnum.Half, Downsampling._2xBilinear },
            { WaterQualityLevelSettings.RefractionResolutionEnum.Quarter, Downsampling._4xBilinear },
        };
        private Downsampling _defaultDownsampling = Downsampling._2xBilinear;
        private FieldInfo    _downsampleProp;

        internal KWS_WaterPassHandler()
        {
            
            _waterPrePass.renderPassEvent           = RenderPassEvent.BeforeRenderingSkybox;
            _volumetricLightingPass.renderPassEvent = RenderPassEvent.BeforeRenderingSkybox;

            _ssrPass.renderPassEvent             = RenderPassEvent.BeforeRenderingTransparents;
            _reflectionFinalPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            
            _dynamicWavesPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            _copyWaterColorPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            _drawToDepthPass.renderPassEvent    = RenderPassEvent.AfterRenderingTransparents;
            _underwaterPass.renderPassEvent     = RenderPassEvent.AfterRenderingTransparents;

            _waterPasses = new List<WaterPass>
            {
#if KWS_HD_MODULE_INSTALLED
              _oceanRenderPass, _fftWavesPass, _causticPrePass,
#endif
                _dynamicWavesPass, _wetnessPass, _buoyancyPass, 
                _waterPrePass, _volumetricLightingPass, _copyWaterColorPass,
                _ssrPass, _planarReflectionPass, _reflectionFinalPass, _drawMeshPass, _underwaterPass, _drawToDepthPass
            };



            //#if UNITY_EDITOR
            var urpAsset = UniversalRenderPipeline.asset;
            if (urpAsset != null)
            {
                urpAsset.supportsCameraOpaqueTexture = true;
                urpAsset.supportsCameraDepthTexture  = true;
                _defaultDownsampling                 = urpAsset.opaqueDownsampling;
            }
//#endif
            
        }

        void SetAssetSettings(UniversalAdditionalCameraData data, UniversalRenderPipelineAsset urpAsset)
        {
            data.requiresColorOption = CameraOverrideOption.On;
            data.requiresDepthOption = CameraOverrideOption.On;
            if (urpAsset.opaqueDownsampling != _defaultDownsampling)
            {
                _defaultDownsampling = urpAsset.opaqueDownsampling;
                Debug.Log("downsample changed " + _defaultDownsampling);
            }

            _downsampleProp = urpAsset.GetType().GetField("m_OpaqueDownsampling", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var downsample = KWS_CoreUtils.CanRenderUnderwater() ? Downsampling.None : _downsamplingQuality[WaterSystem.QualitySettings.RefractionResolution];
            if (_downsampleProp != null) _downsampleProp.SetValue(urpAsset, downsample);

        }

        void RestoreAssetSettings()
        {
            var urpAsset = UniversalRenderPipeline.asset;
            if(urpAsset == null) return;
            var prop     = urpAsset.GetType().GetField("m_OpaqueDownsampling", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            prop.SetValue(urpAsset, _defaultDownsampling);
        }


        internal void OnBeforeFrameRendering(HashSet<Camera> cameras, CustomFixedUpdates fixedUpdates)
        {
            foreach (var waterPass in _waterPasses) waterPass.ExecutePerFrame(cameras, fixedUpdates);

        }


        internal void OnBeforeCameraRendering(Camera cam, ScriptableRenderContext ctx)
        {
            try
            {
                var urpAsset = UniversalRenderPipeline.asset;
                if (urpAsset == null) return;

                var data = cam.GetUniversalAdditionalCameraData();
                if (data == null) return;
                SetAssetSettings(data, urpAsset);

                var cameraSize = KWS_CoreUtils.GetScreenSizeLimited(KWS_CoreUtils.SinglePassStereoEnabled);
                KWS_CoreUtils.RTHandles.SetReferenceSize(cameraSize.x, cameraSize.y);

                WaterPass.WaterPassContext waterContext = default;
                waterContext.cam = cam;

                waterContext.RenderContext        = ctx;
                waterContext.AdditionalCameraData = data;

                foreach (var waterPass in _waterPasses)
                {
                    waterPass.SetWaterContext(waterContext);
                    waterPass.ExecuteBeforeCameraRendering(cam, ctx);
                }

                _waterPrePass.renderPassEvent = KWS_TileZoneManager.VisibleClipMeshZones.Count > 0 ? 
                    RenderPassEvent.AfterRenderingPrePasses : RenderPassEvent.BeforeRenderingSkybox; //prepass must be renderer before Decal buffer 
                
                var srpRenderer = data.scriptableRenderer;
                _waterPrePass.ConfigureInput(ScriptableRenderPassInput.Depth); //we need depth texture before "copy color" pass and caustic rendering
                
                _drawToDepthPass.DepthPassWriteAccess  = true;
                _underwaterPass.ColorPassWriteAccess   = true;

                srpRenderer.EnqueuePass(_waterPrePass);
                //srpRenderer.EnqueuePass(_motionVectorsPass);

                srpRenderer.EnqueuePass(_dynamicWavesPass);

                if (WaterSystem.QualitySettings.UseWetEffect) srpRenderer.EnqueuePass(_wetnessPass);
                if (WaterSystem.QualitySettings.UseVolumetricLight) srpRenderer.EnqueuePass(_volumetricLightingPass);
               
                if (WaterSystem.QualitySettings.UseScreenSpaceReflection) srpRenderer.EnqueuePass(_ssrPass);
                if (WaterSystem.QualitySettings.UseScreenSpaceReflection || WaterSystem.QualitySettings.UsePlanarReflection) srpRenderer.EnqueuePass(_reflectionFinalPass);

                if (WaterSystem.QualitySettings.UseUnderwaterEffect && WaterSystem.Instance.UseWaterDropsEffect) srpRenderer.EnqueuePass(_copyWaterColorPass);
                if (WaterSystem.QualitySettings.UseUnderwaterEffect) srpRenderer.EnqueuePass(_underwaterPass);
                if (WaterSystem.QualitySettings.DrawToPosteffectsDepth) srpRenderer.EnqueuePass(_drawToDepthPass);

            }
            catch (Exception e)
            {
                Debug.LogError("Water rendering error: " + e.InnerException);
            }
        }

        internal void OnAfterCameraRendering(Camera cam, ScriptableRenderContext ctx)
        {
            if (UniversalRenderPipeline.asset == null) return;

            if (_downsampleProp               != null) _downsampleProp.SetValue(UniversalRenderPipeline.asset, _defaultDownsampling);
        }


        public void Release()
        {
            if (_waterPasses != null)
            {
                foreach (var waterPass in _waterPasses) waterPass?.Release();
            }

            RestoreAssetSettings();
        }
    }
}
#endif