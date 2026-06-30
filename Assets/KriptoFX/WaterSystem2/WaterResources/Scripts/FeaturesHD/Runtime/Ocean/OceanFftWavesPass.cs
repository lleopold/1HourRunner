//#define DEBUG_FFT

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;


namespace KWS
{
    internal class OceanFftWavesPass : WaterPass
    {
        internal override string PassName => "Water.FftWavesPass";

        int kernelSpectrumInit;
        int kernelSpectrumUpdate;
        int kernelNormal;
        int kernelClearTextureArray;

        Dictionary<int, Texture2D> _butterflyTextures         = new Dictionary<int, Texture2D>();
        private readonly int                        KwsDisplaceXYZ             = Shader.PropertyToID("_displaceXYZ");
        private readonly int                        KwsKwsNormalFoamTargetRW   = Shader.PropertyToID("KWS_NormalFoamTargetRW");
        private readonly int                        KwsKwsPrevNormalFoamTarget = Shader.PropertyToID("KWS_PrevNormalFoamTarget");

        static readonly int TargetID     = Shader.PropertyToID( "KWS_ClearTarget" );
        static readonly int TargetSizeID = Shader.PropertyToID( "KWS_ClearTargetSize" );
        static readonly int ClearValueID = Shader.PropertyToID( "KWS_ClearValue" );
        
        private WindZone _lastWindZone;
        private float _lastWindZoneSpeed;
        private float _lastWindZoneTurbulence;
        private Vector3 _lastWindZoneRotation;
        private CommandBuffer _cmd;

        private const float          DefaultTimeScale = 1.5f;
        private const GraphicsFormat fftFormat        = GraphicsFormat.R16G16B16A16_SFloat;
        private const GraphicsFormat spectrumFormat   = GraphicsFormat.R16G16_SFloat;
        private const GraphicsFormat normalFormat     = GraphicsFormat.R16G16B16A16_SFloat;


        RTHandle[] DisplaceTexture = new RTHandle[2];
        RTHandle[] NormalTextures  = new RTHandle[2];

        RTHandle spectrumInit;
        RTHandle spectrumDisplaceX;
        RTHandle spectrumDisplaceY;
        RTHandle spectrumDisplaceZ;

        RTHandle fftTemp1;
        RTHandle fftTemp2;
        RTHandle fftTemp3;

        ComputeShader spectrumShader;
        ComputeShader shaderFFT;

        bool RequireReinitializeSpectrum;

        int Frame;

        internal static OceanFftWavesPass _instance;

        bool RequireReinitialize(int size, int cascades)
        {
            if (DisplaceTexture[0] == null || DisplaceTexture[0].rt == null || shaderFFT == null) return true;

            var rt = DisplaceTexture[0].rt;
            if (rt.width != size || rt.volumeDepth != cascades) return true;

            return false;
        }

