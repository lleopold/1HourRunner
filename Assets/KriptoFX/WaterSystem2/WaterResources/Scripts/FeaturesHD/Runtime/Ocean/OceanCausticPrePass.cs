using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static KWS.KWS_CoreUtils;
using static KWS.KWS_ShaderConstants;

namespace KWS
{
    internal class OceanCausticPrePass : WaterPass
    {
        
        internal override string PassName => "Water.CausticPrePass";

        private static Dictionary<int, Mesh> _causticMeshes = new Dictionary<int, Mesh>();

        static Material _causticMaterial;       
        private CommandBuffer _cmd;

        const   float KWS_CAUSTIC_MULTIPLIER = 0.15f;
        private static bool   _lastDispersionUsing;
        

        static Dictionary<WaterQualityLevelSettings.CausticTextureResolutionQualityEnum, int> _causticQualityToMeshQuality = new Dictionary<WaterQualityLevelSettings.CausticTextureResolutionQualityEnum, int>()
        {
            {WaterQualityLevelSettings.CausticTextureResolutionQualityEnum.Extreme, 512},
            {WaterQualityLevelSettings.CausticTextureResolutionQualityEnum.Ultra, 384},
            {WaterQualityLevelSettings.CausticTextureResolutionQualityEnum.High, 256},
            {WaterQualityLevelSettings.CausticTextureResolutionQualityEnum.Medium, 192},
            {WaterQualityLevelSettings.CausticTextureResolutionQualityEnum.Low, 128},
        };

     
        public OceanCausticPrePass()
        {
            WaterSystem.OnAnyWaterSettingsChanged += OnAnyWaterSettingsChanged;
            
        }

        static void InitializeTextures(int size, int cascades, bool useDispersion)
        {
            var format = useDispersion ? KWS_CoreUtils.GetSafeR8G8B8A8_UNorm() : KWS_CoreUtils.GetSafeR8_UNorm();
            WaterSharedResources.CausticRTArray = KWS_CoreUtils.RTHandles.Alloc(size, size, colorFormat: format, name: "_CausticRTArray", useMipMap: true, autoGenerateMips: false, slices: cascades, dimension:TextureDimension.Tex2DArray);
            Shader.SetGlobalTexture(CausticID.KWS_CausticRTArray, WaterSharedResources.CausticRTArray);

            KWS_CoreUtils.ClearRenderTexture(WaterSharedResources.CausticRTArray.rt, ClearFlag.Color, new Color(KWS_CAUSTIC_MULTIPLIER, KWS_CAUSTIC_MULTIPLIER, KWS_CAUSTIC_MULTIPLIER));
            //this.WaterLog(WaterSharedResources.CausticRTArray);
        }

        void ReleaseTextures()
        {
            WaterSharedResources.CausticRTArray?.Release();
            WaterSharedResources.CausticRTArray = null;
            this.WaterLog(string.Empty, KW_Extensions.WaterLogMessageType.ReleaseRT);
        }

        public override void Release()
        {
            WaterSystem.OnAnyWaterSettingsChanged -= OnAnyWaterSettingsChanged;
           
            ReleaseTextures();
            KW_Extensions.SafeDestroy(_causticMaterial);

            foreach (var causticMesh in _causticMeshes) KW_Extensions.SafeDestroy(causticMesh.Value);
            _causticMeshes.Clear();

            this.WaterLog(string.Empty, KW_Extensions.WaterLogMessageType.Release);
        }

        private void OnAnyWaterSettingsChanged(WaterSystem.WaterSettingsCategory changedTabs)
        {
            if (KWS_Ocean.Instance == false) return;
            
            var useCausticEffect = WaterQualityLevelSettings.ResolveQualityOverride(WaterSystem.Instance.CausticEffect, WaterSystem.QualitySettings.UseCausticEffect);
            if(!useCausticEffect) return;

            if (changedTabs.HasTab(WaterSystem.WaterSettingsCategory.Caustic))
            {
                var size                 = (int)WaterSystem.QualitySettings.OceanCausticTextureResolutionQuality;
                var cascades             = Mathf.Min(2, KWS_Ocean.Instance.FftWavesCascades);
                var useCausticDispersion = WaterSystem.QualitySettings.UseOceanCausticDispersion;
                
                if (WaterSharedResources.CausticRTArray                == null 
                 || WaterSharedResources.CausticRTArray.rt.width       != size
                 || WaterSharedResources.CausticRTArray.rt.volumeDepth != cascades
                    ||_lastDispersionUsing != useCausticDispersion)
                {
                    _lastDispersionUsing = useCausticDispersion;
                    ReleaseTextures();
                    InitializeTextures(size, cascades, useCausticDispersion);
                }
            }
        }

