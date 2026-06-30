
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static KWS.KWS_DynamicWavesSimulationZone;

#if KWS_URP
using UnityEngine.Rendering.Universal;
#endif

#if KWS_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace KWS
{

    internal class WetnessPass : WaterPass
    {
        internal override string     PassName => "Water.WetnessPass";
        
        private           GameObject _wetDecalObject;
        #if KWS_URP || KWS_HDRP
        private DecalProjector _wetDecalProjector;
        #endif
        
        private Mesh     _cubeMesh;
        private Material _wetMaterial;
        private Material _wetMaterialZone;

        private const     float  BakedWetMapOffset = 0.2f;
       

        public override void Release()
        {
           
#if !KWS_HDRP && !KWS_URP
            KW_Extensions.SafeDestroy(_wetMaterial, _wetMaterialZone);
            _wetMaterial     = null;
            _wetMaterialZone = null;

#endif
            
#if KWS_HDRP || KWS_URP
 
            //removing hdrp decals cause native editor bug
            
            KW_Extensions.SafeDestroy(_wetDecalObject);
            _wetDecalObject = null;
            
#endif

            this.WaterLog(string.Empty, KW_Extensions.WaterLogMessageType.Release);
        }

        public override void ExecuteBeforeCameraRendering(Camera cam, ScriptableRenderContext context)
        {
            var useWetEffect = WaterQualityLevelSettings.ResolveQualityOverride(WaterSystem.Instance.WetEffect, WaterSystem.QualitySettings.UseWetEffect);
            if (useWetEffect) UpdateWetDecal(cam);

        }

        public override void ExecuteCommandBuffer(WaterPassContext waterContext)
        {
            ExecuteWetMap(waterContext);
        }
        

        private static void UpdateWetDecalPositon(Camera cam, out Vector3 decalSize, out Vector3 decalPos)
        {
            var waterInstance = WaterSystem.Instance;
            var farDistance   = cam.farClipPlane * 0.5f;

            var bounds                                     = waterInstance.WorldSpaceBoundsWithZones;
            bounds.Expand(new Vector3(0, waterInstance.WetnessHeightAboveWater * 2, 0));
            decalSize  = bounds.size;
            decalPos   = cam.transform.position;
            decalPos.y = waterInstance.WaterLevel + Mathf.Max(waterInstance.WetnessHeightAboveWater, waterInstance.CurrentMaxHeightOffsetRelativeToWaterLevel);

            //farDistance = Mathf.Min(farDistance, WaterSystem.QualitySettings.MeshDetailingFarDistance * WaterSystem._globalWaterScaleFactor);
            decalSize.x = Mathf.Min(decalSize.x, farDistance);
            decalSize.y = Mathf.Min(decalSize.y, farDistance);
            decalSize.z = Mathf.Min(decalSize.z, farDistance);
            
#if KWS_BUILTIN
            decalPos.y -= decalSize.y * 0.5f;
#endif
         
        }
        

        void UpdateWetDecal(Camera cam)
        {
#if KWS_URP || KWS_HDRP
            if (!_wetDecalProjector) return;
            
            UpdateWetDecalPositon(cam, out var decalSize, out var decalPos);
            var rotatedSize = new Vector3(decalSize.x, decalSize.z, decalSize.y);
            _wetDecalProjector.size               = rotatedSize;
            _wetDecalProjector.transform.position = decalPos;
            
            
#if UNITY_6000_0_OR_NEWER && (KWS_URP)
           _wetDecalProjector.renderingLayerMask = WaterSystem.Instance.WetDecalLayerMask;
#endif
#if UNITY_6000_0_OR_NEWER && (KWS_HDRP)
           _wetDecalProjector.decalLayerMask  = WaterSystem.Instance.WetDecalLayerMask;
#endif
            
#endif
        }

        
        private void ExecuteWetMap(WaterPassContext waterContext)
        {
            var useWetEffect = WaterQualityLevelSettings.ResolveQualityOverride(WaterSystem.Instance.WetEffect, WaterSystem.QualitySettings.UseWetEffect);
            
            if (!useWetEffect)
            {
                if (_wetDecalObject)
                {
                    if(_wetDecalObject != null) _wetDecalObject.SetActive(false);
                    KW_Extensions.SafeDestroy(_wetDecalObject);
                    _wetDecalObject = null;
                }

                return;
            }
            
            var cam           = waterContext.cam;
          

#if KWS_BUILTIN
            if (!_cubeMesh) _cubeMesh               = MeshUtils.CreateCubeMesh();
            if (!_wetMaterial) _wetMaterial         = KWS_CoreUtils.CreateMaterial("Hidden/KriptoFX/KWS/KWS_DynamicWavesWetDecal");
            if (!_wetMaterialZone) _wetMaterialZone = KWS_CoreUtils.CreateMaterial("Hidden/KriptoFX/KWS/KWS_DynamicWavesWetDecalZoneInstance");
            
            UpdateWetDecalPositon(cam, out var decalSize, out var decalPos);
            var decalTRS = Matrix4x4.TRS(decalPos, Quaternion.identity, decalSize);

            waterContext.cmd.SetRenderTarget(KWS_CoreUtils.GetMrt(BuiltinRenderTextureType.GBuffer0, BuiltinRenderTextureType.GBuffer1, waterContext.cameraColor), waterContext.cameraColor);
            waterContext.cmd.DrawMesh(_cubeMesh, decalTRS, _wetMaterial, 0, 0);

            foreach (var zone in KWS_TileZoneManager.VisibleBakedDynamicWavesZones)
            {
                if (zone.ZoneType == SimulationZoneTypeMode.BakedSimulation && zone.SavedMesh && zone.IsZoneInitialized)
                {
                    var pos = zone.Position;
                    pos.y = zone.bakedWaterLevel + BakedWetMapOffset;
                    var trs = Matrix4x4.TRS(pos, zone.Rotation, zone.Size);
                    DynamicWavesPass.UpdateSimulationShaderParams(_wetMaterialZone, zone);
                    waterContext.cmd.DrawMesh(zone.SavedMesh, trs, _wetMaterialZone, 0, 0);
                }
            }
#endif

#if KWS_URP || KWS_HDRP

            if (!_wetMaterial)
            {
               _wetMaterial = Resources.Load<Material>("PlatformSpecific/WetDecal");
            }
            
            if (!_wetDecalObject)
            {
                _wetDecalObject                    = new GameObject("KWS_WetnessDecal");
                #if KWS_DEBUG
                    _wetDecalObject.hideFlags          = HideFlags.DontSave;
                #else
                    _wetDecalObject.hideFlags          = HideFlags.HideAndDontSave;
                #endif
                //_wetDecalObject.transform.SetParent(WaterSystem.UpdateManagerObject.transform);
                _wetDecalObject.transform.rotation = Quaternion.Euler(90, 0, 0);
                _wetDecalProjector                 = _wetDecalObject.AddComponent<DecalProjector>();
                _wetDecalProjector.material        = _wetMaterial;
            }
#endif
        }

     
        
    }
}