        void Initialize(int size, int cascades)
        {
       
            spectrumInit      = KWS_CoreUtils.RTHandles.Alloc(size, size, colorFormat: fftFormat,      enableRandomWrite: true, dimension: TextureDimension.Tex2DArray, slices: cascades);
            spectrumDisplaceY = KWS_CoreUtils.RTHandles.Alloc(size, size, colorFormat: spectrumFormat, enableRandomWrite: true, dimension: TextureDimension.Tex2DArray, slices: cascades);
            spectrumDisplaceX = KWS_CoreUtils.RTHandles.Alloc(size, size, colorFormat: spectrumFormat, enableRandomWrite: true, dimension: TextureDimension.Tex2DArray, slices: cascades);
            spectrumDisplaceZ = KWS_CoreUtils.RTHandles.Alloc(size, size, colorFormat: spectrumFormat, enableRandomWrite: true, dimension: TextureDimension.Tex2DArray, slices: cascades);

            fftTemp1 = KWS_CoreUtils.RTHandles.Alloc(size, size, colorFormat: spectrumFormat, enableRandomWrite: true, dimension: TextureDimension.Tex2DArray, slices: cascades);
            fftTemp2 = KWS_CoreUtils.RTHandles.Alloc(size, size, colorFormat: spectrumFormat, enableRandomWrite: true, dimension: TextureDimension.Tex2DArray, slices: cascades);
            fftTemp3 = KWS_CoreUtils.RTHandles.Alloc(size, size, colorFormat: spectrumFormat, enableRandomWrite: true, dimension: TextureDimension.Tex2DArray, slices: cascades);

            DisplaceTexture[0] = KWS_CoreUtils.RTHandles.Alloc(size, size, name: "KWS_FftWavesDisplacement0", colorFormat: fftFormat, enableRandomWrite: true, dimension: TextureDimension.Tex2DArray, slices: cascades);
            DisplaceTexture[1] = KWS_CoreUtils.RTHandles.Alloc(size, size, name: "KWS_FftWavesDisplacement1", colorFormat: fftFormat, enableRandomWrite: true, dimension: TextureDimension.Tex2DArray, slices: cascades);

            NormalTextures[0] = KWS_CoreUtils.RTHandles.Alloc(size, size, name: "KWS_FftWavesNormal1", colorFormat: normalFormat, enableRandomWrite: true,
                                                           autoGenerateMips: false, useMipMap: true, dimension: TextureDimension.Tex2DArray, slices: cascades, filterMode: FilterMode.Trilinear);
            NormalTextures[1] = KWS_CoreUtils.RTHandles.Alloc(size, size, name: "KWS_FftWavesNormal2", colorFormat: normalFormat, enableRandomWrite: true,
                                                           autoGenerateMips: false, useMipMap: true, dimension: TextureDimension.Tex2DArray, slices: cascades, filterMode: FilterMode.Trilinear);


            GetOrCreateButterflyTexture(size);
            //this.WaterLog(DisplaceTexture[0], NormalTextures[0]);
        }

        void InitializeShaders(int size, int cascades)
        {

            if (spectrumShader == null) spectrumShader = KWS_CoreUtils.LoadComputeShader("Common/CommandPass/KWS_WavesSpectrum");
            if (shaderFFT == null) shaderFFT = KWS_CoreUtils.LoadComputeShader("Common/CommandPass/KWS_WavesFFT");


            if (spectrumShader != null)
            {
                spectrumShader.name = "WavesSpectrum";
                kernelSpectrumInit = spectrumShader.FindKernel("SpectrumInitalize");
                kernelSpectrumUpdate = spectrumShader.FindKernel("SpectrumUpdate");

                spectrumShader.SetTexture(kernelSpectrumUpdate, "SpectrumInit", spectrumInit);
                spectrumShader.SetTexture(kernelSpectrumUpdate, "SpectrumDisplaceX", spectrumDisplaceX);
                spectrumShader.SetTexture(kernelSpectrumUpdate, "SpectrumDisplaceY", spectrumDisplaceY);
                spectrumShader.SetTexture(kernelSpectrumUpdate, "SpectrumDisplaceZ", spectrumDisplaceZ);
            }

            if (shaderFFT != null)
            {
                shaderFFT.name          = "WavesFFT";
                kernelNormal            = shaderFFT.FindKernel("ComputeNormal");
                kernelClearTextureArray = shaderFFT.FindKernel("ClearTextureArray");

                var fftKernel = GetKernelBySize(size);

                shaderFFT.SetTexture(fftKernel, "SpectrumDisplaceX", spectrumDisplaceX);
                shaderFFT.SetTexture(fftKernel, "SpectrumDisplaceY", spectrumDisplaceY);
                shaderFFT.SetTexture(fftKernel, "SpectrumDisplaceZ", spectrumDisplaceZ);
                shaderFFT.SetTexture(fftKernel, "inputButterfly", GetOrCreateButterflyTexture(size));
                shaderFFT.SetTexture(fftKernel, "_displaceX", fftTemp1);
                shaderFFT.SetTexture(fftKernel, "_displaceY", fftTemp2);
                shaderFFT.SetTexture(fftKernel, "_displaceZ", fftTemp3);

                shaderFFT.SetTexture(fftKernel + 1, "SpectrumDisplaceX", fftTemp1);
                shaderFFT.SetTexture(fftKernel + 1, "SpectrumDisplaceY", fftTemp2);
                shaderFFT.SetTexture(fftKernel + 1, "SpectrumDisplaceZ", fftTemp3);
                shaderFFT.SetTexture(fftKernel + 1, "inputButterfly", GetOrCreateButterflyTexture(size));
                //shaderFFT.SetTexture(fftKernel + 1, "_displaceXYZ",      DisplaceTexture);

                shaderFFT.SetVector("KWS_FFT_TexelSize", new Vector4(1f / NormalTextures[0].rt.width, 1f / NormalTextures[0].rt.height, NormalTextures[0].rt.width, NormalTextures[0].rt.height));
                //shaderFFT.SetTexture(kernelNormal, "_displaceXYZ", DisplaceTexture);
            }
        }

