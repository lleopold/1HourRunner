using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace KWS
{
    public partial class WaterSystem
    {
        internal int  BakedFluidsSimPercentPassed;
        internal bool _isFluidsSimBakedMode;

        [Flags]
        public enum WaterSettingsCategory
        {
            ColorSettings       = 1 << 0,
            Ocean               = 1 << 1,
            Reflection          = 1 << 2,
            ColorRefraction     = 1 << 3,
            WetEffect           = 1 << 4,
            Foam                = 1 << 5,
            VolumetricLighting  = 1 << 6,
            Caustic             = 1 << 7,
            Underwater          = 1 << 8,
            Mesh                = 1 << 9,
            Rendering           = 1 << 10,
            Transform           = 1 << 11,
            LocalZone           = 1 << 12,
            SimulationZone      = 1 << 13,
            DynamicWaves          = 1 << 14,
            All                 = ~0
        }




#if UNITY_EDITOR
        
        public static void CreateOrFindWaterSystem(Vector3 pos, bool throwErrorIfExist)
        {
            var existing = FindFirstObjectByType<WaterSystem>();
            if (existing != null)
            {
                if (throwErrorIfExist)
                {
                    EditorUtility.DisplayDialog(
                        "Water Manager (Global Settings) already exists",
                        "This scene already has a 'Water Manager (Global Settings)' that controls global water settings. Only one is allowed per scene.",
                        "Ok"
                    );
                    return;
                }

                return;
            }

            var go = new GameObject("Water Settings");
            go.transform.position = pos;
            go.AddComponent<WaterSystem>();

            SceneView.lastActiveSceneView.LookAt(pos);
            go.layer = KWS_Settings.Water.WaterLayer;

            Undo.RegisterCreatedObjectUndo(go, "Create Water Settings");
            Selection.activeObject = go;
        }

        
        private static void CreateEditorDynamicWavesSimulationEffector(MenuCommand menuCommand, Vector3 pos)
        {
            var go = new GameObject("Dynamic Waves Simulation Effector");
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(1, 1, 1);

            SceneView.lastActiveSceneView.LookAt(go.transform.position);
            var source = go.AddComponent<KWS_DynamicWavesSimulationEffector>();

            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }

        

        internal static void CreateWaterSystemManager()
        {
            var pos = SceneView.lastActiveSceneView.camera.transform.TransformPoint(Vector3.forward * 3f);
            CreateOrFindWaterSystem(pos, true);
        }


        [MenuItem("GameObject/Effects/KWS Water/Dynamic Waves Simulation Zone")]
        private static void CreateDynamicWavesSimulationZoneEditor(MenuCommand menuCommand)
        {
            var go  = new GameObject("Dynamic Waves Simulation Zone");
            var pos = SceneView.lastActiveSceneView.camera.transform.TransformPoint(Vector3.forward * 3f);

            CreateOrFindWaterSystem(pos + Vector3.down * 25, false);

            go.transform.position   = pos;
            go.transform.localScale = new Vector3(100, 50, 100);

            SceneView.lastActiveSceneView.LookAt(go.transform.position);
            var simZone = go.AddComponent<KWS_DynamicWavesSimulationZone>();

            var source = FindFirstObjectByType<KWS_DynamicWavesSimulationEffector>();
            if (!source) CreateEditorDynamicWavesSimulationEffector(menuCommand, pos);

            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }

        [MenuItem("GameObject/Effects/KWS Water/Dynamic Waves Simulation Effector")]
        private static void CreateDynamicWavesSourceEditor(MenuCommand menuCommand)
        {
            var pos = SceneView.lastActiveSceneView.camera.transform.TransformPoint(Vector3.forward * 3f);
            CreateEditorDynamicWavesSimulationEffector(menuCommand, pos);
        }


        [MenuItem("GameObject/Effects/KWS Water/Local Water Zone")]
        private static void CreateLocalWaterZoneEditor(MenuCommand menuCommand)
        {
            var go            = new GameObject("Local Water Zone");
            var pos           = SceneView.lastActiveSceneView.camera.transform.TransformPoint(Vector3.forward * 3f);
            var waterInstance = Instance;
            if (waterInstance != null) pos.y = Instance.WaterLevel;
            else pos.y                       = 0;
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(50, 20, 50);

            SceneView.lastActiveSceneView.LookAt(go.transform.position);
            var simZone = go.AddComponent<KWS_LocalWaterZone>();

            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;

            CreateOrFindWaterSystem(go.transform.position, false);
        }       
      
        [MenuItem("GameObject/Effects/KWS Water/Ocean")]
        private static void CreateOceanRendererEditor(MenuCommand menuCommand)
        {
            var go            = new GameObject("Ocean");
            var pos           = SceneView.lastActiveSceneView.camera.transform.TransformPoint(Vector3.forward * 3f);
            var waterInstance = Instance;
            if (waterInstance != null) pos.y = Instance.WaterLevel;
            else pos.y                       = 0;
            go.transform.position = pos;

            SceneView.lastActiveSceneView.LookAt(go.transform.position);
            var ocean = go.AddComponent<KWS_Ocean>();

            CreateOrFindWaterSystem(go.transform.position, false);
            
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;

            
        }
        
        void OnDrawGizmosSelected()
        {
            if (KWS_Ocean.Instance != null) return;
            
            Gizmos.color = new Color(0.1f, 0.1f, 0.95f, 0.3f);
            Gizmos.DrawCube(transform.position, new Vector3(1000, 0.1f, 1000));
        }
#endif
    }
}