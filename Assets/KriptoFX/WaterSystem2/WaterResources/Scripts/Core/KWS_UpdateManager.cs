
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KWS
{
    [AddComponentMenu("")]
    [ExecuteAlways]
    internal partial class KWS_UpdateManager : MonoBehaviour
    {
        private KWS_WaterPassHandler _passHandler;
        
        
        internal static uint GlobalFrame;
        internal static uint Global30FpsFrame;
        internal static uint Global45FpsFrame;
        internal static uint Global60FpsFrame;
        internal static float EditorRefreshScale = 1;

        internal static bool IsHasFocus = true;

        private float _fixedUpdateLeftTime_60fps = 0;
        private float _fixedUpdateLeftTime_45fps = 0;
        private float _fixedUpdateLeftTime_30fps = 0;
        
        private float _timeScaledFixedUpdateLeftTime_60fps = 0;


        const int maxAllowedFrames = 2;
        const float fixedUpdate_60fps = 1f / 60f;
        const float fixedUpdate_45fps = 1f / 45f;
        const float fixedUpdate_30fps = 1f / 30f;

        private CustomFixedUpdates _customFixedUpdates = new CustomFixedUpdates();

        internal static HashSet<Camera> LastFrameRenderedCameras = new HashSet<Camera>();

    
        private KWS_TileZoneManager _zoneManager = new KWS_TileZoneManager();

        public static Dictionary<Camera, CameraFrustumCache> FrustumCaches = new Dictionary<Camera, CameraFrustumCache>();
        internal class CameraFrustumCache
        {
            public Plane[]   FrustumPlanes  = new Plane[6];
            public Vector3[] FrustumCorners = new Vector3[8];

            public void Update(Camera cam)
            {
                GeometryUtility.CalculateFrustumPlanes(cam, FrustumPlanes);
                KW_Extensions.CalculateFrustumCorners(ref FrustumCorners, cam);
            }
        }


        void OnEnable()
        {
#if !KWS_URP && !KWS_HDRP
            Camera.onPreCull += OnBeforeCameraRendering;
            Camera.onPostRender += OnAfterCameraRendering;
#else
            RenderPipelineManager.beginCameraRendering += OnBeforeCameraRendering;
            RenderPipelineManager.endCameraRendering   += OnAfterCameraRendering;
#endif

            if (_passHandler == null) _passHandler = new KWS_WaterPassHandler();

#if UNITY_EDITOR
            EditorApplication.hierarchyChanged         += OnHierarchyChanged;
            EditorApplication.update                   += EditorUpdate;
            EditorApplication.focusChanged += EditorApplicationOnfocusChanged;
#endif

         
            _zoneManager.OnEnable();
            if(Application.isPlaying && Camera.main) LastFrameRenderedCameras.Add(Camera.main);
            
            

        }


        void OnDisable()
        {
#if !KWS_URP && !KWS_HDRP

            Camera.onPreCull -= OnBeforeCameraRendering;
            Camera.onPostRender -= OnAfterCameraRendering;

#else
            RenderPipelineManager.beginCameraRendering -= OnBeforeCameraRendering;
            RenderPipelineManager.endCameraRendering   -= OnAfterCameraRendering;
#endif
            LastFrameRenderedCameras.Clear();

            _passHandler?.Release();
            _passHandler = null;
            KWS_CoreUtils.ReleaseRTHandles();

#if UNITY_EDITOR
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.update           -= EditorUpdate;
            EditorApplication.focusChanged     -= EditorApplicationOnfocusChanged;
#endif

            _zoneManager.OnDisable();
            
            GlobalFrame      = 0;
            Global30FpsFrame = 0;
            Global45FpsFrame = 0;
            Global60FpsFrame = 0;
        }


#if UNITY_EDITOR
        private void OnHierarchyChanged()
        {
            WaterSharedResources.UpdateReflectionProbeCache();
        }

        private void EditorUpdate()
        {
            EditorRefreshScale = 1;
            if (Application.isPlaying) return;

            EditorRefreshScale = (KW_Extensions.IsAlwaysRefreshEnabled() || KWS_DynamicWavesSimulationZone.IsAnyZoneInBakeMode) ? 1 : 0.01f;
           
            ExecutePerFrame();
        }
        
        private void EditorApplicationOnfocusChanged(bool hasFocus)
        {
            IsHasFocus = hasFocus;
        }
#endif

        private void LateUpdate()
        {
            if (!Application.isPlaying) return;

            ExecutePerFrame();
        }



#if !KWS_URP && !KWS_HDRP
        private void OnBeforeCameraRendering(Camera cam)
        {
            ExecutePerCamera(cam, default);
        }

        private void OnAfterCameraRendering(Camera cam)
        {
            if (_passHandler != null) _passHandler.OnAfterCameraRendering(cam);
        }

#else
        private void OnBeforeCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            ExecutePerCamera(cam, context);
        }

        private void OnAfterCameraRendering(ScriptableRenderContext context, Camera cam)
        {

            if (_passHandler != null) _passHandler.OnAfterCameraRendering(cam, context);
        }
#endif



        private void ExecutePerFrame()
        {
            if (!IsHasFocus) return;
            if (_passHandler == null) return;

            KW_Extensions.UpdateEditorDeltaTime();
            KWS_WaterSettingsRuntimeLoader.LoadWaterSettings();


            if (WaterSystem.QualitySettings == null)
            {
                Debug.LogError("WaterQualitySettings.asset not found in Resources.");
                return;
            }

#if ENABLE_VR_MODULE && ENABLE_VR && ENABLE_XR_MODULE
            KWS_CoreUtils.SinglePassStereoEnabled = KWS_CoreUtils.IsSinglePassStereoActive();
#endif
            if (!KWS_CoreUtils.CanRenderAnyWater()) return;
            

#if !KWS_HDRP && !KWS_URP
            Shader.SetGlobalInteger("KWS_Pipeline", 0);
#elif KWS_URP
            Shader.SetGlobalInteger("KWS_Pipeline", 1);
#elif KWS_HDRP
            Shader.SetGlobalInteger("KWS_Pipeline", 2);
#endif

            UpdateCustomFixedUpdates();

            LastFrameRenderedCameras.RemoveWhere(item => item == null);

            WaterSystem.Instance.ExecutePerFrame(LastFrameRenderedCameras, _customFixedUpdates);
            _zoneManager.ExecutePerFrame(LastFrameRenderedCameras);
           
            WaterSystem.RequestUnderwaterState(LastFrameRenderedCameras);
            _passHandler.OnBeforeFrameRendering(LastFrameRenderedCameras, _customFixedUpdates);
           
            
            LastFrameRenderedCameras.Clear();

            if (_customFixedUpdates.FramesCount_30fps != 0) Global30FpsFrame++;
            if (_customFixedUpdates.FramesCount_45fps != 0) Global45FpsFrame++;
            if (_customFixedUpdates.FramesCount_60fps != 0) Global60FpsFrame++;
            GlobalFrame++;
        }


        void ExecutePerCamera(Camera cam, ScriptableRenderContext context)
        {
            if (WaterSystem.QualitySettings == null) return;
            if (_passHandler                == null) return;
           

            if (!KWS_CoreUtils.CanRenderAnyWater()) return;
            if (!KWS_CoreUtils.CanRenderCurrentCamera(cam)) return;

            LastFrameRenderedCameras.Add(cam);
            CacheCameraData(cam);
            
            WaterSystem.Instance.ExecutePerCamera(cam);

            _passHandler.OnBeforeCameraRendering(cam, context);
        }

        private static void CacheCameraData(Camera cam)
        {
            if (FrustumCaches.Count > 10) FrustumCaches.Clear();
            if (!FrustumCaches.ContainsKey(cam)) FrustumCaches.Add(cam, new CameraFrustumCache());
            FrustumCaches[cam].Update(cam);
        }


        public void UpdateCustomFixedUpdates()
        {
            var deltaTime         = KW_Extensions.DeltaTime() * WaterSystem.GlobalTimeScale;
            var unscaledDeltaTime = KW_Extensions.DeltaTime();
            
            _fixedUpdateLeftTime_60fps           += deltaTime;
            _fixedUpdateLeftTime_45fps           += deltaTime;
            _fixedUpdateLeftTime_30fps           += deltaTime;
            _timeScaledFixedUpdateLeftTime_60fps += unscaledDeltaTime;
           
            UpdateFixedFrames(ref _fixedUpdateLeftTime_60fps,           out _customFixedUpdates.FramesCount_60fps, fixedUpdate_60fps);
            UpdateFixedFrames(ref _fixedUpdateLeftTime_45fps,           out _customFixedUpdates.FramesCount_45fps, fixedUpdate_45fps);
            UpdateFixedFrames(ref _fixedUpdateLeftTime_30fps,           out _customFixedUpdates.FramesCount_30fps, fixedUpdate_30fps);
            UpdateFixedFrames(ref _timeScaledFixedUpdateLeftTime_60fps, out _customFixedUpdates.TimeScaledFramesCount_60fps, fixedUpdate_60fps);

#if UNITY_EDITOR
            if (KWS_CoreUtils.IsFrameDebuggerEnabled() || (EditorApplication.isPlaying && EditorApplication.isPaused))
            { 
                _customFixedUpdates.FramesCount_60fps = 1;
                _customFixedUpdates.FramesCount_45fps = 1;
                _customFixedUpdates.FramesCount_30fps = 1;
            }

#endif
        }

        private void UpdateFixedFrames(ref float leftTimeVal, out int framesVal, float deltaTime)
        {
            int frames = 0;
            while (leftTimeVal > 0f)
            {
                leftTimeVal -= deltaTime;
                frames++;
                if (frames > maxAllowedFrames)
                {
                    leftTimeVal = 0;
                    framesVal = frames;
                    return;
                }
            }

            framesVal = frames;
        }

   

    }

 

    internal class CustomFixedUpdates
    {
        public int TimeScaledFramesCount_60fps;
        public int FramesCount_60fps;
        public int FramesCount_45fps;
        public int FramesCount_30fps;
    }
}