        RTHandle GetTargetNormal()
        {
            return NormalTextures[Frame];
        }

        RTHandle GetPreviousTargetNormal()
        {
            return NormalTextures[(Frame + 1) % 2];
        }

        RTHandle GetDisplacement()
        {
            return DisplaceTexture[Frame];
        }

        RTHandle GetPreviousDisplacement()
        {
            return DisplaceTexture[(Frame + 1) % 2];
        }

        void SwapPingPong()
        {
            Frame = (Frame + 1) % 2;
        }


        void ReleaseTextures()
        {
            spectrumInit?.Release();
            spectrumDisplaceY?.Release();
            spectrumDisplaceX?.Release();
            spectrumDisplaceZ?.Release();

            fftTemp1?.Release();
            fftTemp2?.Release();
            fftTemp3?.Release();

            DisplaceTexture[0]?.Release();
            DisplaceTexture[1]?.Release();

            NormalTextures[0]?.Release();
            NormalTextures[1]?.Release();

            DisplaceTexture[0] = DisplaceTexture[1] = NormalTextures[0] = NormalTextures[1] = null;
            
            spectrumInit = spectrumDisplaceX = spectrumDisplaceY = spectrumDisplaceZ = spectrumDisplaceZ = null;
            
            fftTemp1 = fftTemp2 = fftTemp3 = null;

           // this.WaterLog(String.Empty, KW_Extensions.WaterLogMessageType.ReleaseRT);
        }


        public OceanFftWavesPass()
        {
            WaterSystem.OnAnyWaterSettingsChanged += OnAnyWaterSettingsChanged;
            _instance                             =  this;
        }


        public override void Release()
        {
            WaterSystem.OnAnyWaterSettingsChanged -= OnAnyWaterSettingsChanged;

            ReleaseFFT();
            
            this.WaterLog(String.Empty, KW_Extensions.WaterLogMessageType.ReleaseRT);
            this.WaterLog(String.Empty, KW_Extensions.WaterLogMessageType.Release);
        }

        internal void ReleaseFFT()
        {
            foreach (var butterflyTexture in _butterflyTextures) KW_Extensions.SafeDestroy(butterflyTexture.Value);
            _butterflyTextures.Clear();

            ReleaseTextures();
            KW_Extensions.SafeDestroy(spectrumShader, shaderFFT);
            spectrumShader              = null;
            shaderFFT                   = null;
            RequireReinitializeSpectrum = true;

        }

        private void OnAnyWaterSettingsChanged(WaterSystem.WaterSettingsCategory changedTabs)
        {   
            if (KWS_Ocean.Instance == false) return;
            
            if (!changedTabs.HasTab(WaterSystem.WaterSettingsCategory.Ocean)) return;
            
            var size     = (int)KWS_Ocean.Instance.FftWavesQuality;
            var cascades = KWS_Ocean.Instance.FftWavesCascades;

            InitializeFftWavesData(size, cascades);
        }

        void InitializeFftWavesData(int size, int cascades)
        {  
            if (RequireReinitialize(size, cascades))
            {
                ReleaseTextures();
                Initialize(size, cascades);
                InitializeShaders(size, cascades);
                ClearFftTargets(size, cascades);

            }

            RequireReinitializeSpectrum = true;
        }
        
