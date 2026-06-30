using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static KWS.KWS_ShaderConstants;

namespace KWS
{
    internal class WaterPrePass : WaterPass
    {
        private static readonly int    KwsWaterPrePassTexelSize = Shader.PropertyToID("KWS_WaterPrePass_TexelSize");
        internal override       string PassName => "Water.PrePass";

        readonly Vector2 _rtScale          = new Vector2(0.35f, 0.35f);
        readonly Vector2 _rtScaleThickness = new Vector2(0.25f, 0.25f);

        
        private  KW_PyramidBlur _pyramidBlur = new KW_PyramidBlur();
        private  RTHandle       _tempIntersectionTensionRT;
        Material       _prePassMaterial;
        Material       _prePassMaterialCustomMesh;
        Material       _prePassMaterialZoneInstance;
        Material _prePassMaskThicknessDepth;

        enum WaterShaderPassEnum
        {
            Main = 0,
            Backface = 1,
            OceanMask = 2,
            TensionMask = 3,
            ClipMask = 4
        }
        
    
        public WaterPrePass()
        {
            _prePassMaterial             = KWS_CoreUtils.CreateMaterial(KWS_ShaderConstants.ShaderNames.WaterPrePassShaderName,             useWaterStencilMask: true);
            _prePassMaterialCustomMesh   = KWS_CoreUtils.CreateMaterial(KWS_ShaderConstants.ShaderNames.WaterPrePassCustomMeshShaderName,   useWaterStencilMask: true);
            _prePassMaterialZoneInstance = KWS_CoreUtils.CreateMaterial(KWS_ShaderConstants.ShaderNames.WaterPrePassZoneInstanceShaderName, useWaterStencilMask: true);
            _prePassMaskThicknessDepth   = KWS_CoreUtils.CreateMaterial(ShaderNames.ClipMaskShaderName,                                     useWaterStencilMask: false);
        }

        void InitializePrepassTextures()
        {
            if (WaterSharedResources.WaterPrePassRT0 != null) return;
            

            WaterSharedResources.WaterPrePassRT0 = KWS_CoreUtils.RTHandleAllocVR(_rtScale, name: "_waterPrePassRT0", colorFormat: KWS_CoreUtils.GetSafeR8G8B8A8_UNorm());
            WaterSharedResources.WaterPrePassRT1 = KWS_CoreUtils.RTHandleAllocVR(_rtScale, name: "_waterPrePassRT1", colorFormat: GraphicsFormat.R16G16_SNorm);
            WaterSharedResources.WaterDepthRT    = KWS_CoreUtils.RTHandleAllocVR(_rtScale, name: "_waterDepthRT",    depthBufferBits: DepthBits.Depth24, colorFormat: GraphicsFormat.None);
           
            Shader.SetGlobalTexture(MaskPassID.KWS_WaterPrePassRT0, WaterSharedResources.WaterPrePassRT0);
            Shader.SetGlobalTexture(MaskPassID.KWS_WaterPrePassRT1, WaterSharedResources.WaterPrePassRT1);
            Shader.SetGlobalTexture(MaskPassID.KWS_WaterDepthRT,    WaterSharedResources.WaterDepthRT);

            this.WaterLog(WaterSharedResources.WaterPrePassRT0, WaterSharedResources.WaterPrePassRT1, WaterSharedResources.WaterDepthRT);
        }

        void InitializeIntersectionHalflineTensionTextures()
        {
            if (WaterSharedResources.WaterIntersectionHalfLineTensionMaskRT != null) return;

            WaterSharedResources.WaterIntersectionHalfLineTensionMaskRT = KWS_CoreUtils.RTHandleAllocVR(_rtScale, name: "_waterIntersectionHalfLineTensionMaskRT", colorFormat: KWS_CoreUtils.GetSafeR8_UNorm());
            _tempIntersectionTensionRT                                  = KWS_CoreUtils.RTHandleAllocVR(_rtScale, name: "_tempIntersectionTensionRT",              colorFormat: KWS_CoreUtils.GetSafeR8_UNorm());
            
            Shader.SetGlobalTexture(MaskPassID.KWS_WaterIntersectionHalfLineTensionMaskRT, WaterSharedResources.WaterIntersectionHalfLineTensionMaskRT);
        }

