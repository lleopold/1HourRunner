using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

// ==============================================================================
//  DemoMaterialPipelineFixer: Assigns the correct shader to the demo material based on the active render pipeline.
// ==============================================================================
namespace JuiceBox
{
    [InitializeOnLoad]
    static class DemoMaterialPipelineFixer
    {
        const string MaterialGuid = "d8e9c7f6b5a4314253637485a6b7c8d9";
        const string ShaderStandard = "Standard";
        const string ShaderURP = "Universal Render Pipeline/Lit";
        const string ShaderHDRP = "HDRP/Lit";

        static DemoMaterialPipelineFixer()
        {
            string path = AssetDatabase.GUIDToAssetPath(MaterialGuid);
            if (string.IsNullOrEmpty(path))
                return;

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
                return;

            string targetShaderName = GetTargetShaderName();
            if (mat.shader != null && mat.shader.name == targetShaderName)
                return;

            Shader shader = Shader.Find(targetShaderName);
            if (shader == null)
            {
                Debug.LogWarning("[JuiceBox] Could not find shader '" + targetShaderName +
                    "'. DemoCube material was not updated.");
                return;
            }

            mat.shader = shader;
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssetIfDirty(mat);
        }

        static string GetTargetShaderName()
        {
            RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline == null)
                return ShaderStandard;

            string typeName = pipeline.GetType().Name;
            if (typeName.Contains("Universal"))
                return ShaderURP;
            if (typeName.Contains("HDRender"))
                return ShaderHDRP;

            return ShaderStandard;
        }
    }
}