        void ClearFftTargets(int size, int cascades)
        {
            if(shaderFFT == null ) return;

            var cmd = new CommandBuffer();
            cmd.name = "Clear Tex2DArray";
           
            // Displacement: float4(0,0,0,0)
            ClearArray(cmd, DisplaceTexture[0], size, cascades, Vector4.zero );
            ClearArray(cmd, DisplaceTexture[1], size, cascades,   Vector4.zero );

            // Normal: float4(0,1,0,1) in SFloat format is safe (will normalize to up)
            ClearArray(cmd, NormalTextures[0], size, cascades, new Vector4( 0, 1, 0, 1 ) );
            ClearArray(cmd, NormalTextures[1], size, cascades, new Vector4( 0, 1, 0, 1 ) );
           
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();
        }

        void ClearArray(CommandBuffer cmd, RTHandle target, int size, int slices, Vector4 value )
        {
            if( target == null || target.rt == null )
                return;

            cmd.SetComputeTextureParam(shaderFFT, kernelClearTextureArray, TargetID, target );
            cmd.SetComputeVectorParam( shaderFFT, ClearValueID, value );
            cmd.SetComputeVectorParam( shaderFFT, TargetSizeID, new Vector4( size, size, slices, 0 ) );

            // dispatch x,y over pixels, z over slices
            int gx = Mathf.CeilToInt( size / 8.0f );
            int gy = Mathf.CeilToInt( size / 8.0f );
            cmd.DispatchCompute( shaderFFT, kernelClearTextureArray, gx, gy, slices );
        }

        
        public override void ExecutePerFrame(HashSet<Camera> cameras, CustomFixedUpdates fixedUpdates)
        {
            #if DEBUG_FFT
                return;
            #endif
            
            if (KWS_Ocean.Instance == false)
            {
                Shader.SetGlobalTexture(KWS_ShaderConstants.FFT.KWS_FftWavesDisplace, KWS_CoreUtils.DefaultBlackTexture);
                Shader.SetGlobalTexture(KWS_ShaderConstants.FFT.KWS_FftWavesNormal,   KWS_CoreUtils.DefaultBlackTexture);
                
                return;
            }
            
            if (WaterSystem.UseNetworkBuoyancy == false && fixedUpdates.FramesCount_60fps == 0) return;

            if (_cmd == null) _cmd = new CommandBuffer() { name = PassName };
            _cmd.Clear();
            
            if (KWS_Ocean.Instance.WindZone != null && IsWindZoneChanged()) RequireReinitializeSpectrum = true;

            ExecuteInstance(_cmd);
            WaterSharedResources.FftWavesDisplacement     = GetDisplacement();
            WaterSharedResources.FftWavesDisplacementPrev = GetPreviousDisplacement();
            WaterSharedResources.FftWavesNormal           = GetTargetNormal();

            Shader.SetGlobalTexture(KWS_ShaderConstants.FFT.KWS_FftWavesDisplace,     WaterSharedResources.FftWavesDisplacement);
            Shader.SetGlobalTexture(KWS_ShaderConstants.FFT.KWS_FftWavesDisplacePrev, WaterSharedResources.FftWavesDisplacementPrev);
            Shader.SetGlobalTexture(KWS_ShaderConstants.FFT.KWS_FftWavesNormal,       WaterSharedResources.FftWavesNormal);
        
            Graphics.ExecuteCommandBuffer(_cmd);
           
        }