        void CheckAndInitializeClipMaskTextures()
        {
            if (WaterSharedResources.WaterClipMaskDepthFront != null) return;
            
            WaterSharedResources.WaterClipMaskDepthFront     = KWS_CoreUtils.RTHandleAllocVR(_rtScaleThickness, name: "_waterClipMaskDepthFront",     depthBufferBits: DepthBits.Depth24, colorFormat: GraphicsFormat.None);
            WaterSharedResources.WaterClipMaskDepthBack      = KWS_CoreUtils.RTHandleAllocVR(_rtScaleThickness, name: "_waterClipMaskDepthBack",      depthBufferBits: DepthBits.Depth24, colorFormat: GraphicsFormat.None);
            WaterSharedResources.WaterClipMask         = KWS_CoreUtils.RTHandleAllocVR(_rtScaleThickness, name: "_waterClipMaskRT",  colorFormat: KWS_CoreUtils.GetSafeR8_UNorm());
            WaterSharedResources.WaterClipMaskDepth = KWS_CoreUtils.RTHandleAllocVR(_rtScaleThickness, name: "_waterClipMaskDepthRT", depthBufferBits: DepthBits.Depth24);
            
            Shader.SetGlobalTexture(MaskPassID.KWS_WaterClipMaskDepthFrontRT, WaterSharedResources.WaterClipMaskDepthFront);
            Shader.SetGlobalTexture(MaskPassID.KWS_WaterClipMaskDepthBackRT,  WaterSharedResources.WaterClipMaskDepthBack);
            Shader.SetGlobalTexture(MaskPassID.KWS_WaterClipMaskRT,           WaterSharedResources.WaterClipMask);
        }


        void ReleaseTextures()
        {
            WaterSharedResources.WaterPrePassRT0?.Release();
            WaterSharedResources.WaterPrePassRT1?.Release();
            WaterSharedResources.WaterDepthRT?.Release();
            WaterSharedResources.WaterIntersectionHalfLineTensionMaskRT?.Release();

            WaterSharedResources.WaterClipMaskDepthFront?.Release();
            WaterSharedResources.WaterClipMaskDepthBack?.Release();
            WaterSharedResources.WaterClipMask?.Release();
            WaterSharedResources.WaterClipMaskDepth?.Release();
            
            WaterSharedResources.WaterPrePassRT0              = WaterSharedResources.WaterPrePassRT1         = WaterSharedResources.WaterDepthRT         = WaterSharedResources.WaterIntersectionHalfLineTensionMaskRT = null;
            WaterSharedResources.WaterClipMaskDepthFront =  WaterSharedResources.WaterClipMaskDepthBack = WaterSharedResources.WaterClipMask = WaterSharedResources.WaterClipMaskDepth = null;

            _tempIntersectionTensionRT?.Release();
            _tempIntersectionTensionRT = null;

            this.WaterLog(string.Empty, KW_Extensions.WaterLogMessageType.ReleaseRT);
        }

        public override void Release()
        {
            ReleaseTextures();
            _pyramidBlur?.Release();
            KW_Extensions.SafeDestroy(_prePassMaterial, _prePassMaterialCustomMesh, _prePassMaterialZoneInstance, _prePassMaskThicknessDepth);
            _prePassMaterial             = null;
            _prePassMaterialCustomMesh   = null;
            _prePassMaterialZoneInstance = null;
            _prePassMaskThicknessDepth   = null;

            this.WaterLog(string.Empty, KW_Extensions.WaterLogMessageType.Release);
        }

        
        public override void ExecuteCommandBuffer(WaterPass.WaterPassContext waterContext)
        {
            if (WaterSystem.ForceDisableWaterRendering) return;
            
            var useUnderwaterEffect     = WaterQualityLevelSettings.ResolveQualityOverride(WaterSystem.Instance.UnderwaterEffect, WaterSystem.QualitySettings.UseUnderwaterEffect);
            var useIntersectionHalfline = useUnderwaterEffect && WaterSystem.Instance.UseUnderwaterHalfLineTensionEffect        && WaterSystem.IsCameraPartialUnderwater;
            var useOceanUnderwater      = useUnderwaterEffect && (WaterSystem.IsCameraPartialUnderwater || KWS_TileZoneManager.VisibleClipMeshZones.Count > 0);
            var useCustomMeshes         = KWS_TileZoneManager.VisibleCustomMeshZones.Count        > 0;
            var useClipMasking          = KWS_TileZoneManager.VisibleClipMeshZones.Count          > 0;
            var useBakedZones           = KWS_TileZoneManager.VisibleBakedDynamicWavesZones.Count > 0;
            
            
            if(useClipMasking) ExecuteMaskThickness(waterContext.cam, waterContext.cmd, useOceanUnderwater);
            
            ExecuteMainPrePass(waterContext.cam, waterContext.cmd, useOceanUnderwater);
            
            if(useCustomMeshes) ExecuteCustomMeshes(waterContext.cam, waterContext.cmd, _prePassMaterialCustomMesh, 0);
            if(useBakedZones) ExecuteBakedZoneMeshes(waterContext.cam, waterContext.cmd, _prePassMaterialZoneInstance, 0);
            if(useIntersectionHalfline) ExecuteHalfLinePrePass(waterContext.cam, waterContext.cmd, useClipMasking);
        }
        

