#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;


namespace KWS
{
    public class KWS_PipelineDefines : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets,
                                           string[] deletedAssets,
                                           string[] movedAssets,
                                           string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                if (assetPath.Contains("KriptoFX/WaterSystem2"))
                {
#if KWS_DEBUG
                    //Debug.Log("KWS2 assets imported, applying keyword setup...");
#endif
                    CheckAndUpdateDefines();
                    break;
                }
            }
        }
        


        internal static void CheckAndUpdateDefines()
        {
            var actualPipeline = KWS_EditorUtils.GetCurrentPipeline();
            var shaderDefine   = KWS_EditorUtils.GetActiveShaderDefine(KWS_Settings.ShaderPaths.KWS_WaterDefines);

            var scriptDefine = KWS_EditorUtils.UnityPipeline.Unknown;


#if KWS_BUILTIN
            scriptDefine = KWS_EditorUtils.UnityPipeline.Builtin;
#endif

#if KWS_URP
            scriptDefine = KWS_EditorUtils.UnityPipeline.URP;
#endif

#if KWS_HDRP
            scriptDefine = KWS_EditorUtils.UnityPipeline.HDRP;
#endif

#if KWS_DEBUG
            //Debug.Log("actualPipeline " + actualPipeline + "    " + shaderDefine  + "    " + shaderDefine);
#endif


            if (actualPipeline != scriptDefine || actualPipeline != shaderDefine || scriptDefine == KWS_EditorUtils.UnityPipeline.Unknown)
            {
                UpdatePipelineDefine(actualPipeline);
            }

            bool scriptDefineOceanModule = false;
            #if KWS_HD_MODULE_INSTALLED
                scriptDefineOceanModule = true;
            #endif
            bool actualOceanModule = KWS_EditorUtils.IsHDModuleInstalled();
            
            if(scriptDefineOceanModule != actualOceanModule) UpdateModuleDefine();
           
        }


        static void UpdatePipelineDefine(KWS_EditorUtils.UnityPipeline actualPipeline)
        {
            var    group   = BuildTargetGroup.Standalone;
           
            string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group));
            
            defines = Remove(defines, "KWS_BUILTIN");
            defines = Remove(defines, "KWS_URP");
            defines = Remove(defines, "KWS_HDRP");

            KWS_EditorUtils.DisableAllShaderTextDefines(KWS_Settings.ShaderPaths.KWS_WaterDefines, lockFile: false, "KWS_BUILTIN", "KWS_URP", "KWS_HDRP");

            switch (actualPipeline)
            {
                case KWS_EditorUtils.UnityPipeline.Builtin:
                    defines += ";KWS_BUILTIN";
                    KWS_EditorUtils.SetShaderTextDefine(KWS_Settings.ShaderPaths.KWS_WaterDefines, lockFile: false, "KWS_BUILTIN", enabled: true);
                    break;
                case KWS_EditorUtils.UnityPipeline.URP:
                    defines += ";KWS_URP";
                    KWS_EditorUtils.SetShaderTextDefine(KWS_Settings.ShaderPaths.KWS_WaterDefines, lockFile: false, "KWS_URP", enabled: true);
                    break;
                case KWS_EditorUtils.UnityPipeline.HDRP:
                    defines += ";KWS_HDRP";
                    KWS_EditorUtils.SetShaderTextDefine(KWS_Settings.ShaderPaths.KWS_WaterDefines, lockFile: false, "KWS_HDRP", enabled: true);
                    break;
                case KWS_EditorUtils.UnityPipeline.Unknown:
                    Debug.LogError("KWS2 Water Unknown RenderPipeline: ");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(actualPipeline), actualPipeline, null);
            }
#if KWS_DEBUG
            //Debug.Log("Water Pipeline: " + actualPipeline + "     defines "+  defines );
#endif

           
            EditorApplication.delayCall += () =>
            {
                //PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines);
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group), defines);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            };
        }

        static void UpdateModuleDefine()
        {
            var    group   = BuildTargetGroup.Standalone;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);

            defines = Remove(defines, "KWS_HD_MODULE_INSTALLED");
            
            if (KWS_EditorUtils.IsHDModuleInstalled())
            {
                defines += ";KWS_HD_MODULE_INSTALLED";
            }
            
            EditorApplication.delayCall += () =>
            {
                //PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines);
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group), defines);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            };
            //Debug.Log(defines);
        }


        static string Remove(string input, string keyword)
        {
            return input.Replace(keyword + ";", "").Replace(";" + keyword, "").Replace(keyword, "");
        }
        
        
        
#if KWS_DEBUG
        
       
        [MenuItem("KWS/Use BUILT-IN", priority = 2002)]
        static void UseBuiltin()
        {
            ApplyPipeline(null, "BUILT-IN");
        }

        [MenuItem("KWS/Use URP", priority = 2003)]
        static void UseURP()
        {
            var asset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/PipelineSettings/URP Settings.asset");
            ApplyPipeline(asset, "URP");
        }

        [MenuItem("KWS/Use HDRP", priority = 2004)]
        static void UseHDRP()
        {
            var asset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/PipelineSettings/HDRP Settings.asset");
            ApplyPipeline(asset, "HDRP");
        }
        
        static void ApplyPipeline(RenderPipelineAsset asset, string label)
        {
            GraphicsSettings.defaultRenderPipeline = asset;  
            QualitySettings.renderPipeline         = asset;

            Debug.Log($"[KWS] Switched Render Pipeline to {label}");
        }

#endif

        
        
    }
}
#endif