        public override void ExecuteBeforeCameraRendering(Camera cam, ScriptableRenderContext context)
        {
            #if DEBUG_FFT
                if (KWS_Ocean.Instance == false)
                {
                    Shader.SetGlobalTexture(KWS_ShaderConstants.FFT.KWS_FftWavesDisplace, KWS_CoreUtils.DefaultBlackTexture);
                    Shader.SetGlobalTexture(KWS_ShaderConstants.FFT.KWS_FftWavesNormal,   KWS_CoreUtils.DefaultBlackTexture);
                    
                    return;
                }
              
                if (_cmd == null) _cmd = new CommandBuffer() { name = PassName };
                _cmd.Clear();
                
                RequireReinitializeSpectrum = true;

                ExecuteInstance(_cmd);
                WaterSharedResources.FftWavesDisplacement = GetDisplacement();
                WaterSharedResources.FftWavesNormal       = GetTargetNormal();

                Shader.SetGlobalTexture(KWS_ShaderConstants.FFT.KWS_FftWavesDisplace, WaterSharedResources.FftWavesDisplacement);
                Shader.SetGlobalTexture(KWS_ShaderConstants.FFT.KWS_FftWavesNormal,   WaterSharedResources.FftWavesNormal);
            
                Graphics.ExecuteCommandBuffer(_cmd);
            #endif

        }

 

        void ExecuteInstance(CommandBuffer cmd)
        {
            var size     = (int)KWS_Ocean.Instance.FftWavesQuality;
            var cascades = KWS_Ocean.Instance.FftWavesCascades;
            
            if (RequireReinitialize(size, cascades))
            {  
                InitializeFftWavesData(size, cascades);
                return; //todo one frame delay to avoid nan init. Why? 
            }

            cmd.SetGlobalFloat(KWS_ShaderConstants.ConstantWaterParams.KWS_WavesAreaScale, KWS_Ocean.Instance.WavesAreaScale);
            var time = KW_Extensions.TotalTime() * KWS_Ocean.Instance.TimeScale * DefaultTimeScale;
            
            if (RequireReinitializeSpectrum) InitializeSpectrum(cmd, size, cascades);
            UpdateSpectrum(cmd, size, cascades, time);
            DispatchFFT(cmd, size, cascades);
        }


        void InitializeSpectrum(CommandBuffer cmd, int size, int cascades)
        {
            cmd.SetComputeFloatParam(spectrumShader, "KWS_WindSpeed",    KWS_Ocean.Instance.WindSpeed);
            cmd.SetComputeFloatParam(spectrumShader, "KWS_Turbulence",   KWS_Ocean.Instance.WindTurbulence);
            cmd.SetComputeFloatParam(spectrumShader, "KWS_WindRotation", KWS_Ocean.Instance.WindRotation);

            cmd.SetComputeIntParam(spectrumShader, "KWS_Size", size);

            //cmd.SetComputeFloatParams(spectrumShader, KWS_ShaderConstants.ConstantWaterParams.KWS_WavesDomainSizes, KWS_Settings.FFT.FftDomainSize);
            cmd.SetComputeFloatParam(spectrumShader, KWS_ShaderConstants.ConstantWaterParams.KWS_WavesAreaScale, KWS_Ocean.Instance.WavesAreaScale);

            cmd.SetComputeTextureParam(spectrumShader, kernelSpectrumInit, "RW_SpectrumInit", spectrumInit);

            cmd.DispatchCompute(spectrumShader, kernelSpectrumInit, size / 8, size / 8, cascades);
            RequireReinitializeSpectrum = false;

            //this.WaterLog($"InitializeSpectrum");
        }

        void UpdateSpectrum(CommandBuffer cmd, int size, int cascades, float time)
        {
           
            if (spectrumShader == null)
            {
                Debug.LogError($"Water UpdateSpectrum error: {spectrumShader}");
                return;
            }
            cmd.SetComputeFloatParam(spectrumShader, "time", time);
            cmd.DispatchCompute(spectrumShader, kernelSpectrumUpdate, size / 8, size / 8, cascades);
        }

