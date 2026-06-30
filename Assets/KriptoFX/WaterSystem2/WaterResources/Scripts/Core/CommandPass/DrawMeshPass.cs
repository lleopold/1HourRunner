using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


namespace KWS
{
    internal class DrawMeshPass : WaterPass
    {
        internal override string PassName => "Water.DrawMeshPass";

        private RenderParams _customMeshRenderParams;
        Material             _drawCustomMeshMaterial;

        private RenderParams _renderParams;
        Material             _drawMeshMaterial;
        
        
        public override void Release()
        {
            KW_Extensions.SafeDestroy(_drawMeshMaterial, _drawCustomMeshMaterial);
            _drawCustomMeshMaterial = null;
            _drawMeshMaterial       = null;
        }


        public override void ExecuteBeforeCameraRendering(Camera cam, ScriptableRenderContext context)
        {
            if (cam == null) return;

            if (WaterSystem.ForceDisableWaterRendering) return;
            
#if KWS_HD_MODULE_INSTALLED
            DrawInstancedQuadTree(cam);
#endif

            DrawCustomMeshes(cam);
        }

        private void DrawCustomMeshes(Camera cam)
        {
            if (_drawCustomMeshMaterial == null) _drawCustomMeshMaterial = KWS_CoreUtils.CreateMaterial(KWS_ShaderConstants.ShaderNames.WaterCustomMeshShaderName, useWaterStencilMask: true);
            _drawCustomMeshMaterial.renderQueue = KWS_Settings.Water.DefaultWaterQueue + WaterSystem.QualitySettings.WaterTransparentSortingPriority;

            _customMeshRenderParams.camera               = cam;
            _customMeshRenderParams.material             = _drawCustomMeshMaterial;
            _customMeshRenderParams.reflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox;
       
            _customMeshRenderParams.renderingLayerMask   = 1;
            _customMeshRenderParams.layer                = KWS_Settings.Water.WaterLayer;

            var meshZones = KWS_TileZoneManager.VisibleCustomMeshZones;
            foreach (var zone in meshZones)
            {
                if (zone.OverrideMesh && zone.CustomMesh)
                {
                    _customMeshRenderParams.worldBounds          = zone.Bounds;
                    Graphics.RenderMesh(_customMeshRenderParams, zone.CustomMesh, 0, zone.CachedFittedMatrix);
                }
            }

        }


#if KWS_HD_MODULE_INSTALLED
        void DrawInstancedQuadTree(Camera cam)
        {
            if (_drawMeshMaterial == null)
            {
                _drawMeshMaterial = KWS_CoreUtils.CreateMaterial(KWS_ShaderConstants.ShaderNames.WaterShaderName, useWaterStencilMask: true);
            }

            _drawMeshMaterial.renderQueue = KWS_Settings.Water.DefaultWaterQueue + WaterSystem.QualitySettings.WaterTransparentSortingPriority;

            _renderParams.camera               = cam;
            _renderParams.material             = _drawMeshMaterial;
            _renderParams.reflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox;
            _renderParams.worldBounds          = WaterSystem.Instance.WorldSpaceBoundsWithZones;
            _renderParams.renderingLayerMask   = 1;
            _renderParams.layer                = KWS_Settings.Water.WaterLayer;

            var lodBias = 0;
            if (!OceanRenderPass._meshQuadTree.TryGetRenderingContext(cam, lodBias, out var context)) return;

         
            if (_renderParams.camera == null || _renderParams.material == null || context.chunkInstance == null || context.visibleChunksArgs == null || context.visibleChunksArgs.count == 0)
            {
                Debug.LogError($"Water draw mesh rendering error: {_renderParams.camera}, {_renderParams.material}, {context.chunkInstance}, {context.visibleChunksArgs}");
                return;
            }

            if (context.visibleChunksComputeBuffer != null)
            {
                _drawMeshMaterial.SetBuffer(KWS_ShaderConstants.StructuredBuffers.InstancedMeshData, context.visibleChunksComputeBuffer);
                Graphics.RenderMeshIndirect(_renderParams, context.chunkInstance, context.visibleChunksArgs);
            }
          
        }


#endif

    }
}