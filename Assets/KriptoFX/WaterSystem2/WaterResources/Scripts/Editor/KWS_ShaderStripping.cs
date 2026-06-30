#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KWS.Build
{
    internal sealed class KWS_PreProcessBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public static readonly HashSet<string> _waterVariants = new(StringComparer.Ordinal);

        public static void Clear() => _waterVariants.Clear();

        public static void AddFromKeys(IEnumerable<string> keys)
        {
            var list = new List<string>(keys);
            if (list.Count == 0) return;
            list.Sort(StringComparer.Ordinal);
            _waterVariants.Add(string.Join("|", list));
        }


        public void OnPreprocessBuild(BuildReport report)
        {
            KWS_VariantStubBuilder.RemoveAllMaterials();
            CollectAllVariantsFromAllScenes();

            var pathToResourcesFolder = KW_Extensions.GetResourcesFolderAssetPath();
            if (string.IsNullOrEmpty(pathToResourcesFolder)) return;

            var relativePath = KW_Extensions.GetRelativePathToBuildShaderFeatureMaterials();

            KW_Extensions.WaterLog("Path: " + relativePath);

            KWS_ShaderFeatureCatalog.BuildFromFolder(KW_Extensions.GetShadersFolderAssetPath().GetRelativeToAssetsPath());
            KWS_VariantStubBuilder.GenerateShaderFeatureMaterials(_waterVariants, outputFolder: relativePath, shaderFilter: s => s != null && s.name.StartsWith("Hidden/KriptoFX/KWS/"));

            KW_Extensions.WaterLog($"[KWS] Generated 'ShaderFeature' Materials: {_waterVariants.Count} combinations.");
        }

        void CollectAllVariantsFromAllScenes()
        {
            var opened = new List<Scene>();
            var active = SceneManager.GetActiveScene().path;

            try
            {
                _waterVariants.Clear();
                var paths = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path);
                foreach (var p in paths)
                {
                    var sc = EditorSceneManager.OpenScene(p, OpenSceneMode.Additive);
                    opened.Add(sc);
                    CollectFromScene(sc);
                }
            }
            finally
            {
                foreach (var sc in opened) EditorSceneManager.CloseScene(sc, true);
                if (!string.IsNullOrEmpty(active)) EditorSceneManager.OpenScene(active, OpenSceneMode.Single);
            }
        }

        void CollectFromScene(Scene sc)
        {
            var managers = sc.GetRootGameObjects()
                             .SelectMany(go => go.GetComponentsInChildren<WaterSystem>(true))
                             .ToList();
            if (managers.Count == 0) return;

            var qualityLevels = KWS_WaterSettingsRuntimeLoader._waterSystemQualitySettings?.qualityLevelSettings;
            if (qualityLevels == null || qualityLevels.Count == 0) return;

            foreach (var waterManager in managers)
            {
                foreach (var qualityLevel in qualityLevels)
                {
                    var variant = GetCurrentQualityKeywords(waterManager, qualityLevel);
                    AddFromKeys(BuildFeatureKeys(variant));
                }
            }
        }

        struct ActiveVariant
        {
            public bool useReflectSun;
            public bool useVolumetric;
            public bool useCaustic;
            public bool useCausticFiltering;
            public bool useCausticDispersion;
            public bool useSSR;
            public bool usePlanar;
            public bool useRefrIOR;
            public bool useRefrDisp;
            public bool useUnderwater;
            public bool useHalfLineTension;
        }


        static ActiveVariant GetCurrentQualityKeywords(WaterSystem waterSettings, WaterQualityLevelSettings qualitySettings)
        {
            ActiveVariant variant = default;

            variant.usePlanar     = WaterQualityLevelSettings.ResolveQualityOverride(waterSettings.PlanarReflection,      qualitySettings.UsePlanarReflection);
            variant.useSSR        = WaterQualityLevelSettings.ResolveQualityOverride(waterSettings.ScreenSpaceReflection, qualitySettings.UseScreenSpaceReflection);
            variant.useRefrDisp   = WaterQualityLevelSettings.ResolveQualityOverride(waterSettings.RefractionDispersion,  qualitySettings.UseRefractionDispersion);
            variant.useVolumetric = WaterQualityLevelSettings.ResolveQualityOverride(waterSettings.VolumetricLighting,    qualitySettings.UseVolumetricLight);
            variant.useCaustic    = WaterQualityLevelSettings.ResolveQualityOverride(waterSettings.CausticEffect,         qualitySettings.UseCausticEffect);
            variant.useUnderwater = WaterQualityLevelSettings.ResolveQualityOverride(waterSettings.UnderwaterEffect,      qualitySettings.UseUnderwaterEffect);

            variant.useHalfLineTension   = waterSettings.UseUnderwaterHalfLineTensionEffect;
            variant.useReflectSun        = waterSettings.ReflectSun;
            variant.useRefrIOR           = waterSettings.RefractionMode == WaterQualityLevelSettings.RefractionModeEnum.PhysicalAproximationIOR;
            variant.useCausticFiltering  = qualitySettings.UseOceanCausticHighQualityFiltering;
            variant.useCausticDispersion = qualitySettings.UseOceanCausticDispersion;


            return variant;
        }

        static IEnumerable<string> BuildFeatureKeys(ActiveVariant variant)
        {
            if (variant.useReflectSun) yield return "KWS_REFLECT_SUN";
            if (variant.useVolumetric) yield return "KWS_USE_VOLUMETRIC_LIGHT";
            if (variant.useCaustic) yield return "KWS_USE_CAUSTIC";
            if (variant.useCausticFiltering) yield return "KWS_USE_CAUSTIC_FILTERING";
            if (variant.useCausticDispersion) yield return "KWS_USE_CAUSTIC_DISPERSION";
            if (variant.useSSR) yield return "KWS_SSR_REFLECTION";
            if (variant.usePlanar) yield return "KWS_USE_PLANAR_REFLECTION";
            if (variant.useRefrIOR) yield return "KWS_USE_REFRACTION_IOR";
            if (variant.useRefrDisp) yield return "KWS_USE_REFRACTION_DISPERSION";
            if (variant.useHalfLineTension) yield return "KWS_USE_HALF_LINE_TENSION";
        }