        public void BakeFFT(int frames, float loopTime, int size, int cascades, int cascadeIndexToBake, out RenderTexture fftData, out RenderTexture normalsData)
        {   
            var cmd                      = new CommandBuffer();
            
            ReleaseFFT();
            InitializeFftWavesData(size, cascades);
            InitializeSpectrum(cmd, size, cascades);
            Frame = 0;
            
            
            var kernelInit               = spectrumShader.FindKernel("SpectrumBakeInit");
            var kernelSpectrumBakeRotate = spectrumShader.FindKernel("SpectrumBakeRotate");

            
            fftData = new RenderTexture(size, size, 0) { dimension = TextureDimension.Tex2DArray, volumeDepth = frames, graphicsFormat = fftFormat, useMipMap = false };
            fftData.Create();

            normalsData             = new RenderTexture(size, size, 0) { dimension = TextureDimension.Tex2DArray, volumeDepth = frames, graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm, useMipMap = true, autoGenerateMips = false};
            normalsData.Create();

            var spectrumStateRT = new RenderTexture(size, size, 0, GraphicsFormat.R16G16B16A16_SFloat) { enableRandomWrite = true, dimension = TextureDimension.Tex2DArray, volumeDepth = cascades, useMipMap = false };
            spectrumStateRT.Create();
            
            UpdateSpectrum(cmd, size, cascades, 0.0f);
            
            cmd.SetKeyword("FFT_BAKE_MODE", true);
             
          
            cmd.SetComputeFloatParam(spectrumShader, "KWS_BakeLoopTime", loopTime);
            cmd.SetComputeIntParam(spectrumShader, "KWS_Size", size);
            
            
            // Init state: SpectrumState = SpectrumInit
            cmd.SetComputeTextureParam(spectrumShader, kernelInit, "SpectrumState", spectrumStateRT);
            cmd.SetComputeTextureParam(spectrumShader, kernelInit, "SpectrumInit",  spectrumInit);
            cmd.DispatchCompute(spectrumShader, kernelInit, size/8, size/8, cascades);
            
            cmd.SetComputeTextureParam(spectrumShader, kernelSpectrumBakeRotate, "SpectrumState",     spectrumStateRT);
            cmd.SetComputeTextureParam(spectrumShader, kernelSpectrumBakeRotate, "SpectrumDisplaceX", spectrumDisplaceX);
            cmd.SetComputeTextureParam(spectrumShader, kernelSpectrumBakeRotate, "SpectrumDisplaceY", spectrumDisplaceY);
            cmd.SetComputeTextureParam(spectrumShader, kernelSpectrumBakeRotate, "SpectrumDisplaceZ", spectrumDisplaceZ);

            cmd.SetComputeFloatParam(spectrumShader, "KWS_Dt", 0.0f);
            cmd.DispatchCompute(spectrumShader, kernelSpectrumBakeRotate, size/8, size/8, cascades);
            DispatchFFT(cmd, size, cascades);
          
            cmd.Blit(GetPreviousDisplacement(), fftData,     cascadeIndexToBake, 0);
            cmd.Blit(GetPreviousTargetNormal(), normalsData, cascadeIndexToBake, 0);
            
            float dt = (loopTime / frames);
            cmd.SetComputeFloatParam(spectrumShader, "KWS_Dt", dt);

            
            for (int i = 1; i < frames; i++)
            {
                cmd.DispatchCompute(spectrumShader, kernelSpectrumBakeRotate, size / 8, size / 8, cascades);

                DispatchFFT(cmd, size, cascades);

                cmd.Blit(GetPreviousDisplacement(), fftData, cascadeIndexToBake, i);
                cmd.Blit(GetPreviousTargetNormal(), normalsData, cascadeIndexToBake, i);
                
            }
            
            cmd.GenerateMips(normalsData);
            cmd.SetKeyword("FFT_BAKE_MODE", false);
            Graphics.ExecuteCommandBuffer(cmd);
          
            spectrumStateRT.Release();
            
        }

        void DispatchFFT(CommandBuffer cmd, int size, int cascades)
        {
           // var instance  = WaterSystem.Instance;
            
            var fftKernel = GetKernelBySize(size);

            if (shaderFFT == null)
            {
                Debug.LogError($"Water DispatchFFT error: {shaderFFT}");
                return;
            }

            cmd.SetComputeTextureParam(shaderFFT, fftKernel + 1, KwsDisplaceXYZ, GetDisplacement());
            cmd.DispatchCompute(shaderFFT, fftKernel,     1,    size, cascades);
            cmd.DispatchCompute(shaderFFT, fftKernel + 1, size, 1,    cascades);

            cmd.SetComputeTextureParam(shaderFFT, kernelNormal, KwsDisplaceXYZ, GetDisplacement());
            cmd.SetComputeTextureParam(shaderFFT, kernelNormal, KwsKwsNormalFoamTargetRW, GetTargetNormal());
            cmd.SetComputeTextureParam(shaderFFT, kernelNormal, KwsKwsPrevNormalFoamTarget, GetPreviousTargetNormal());

            cmd.DispatchCompute(shaderFFT, kernelNormal, size / 8, size / 8, cascades);
            cmd.GenerateMips(GetTargetNormal());

            SwapPingPong();
        }


