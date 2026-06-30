#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using static KWS.KWS_EditorUtils;
using static KWS.KWS_EditorVideoTipsSettings;
using Description = KWS.KWS_EditorTextDescription;
using link = KWS.KWS_EditorUrlLinks;

namespace KWS
{
    [CustomEditor(typeof(KWS_LocalWaterZone))]
    internal class KWS_EditorLocalWaterZone : Editor
    {
        private KWS_LocalWaterZone _target;
        public override void OnInspectorGUI()
        {
            //var isChanged = DrawDefaultInspector();
            _target = (KWS_LocalWaterZone)target;
            
            Undo.RecordObject(_target, "Changed Local Water Zone");

            EditorGUI.BeginChangeCheck();
            EditorGUIUtility.labelWidth = 220;
          
            EditorGUILayout.Space(20);  
            
            if (_target.OverrideColorSettings)
            { 
                EditorGUILayout.HelpBox("When OverrideColorSettings is enabled, rotation is locked by design (due clustered rendering system)", MessageType.Info);
                if(_target.UseSphericalBlending) EditorGUILayout.HelpBox("Spherical blending is enabled, so the scale is always in the shape of a cube", MessageType.Info);
            }
           
            KWS2_TabWithEnabledToogle(ref _target.OverrideColorSettings, ref _target.ShowColorSettings, "Override Color", ColorSettings, WaterSystem.WaterSettingsCategory.LocalZone, null, foldoutSpace: 14);
            KWS2_TabWithEnabledToogle(ref _target.OverrideWindSettings,  ref _target.ShowWindSettings,  "Override Wind",  WindSettings,  WaterSystem.WaterSettingsCategory.LocalZone, null, foldoutSpace: 14);

            if (_target.OverrideMesh || _target.UseClipMask)
            {
                GUI.enabled            = false;
                _target.OverrideHeight = false;
                KWS2_TabWithEnabledToogle(ref _target.OverrideHeight, ref _target.ShowHeightSettings, "Override Height (Can't be used due to active Override Mesh)", HeightSettings, WaterSystem.WaterSettingsCategory.LocalZone, null, foldoutSpace: 14);
                GUI.enabled = true;
            }
            else
            {
                KWS2_TabWithEnabledToogle(ref _target.OverrideHeight, ref _target.ShowHeightSettings, "Override Height", HeightSettings, WaterSystem.WaterSettingsCategory.LocalZone, null, foldoutSpace: 14);
            }
            
            
            EditorGUI.BeginChangeCheck();
            KWS2_TabWithEnabledToogle(ref _target.OverrideMesh, ref _target.ShowMeshSettings, "Override Mesh (Experimental)", MeshSettings, WaterSystem.WaterSettingsCategory.LocalZone, null, foldoutSpace: 14);
            if (EditorGUI.EndChangeCheck()) { _target.UpdateTransform(); }
            
            EditorGUI.BeginChangeCheck();
            KWS2_TabWithEnabledToogle(ref _target.UseClipMask, ref _target.ShowClipSettings, "Clip Masking", ClipMaskSettings, WaterSystem.WaterSettingsCategory.LocalZone, null, foldoutSpace: 14);
            if (EditorGUI.EndChangeCheck()) { _target.UpdateTransform(); }
            
          
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_target);
                // AssetDatabase.SaveAssets();
            }

        }

        void ColorSettings()
        {
            _target.Transparent          = Slider("Transparent (Meters)", Description.Color.Transparent, _target.Transparent, 2.0f, 100f, VideoTips.Transparent);
            _target.WaterColor           = ColorField("Water Color",     Description.Color.WaterColor,     _target.WaterColor,     false, false, false);
            _target.TurbidityColor       = ColorField("Turbidity Color", Description.Color.TurbidityColor, _target.TurbidityColor, false, false, false);
            _target.UseSphericalBlending = Toggle("Use Sphere Blending", "", _target.UseSphericalBlending);
           
        }
        
        void WindSettings()
        {
            _target.WindStrengthMultiplier = Slider("Strength Multiplier", "", _target.WindStrengthMultiplier, 0, 1);
            _target.WindEdgeBlending       = Slider("Edge Blending", "", _target.WindEdgeBlending, 0, 1);
        }
        
        void RainSettings()
        {
            _target.WindStrengthMultiplier = Slider("Strength Multiplier", "", _target.WindStrengthMultiplier, 0, 1);
            _target.WindEdgeBlending       = Slider("Edge Blending",       "", _target.WindEdgeBlending,       0, 1);
        }
        
        void HeightSettings()
        {
            _target.ClipWaterBelowZone = Toggle("Clip Water Below Zone", "", _target.ClipWaterBelowZone);
            _target.HeightEdgeBlending = Slider("Edge Blending", "", _target.HeightEdgeBlending, 0, 1);
        }
        
        void MeshSettings()
        {
            
            _target.CustomMesh               = (Mesh) EditorGUILayout.ObjectField("Custom Mesh", _target.CustomMesh, typeof(Mesh), true);
           
        }
        
        void ClipMaskSettings()
        {
            
            _target.ClipMesh               = (Mesh) EditorGUILayout.ObjectField("Clip Mesh", _target.ClipMesh, typeof(Mesh), true);
            
            #if KWS_HDRP
                if(_target.UseClipMask && _target.ClipMesh) EditorGUILayout.LabelField("The 'Underwater Half Line Tension' effect does not work while clip " + 
                                                                                       "zones are visible on the screen; this is an HDRP rendering limitation", KWS_EditorUtils.NotesLabelStyleFade);
            #endif
            
        }
    }
}

#endif