        public override void ExecutePerFrame(HashSet<Camera> cameras, CustomFixedUpdates fixedUpdates)
        {
            if (WaterSystem.ForceDisableWaterRendering) return;
            
            if (KWS_Ocean.Instance == false) return;
            
            if (fixedUpdates.TimeScaledFramesCount_60fps == 0) return;
            
            var useCausticEffect = WaterQualityLevelSettings.ResolveQualityOverride(WaterSystem.Instance.CausticEffect, WaterSystem.QualitySettings.UseCausticEffect);
            if(useCausticEffect == false) return;

            if (_cmd == null) _cmd = new CommandBuffer() { name = PassName };
            _cmd.Clear();
            
            var size                 = (int)WaterSystem.QualitySettings.OceanCausticTextureResolutionQuality;
            var cascades             = Mathf.Min(2, KWS_Ocean.Instance.FftWavesCascades);
            var useCausticDispersion = WaterSystem.QualitySettings.UseOceanCausticDispersion;
            if (WaterSharedResources.CausticRTArray == null) InitializeTextures(size, cascades, useCausticDispersion);
            if(_causticMaterial                     == null) _causticMaterial                               =  KWS_CoreUtils.CreateMaterial(ShaderNames.CausticComputeShaderName);
            
            ComputeCaustic();
            if (WaterSharedResources.CausticRTArray != null) _cmd.GenerateMips(WaterSharedResources.CausticRTArray);
            
            Graphics.ExecuteCommandBuffer(_cmd);
        }


        void ComputeCaustic()
        {
            var maxCascades = Mathf.Min(KWS_Ocean.Instance.FftWavesCascades, 2);
            var mesh        = GetOrCreateCausticMesh(WaterSystem.QualitySettings.OceanCausticTextureResolutionQuality);
            _cmd.SetGlobalFloat("KWS_CausticDispersionStrength", WaterSystem.Instance.OceanCausticDispersionStrength);
            
            for (int idx = 0; idx < maxCascades; idx++)
            {
                CoreUtils.SetRenderTarget(_cmd, WaterSharedResources.CausticRTArray, ClearFlag.Color, Color.clear, depthSlice: idx);
                _cmd.SetGlobalInteger("KWS_CausticCascadeIndex", idx);
                _cmd.DrawMesh(mesh, Matrix4x4.identity, _causticMaterial, submeshIndex: 0, shaderPass: 0);
            }
        }

        internal static RenderTexture BakeCaustic(RenderTexture fftSource, float displacementScale, int fftCascadeIndexToBake, int frames)
        {
            if (KWS_Ocean.Instance == false) return null;
            
            var causticTex = new RenderTexture(256, 256, 0, GraphicsFormat.R8G8B8A8_UNorm);
            causticTex.useMipMap    = true;
            causticTex.antiAliasing = 8;
            causticTex.volumeDepth  = frames;
            causticTex.dimension    = TextureDimension.Tex2DArray;
            causticTex.Create();

            
            var cmd        = new CommandBuffer();
            
            var mesh = GetOrCreateCausticMesh(WaterQualityLevelSettings.CausticTextureResolutionQualityEnum.Extreme);
            
            cmd.SetKeyword("KWS_CAUSTIC_BAKE_MODE",      true);

            for (int frameIdx = 0; frameIdx < frames; frameIdx++)
            {
                cmd.SetGlobalFloat("KWS_DispacementScale",       displacementScale);
                cmd.SetGlobalTexture("KWS_BakedDisplacementFft", fftSource);
                cmd.SetGlobalInteger("KWS_CausticCascadeIndex",         fftCascadeIndexToBake);
                cmd.SetGlobalInteger("KWS_BakedDisplacementSliceIndex", frameIdx);
            
                CoreUtils.SetRenderTarget(cmd, causticTex, ClearFlag.Color, Color.clear, depthSlice: frameIdx);
                cmd.DrawMesh(mesh, Matrix4x4.identity, _causticMaterial, submeshIndex: 0, shaderPass: 0);
            }
            
            
            cmd.SetKeyword("KWS_CAUSTIC_BAKE_MODE", false);
            
            Graphics.ExecuteCommandBuffer(cmd);

            return causticTex;
        }


        static Mesh GetOrCreateCausticMesh(WaterQualityLevelSettings.CausticTextureResolutionQualityEnum quality)
        {
            if (!_causticQualityToMeshQuality.TryGetValue(quality, out var size)) size = 256;
            //var size = _causticQualityToMeshQuality[quality];
            if (!_causticMeshes.ContainsKey(size))
            {
                _causticMeshes.Add(size, MeshUtils.CreatePlaneMesh(size, 1.25f));
            }

            return _causticMeshes[size];
        }

    }
}