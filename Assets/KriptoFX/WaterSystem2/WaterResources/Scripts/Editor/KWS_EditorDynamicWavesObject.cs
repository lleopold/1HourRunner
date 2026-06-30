#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using static KWS.KWS_EditorUtils;
using static KWS.KWS_EditorVideoTipsSettings;

namespace KWS
{
    [CustomEditor(typeof(KWS_DynamicWavesSimulationEffector))]
    internal class KWS_EditorDynamicWavesObject : Editor
    {
        private KWS_DynamicWavesSimulationEffector _target;


        public override void OnInspectorGUI()
        {
            //var isChanged = DrawDefaultInspector();
            _target = (KWS_DynamicWavesSimulationEffector)target;

          
            Undo.RecordObject(_target, "Changed Dynamic Waves Object");

            EditorGUI.BeginChangeCheck();
            EditorGUIUtility.labelWidth = 220;

            _target.InteractionType = (KWS_DynamicWavesSimulationEffector.InteractionTypeEnum)EnumPopup("Interaction Type", "", _target.InteractionType, VideoTips.EffectorType);
            Line();
           
            if (_target.InteractionType == KWS_DynamicWavesSimulationEffector.InteractionTypeEnum.ObstacleObject)
            {
                EditorGUILayout.Space(10);
                if (_target.gameObject.layer != KWS_Settings.Water.WaterLayer)
                {
                    EditorGUILayout.HelpBox("SET THE WATER LAYER! " + Environment.NewLine +
                                            "When using a dynamic ObstacleObject, you need to set water layer to avoid double obstacles intersection", MessageType.Warning);
                }
                EditorGUILayout.Space(10);
                
                _target.UseMeshFilterAsSource = Toggle("Use Mesh Filter As Source", "", _target.UseMeshFilterAsSource);

                if (_target.UseMeshFilterAsSource == false)
                {
                    _target.OverrideObstacleMesh = (Mesh)EditorGUILayout.ObjectField(_target.OverrideObstacleMesh, typeof(Mesh), true);
                }

                _target.MeshOffset                          = Vector3Field("Mesh Offset", "", _target.MeshOffset);
                _target.MeshScale                           = Vector3Field("Mesh Scale",  "", _target.MeshScale);
                _target.ActivateWhenIntersectsWater         = Toggle("Activate When Intersects Water", "", _target.ActivateWhenIntersectsWater);
            }
            


            if (_target.InteractionType == KWS_DynamicWavesSimulationEffector.InteractionTypeEnum.WaterSource)
            { 
                _target.ForceType          = (KWS_DynamicWavesSimulationEffector.ForceTypeEnum)EnumPopup("Mesh", "", _target.ForceType, VideoTips.EffectorSourceMeshType);
                _target.ConstantFlowRate   = Slider("Constant Flow Rate",   "", _target.ConstantFlowRate,    0,  1, VideoTips.EffectorSourceConstantFlowRate);
               _target.DirectionalVelocity = Slider("Directional Velocity", "", _target.DirectionalVelocity, -1, 1, VideoTips.EffectorSourceDirectionalVelocity);
                
                _target.UseSourceColor              = Toggle("Use Source Color", "", _target.UseSourceColor);
                if (_target.UseSourceColor)
                {
                    _target.SourceColor = ColorField("Source Color", "", _target.SourceColor, false, false, false);
                }

                _target.UseWaterSurfaceIntersection = false;
            }

            if (_target.InteractionType == KWS_DynamicWavesSimulationEffector.InteractionTypeEnum.WaterDrain)
            {
                _target.ForceType                   = (KWS_DynamicWavesSimulationEffector.ForceTypeEnum)EnumPopup("Mesh", "", _target.ForceType, VideoTips.EffectorSourceMeshType);
                _target.ConstantDrainRate           = Slider("Drain Rate",           "", _target.ConstantDrainRate,           0,  1);
                _target.UseWaterSurfaceIntersection = Toggle("Use Water Surface Intersection", "", _target.UseWaterSurfaceIntersection, VideoTips.EffectorWaterSurfaceIntersection);
            }

            if (_target.InteractionType == KWS_DynamicWavesSimulationEffector.InteractionTypeEnum.ForceObject)
            {
                _target.ForceType           = (KWS_DynamicWavesSimulationEffector.ForceTypeEnum)EnumPopup("Mesh", "", _target.ForceType, VideoTips.EffectorForceMeshType);
                _target.MotionForce         = Slider("Motion Force",         "", _target.MotionForce,         0,  10, VideoTips.EffectorMotionForce);
                _target.ConstantForce       = Slider("Constant Force",       "", _target.ConstantForce,       0,  1,  VideoTips.EffectorConstantForce); 
                _target.DirectionalVelocity = Slider("Directional Velocity", "", _target.DirectionalVelocity, -1, 1,  VideoTips.EffectorDirectionalVelocity);
                Line();
                _target.UseRotationForce            = Toggle("Use Rotation Force",             "", _target.UseRotationForce, VideoTips.EffectorUseRotationForce);
                _target.UseWaterSurfaceIntersection = Toggle("Use Water Surface Intersection", "", _target.UseWaterSurfaceIntersection, VideoTips.EffectorWaterSurfaceIntersection);
            }

          

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_target);
                // AssetDatabase.SaveAssets();
            }
        }
    }
}

#endif