        Texture2D GetOrCreateButterflyTexture(int size)
        {
            if (!_butterflyTextures.ContainsKey(size)) _butterflyTextures.Add(size, InitializeButterfly(size));

            return _butterflyTextures[size];
        }

        Texture2D InitializeButterfly(int size)
        {
            var log2Size = Mathf.RoundToInt(Mathf.Log(size, 2));
            var butterflyColors = new Color[size * log2Size];

            int offset = 1, numIterations = size >> 1;
            for (int rowIndex = 0; rowIndex < log2Size; rowIndex++)
            {
                int rowOffset = rowIndex * size;
                {
                    int start = 0, end = 2 * offset;
                    for (int iteration = 0; iteration < numIterations; iteration++)
                    {
                        var bigK = 0.0f;
                        for (int K = start; K < end; K += 2)
                        {
                            var phase = 2.0f * Mathf.PI * bigK * numIterations / size;
                            var cos = Mathf.Cos(phase);
                            var sin = Mathf.Sin(phase);
                            butterflyColors[rowOffset + K / 2] = new Color(cos, -sin, 0, 1);
                            butterflyColors[rowOffset + K / 2 + offset] = new Color(-cos, sin, 0, 1);

                            bigK += 1.0f;
                        }
                        start += 4 * offset;
                        end = start + 2 * offset;
                    }
                }
                numIterations >>= 1;
                offset <<= 1;
            }
            var texButterfly = new Texture2D(size, log2Size, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None);
            texButterfly.SetPixels(butterflyColors);
            texButterfly.Apply();
            return texButterfly;
        }


        bool IsWindZoneChanged()
        {
            var windZone = KWS_Ocean.Instance.WindZone;
            if (KWS_Ocean.Instance.WindZone != _lastWindZone)
            {
                _lastWindZone = KWS_Ocean.Instance.WindZone;
                return true;
            }

            if (Math.Abs(_lastWindZoneSpeed - windZone.windMain * KWS_Ocean.Instance.WindZoneSpeedMultiplier) > 0.001f)
            {
                _lastWindZoneSpeed = windZone.windMain * KWS_Ocean.Instance.WindZoneSpeedMultiplier;
                return true;
            }

            if (Math.Abs(_lastWindZoneTurbulence - windZone.windTurbulence * KWS_Ocean.Instance.WindZoneTurbulenceMultiplier) > 0.001f)
            {
                _lastWindZoneTurbulence = windZone.windTurbulence * KWS_Ocean.Instance.WindZoneTurbulenceMultiplier;
                return true;
            }

            var forward = windZone.transform.forward;
            if (Math.Abs(_lastWindZoneRotation.x - forward.x) > 0.001f || Math.Abs(_lastWindZoneRotation.z - forward.z) > 0.001f)
            {
                _lastWindZoneRotation = forward;
                return true;
            }

            return false;
        }



        static int GetKernelBySize(int size)
        {
            var kernelOffset = 0;
            kernelOffset = size switch
            {
                (int)WaterQualityLevelSettings.FftWavesQualityEnum.Low    => 0,
                (int)WaterQualityLevelSettings.FftWavesQualityEnum.Medium => 2,
                (int)WaterQualityLevelSettings.FftWavesQualityEnum.High   => 4,
                (int)WaterQualityLevelSettings.FftWavesQualityEnum.Ultra  => 6,
                //(int)WaterQualityLevelSettings.FftWavesQualityEnum.Extreme  => 8,
                _                                                         => kernelOffset
            };
            return kernelOffset;
        }

    }
}