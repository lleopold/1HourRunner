using UnityEngine;
using UnityEngine.Rendering;

namespace KWS
{
    internal class DynamicWavesParticlesPass : WaterPass
    {
        internal override string   PassName => "Water.DynamicWavesParticlesPass";
        private           Material _copyColorMaterial;

        public DynamicWavesParticlesPass()
        {
            
        }

      
        public override void ExecuteCommandBuffer(WaterPassContext waterContext)
        { 
            if (WaterSystem.ForceDisableWaterRendering) return;
            
          
        }

        public override void Release()
        {
           
        }
       
    }
}