        void ExecuteHalfLinePrePass(Camera cam, CommandBuffer cmd, bool useClipMasking)
        {
            InitializeIntersectionHalflineTensionTextures();
                
            var target = useClipMasking ? WaterSharedResources.WaterClipMask : WaterSharedResources.WaterPrePassRT0;
            cmd.BlitTriangleRTHandle(target, _tempIntersectionTensionRT, _prePassMaterial, ClearFlag.Color, Color.clear, pass: (int)WaterShaderPassEnum.TensionMask);
            var scale = WaterSystem.Instance.UnderwaterHalfLineTensionScale;
            scale = Mathf.Lerp(1f, 3f, scale);
            _pyramidBlur.ComputeBlurPyramid(scale, _tempIntersectionTensionRT, WaterSharedResources.WaterIntersectionHalfLineTensionMaskRT, cmd, _rtScale);
        }
        
        void ExecuteMainPrePass(Camera cam, CommandBuffer cmd, bool useOceanUnderwater)
        {
            InitializePrepassTextures();
            CoreUtils.SetRenderTarget(cmd, KWS_CoreUtils.GetMrt(WaterSharedResources.WaterPrePassRT0, WaterSharedResources.WaterPrePassRT1), WaterSharedResources.WaterDepthRT, ClearFlag.All, Color.clear);
            cmd.SetGlobalVector(MaskPassID.KWS_WaterPrePass_RTHandleScale, WaterSharedResources.WaterPrePassRT0.rtHandleProperties.rtHandleScale);
            
            #if KWS_HD_MODULE_INSTALLED
                DrawInstancedQuadTree(cam, cmd, useOceanUnderwater, isClipPass: false);
            #endif

        }
        

        void ExecuteCustomMeshes(Camera cam, CommandBuffer cmd,  Material mat, int shaderPass)
        {
            CoreUtils.SetRenderTarget(cmd, KWS_CoreUtils.GetMrt(WaterSharedResources.WaterPrePassRT0, WaterSharedResources.WaterPrePassRT1), WaterSharedResources.WaterDepthRT);
            
            var meshZones = KWS_TileZoneManager.VisibleCustomMeshZones;
            foreach (var zone in meshZones)
            {
                if (zone.OverrideMesh && zone.CustomMesh)
                {
                    cmd.DrawMesh(zone.CustomMesh, zone.CachedFittedMatrix, mat, 0, shaderPass);
                }
            }
        } 
        
        void ExecuteMaskThickness(Camera cam, CommandBuffer cmd,  bool useOceanUnderwater)
        {
            var meshZones = KWS_TileZoneManager.VisibleClipMeshZones;
            if(meshZones.Count == 0) return;
            
            CheckAndInitializeClipMaskTextures();
            cmd.SetGlobalVector(MaskPassID.KWS_WaterClipMask_RTHandleScale, WaterSharedResources.WaterClipMaskDepthFront.rtHandleProperties.rtHandleScale);
            CoreUtils.SetRenderTarget(cmd, WaterSharedResources.WaterClipMask, WaterSharedResources.WaterClipMaskDepth, ClearFlag.All,  Color.clear);
            
            CoreUtils.SetRenderTarget(cmd, WaterSharedResources.WaterClipMaskDepthFront, ClearFlag.Depth);
            foreach (var zone in meshZones)
            {
                if (zone.UseClipMask && zone.ClipMesh)
                {
                    cmd.DrawMesh(zone.ClipMesh, zone.CachedClipMatrix, _prePassMaskThicknessDepth, 0, 0);
                }
            }
            
            CoreUtils.SetRenderTarget(cmd, WaterSharedResources.WaterClipMaskDepthBack, ClearFlag.Depth);
            foreach (var zone in meshZones)
            {
                if (zone.UseClipMask && zone.ClipMesh)
                {
                    cmd.DrawMesh(zone.ClipMesh, zone.CachedClipMatrix, _prePassMaskThicknessDepth, 0, 1);
                }
            }

#if KWS_HD_MODULE_INSTALLED
            DrawInstancedQuadTree(cam, cmd, useOceanUnderwater, isClipPass: true);
#endif
            
        } 
        
        
        