#if KWS_DEBUG

        [MenuItem("KWS/Generate Water Variant Stubs (Resources)", priority = 2000)]
        static void MenuGenerateGenerate()
        {
            var pre = new KWS_PreProcessBuild();
            pre.OnPreprocessBuild(null);
            KW_Extensions.WaterLog("[KWS] Variant stubs regenerated (manual).");
        }


        [MenuItem("KWS/Clear Water Variant Stubs (Resources)", priority = 2001)]
        static void MenuGenerateClear()
        {
            var pre = new KWS_PostProcessBuild();
            pre.OnPostprocessBuild(null);
            KW_Extensions.WaterLog("[KWS] Variant stubs removed (manual).");
        }


#endif
    }

    internal sealed class KWS_PostProcessBuild : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            KWS_VariantStubBuilder.RemoveAllMaterials();
        }
    }

    internal static class KWS_VariantStubBuilder
    {
        public static void RemoveAllMaterials()
        {
            var relativePath = KW_Extensions.GetRelativePathToBuildShaderFeatureMaterials();

            var guids = AssetDatabase.FindAssets("t:Material", new[] { relativePath });
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.DeleteAsset(assetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            KW_Extensions.WaterLog("[KWS] Variant stubs removed (manual).");
        }
        
        public static void GenerateShaderFeatureMaterials(
            IReadOnlyCollection<string> signatures,
            string                      outputFolder,
            Func<Shader, bool>          shaderFilter)
        {
            EnsureFolder(outputFolder);

            var oldGuids = AssetDatabase.FindAssets("t:Material", new[] { outputFolder });
            foreach (var g in oldGuids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                AssetDatabase.DeleteAsset(p);
            }

            var shaders = new List<Shader>();
            foreach (var guid in AssetDatabase.FindAssets("t:Shader"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sh   = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shaderFilter(sh)) shaders.Add(sh);
            }

            if (shaders.Count == 0)
            {
                KW_Extensions.WaterLog("[KWS] Variant materials: water shaders not found.");

                return;
            }

            foreach (var shader in shaders)
            {
                if (!KWS_ShaderFeatureCatalog.FeatureByShader.TryGetValue(shader.name, out var shaderFeatures) ||
                    shaderFeatures.Count == 0)
                    continue;

                foreach (var sig in signatures)
                {
                    var allKeys = string.IsNullOrEmpty(sig)
                        ? Array.Empty<string>()
                        : sig.Split('|');

                    var filtered = allKeys.Where(k => shaderFeatures.Contains(k))
                                          .Distinct(StringComparer.Ordinal)
                                          .ToArray();

                    if (filtered.Length == 0) continue;

                    var mat = new Material(shader) { name = BuildMatName(shader.name, filtered) };
                    foreach (var k in filtered) mat.EnableKeyword(k);

                    var assetPath = $"{outputFolder}/{Sanitize(mat.name)}.mat";
                    AssetDatabase.CreateAsset(mat, assetPath);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static string BuildMatName(string shaderName, string[] keys)
        {
            var shortShader = shaderName.Replace("Hidden/", "").Replace("KriptoFX/", "").Replace("KWS/", "");
            var keyPart     = keys.Length == 0 ? "BASE" : string.Join("__", keys);

            if (keyPart.Length > 80) keyPart = keyPart.Substring(0, 80);
            return $"KWS_Stub_{shortShader}__{keyPart}";
        }

        static string Sanitize(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
            return s.Replace(' ', '_');
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Replace('\\', '/').Split('/');
            var acc   = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = acc + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(acc, parts[i]);
                acc = next;
            }
        }
    }

    static class KWS_ShaderFeatureCatalog
    {
        public static readonly Dictionary<string, HashSet<string>> FeatureByShader = new(StringComparer.Ordinal);


        static readonly System.Text.RegularExpressions.Regex kFeatureLine =
            new(@"^\s*#\s*pragma\s+shader_feature(?:_local)?(?:_vertex|_fragment)?\s+(.+)$",
                System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.Compiled);

        static readonly System.Text.RegularExpressions.Regex kToken =
            new(@"[A-Za-z0-9_]+", System.Text.RegularExpressions.RegexOptions.Compiled);

        public static void BuildFromFolder(string absFolderPath)
        {
            FeatureByShader.Clear();


            foreach (var guid in AssetDatabase.FindAssets("t:Shader", new[] { absFolderPath }))
            {
                var path   = AssetDatabase.GUIDToAssetPath(guid);
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (!shader) continue;

                var set = FeatureByShader.TryGetValue(shader.name, out var exist)
                    ? exist
                    : (FeatureByShader[shader.name] = new HashSet<string>(StringComparer.Ordinal));

                var text = File.ReadAllText(path);
                foreach (System.Text.RegularExpressions.Match m in kFeatureLine.Matches(text))
                foreach (System.Text.RegularExpressions.Match t in kToken.Matches(m.Groups[1].Value))
                    if (t.Value != "_")
                        set.Add(t.Value);
            }
        }
    }
}
#endif