        void ExecuteBakedZoneMeshes(Camera cam, CommandBuffer cmd,  Material mat, int shaderPass)
        {
            foreach (var zone in KWS_TileZoneManager.VisibleBakedDynamicWavesZones)
            {
                if (zone.ZoneType == KWS_DynamicWavesSimulationZone.SimulationZoneTypeMode.BakedSimulation && zone.SavedMesh && zone.IsZoneInitialized)
                {
                    var pos = zone.Position;
                    pos.y = zone.bakedWaterLevel;
                    var trs = Matrix4x4.TRS(pos, zone.Rotation, zone.Size);
                    
                    DynamicWavesPass.UpdateSimulationShaderParams(mat, zone);
                    cmd.DrawMesh(zone.SavedMesh, trs, mat, 0, shaderPass);
                }
            }

        }

#if KWS_HD_MODULE_INSTALLED

        void DrawInstancedQuadTree(Camera cam, CommandBuffer cmd, bool useOceanUnderwater, bool isClipPass)
        {
            var isFastMode = WaterSystem.IsCameraPartialUnderwater == false && KWS_TileZoneManager.VisibleLocalWaterZones.Count == 0;
            var lodBias    = isFastMode ? 3 : 0;
            if(isClipPass) lodBias = 5;

            if (!OceanRenderPass._meshQuadTree.TryGetRenderingContext(cam, lodBias, out var context)) return;

            if (context.chunkInstance == null || _prePassMaterial == null || context.visibleChunksArgs == null)
            {
                Debug.LogError($"Water PrePass.DrawInstancedQuadTree error: {context.chunkInstance}, {_prePassMaterial},  {context.visibleChunksArgs}");
                return;
            }

            if (context.visibleChunksComputeBuffer != null)
            {
                if (useOceanUnderwater)
                {
                    var oceanPassTarget = isClipPass ? WaterSharedResources.WaterClipMask : WaterSharedResources.WaterPrePassRT0;
                    CoreUtils.SetRenderTarget(cmd, oceanPassTarget);
                    
                    cmd.SetGlobalFloat(KWS_ShaderConstants.PrePass.KWS_OceanLevel, WaterSystem.Instance.WaterLevel);
                    cmd.SetGlobalVector(KwsWaterPrePassTexelSize, oceanPassTarget.rtHandleProperties.rtHandleScale);
                    cmd.BlitTriangle(_prePassMaterial, pass: (int)WaterShaderPassEnum.OceanMask);
                }

                if (isClipPass)
                {
                    CoreUtils.SetRenderTarget(cmd, WaterSharedResources.WaterClipMask, WaterSharedResources.WaterClipMaskDepth);
                }
                else
                {
                    CoreUtils.SetRenderTarget(cmd, KWS_CoreUtils.GetMrt(WaterSharedResources.WaterPrePassRT0, WaterSharedResources.WaterPrePassRT1), WaterSharedResources.WaterDepthRT);
                }

                cmd.SetGlobalBuffer(KWS_ShaderConstants.StructuredBuffers.InstancedMeshData, context.visibleChunksComputeBuffer);
                int shaderPass = (int)(isClipPass ? WaterShaderPassEnum.ClipMask : WaterShaderPassEnum.Main);
                
                cmd.DrawMeshInstancedIndirect(context.chunkInstance, submeshIndex: 0, _prePassMaterial, shaderPass, context.visibleChunksArgs);
            }
        }

#endif
     
    }
}