using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class WeaponThumbnailGenerator : EditorWindow
{
    // Folders (Project relative)
    private const string PrefabsFolder = "Assets/Resources/Models/Weapons";
    private const string OutputFolder = "Assets/Resources/Thumbnails/Weapons";

    // Render
    private int _size = 512;    // square
    private float _padPct = 1.15f;  // 15% loose frame
    private LayerMask _layer = 30;  // temp layer to render only the spawned weapon (choose a free one)

    // Per-name scaling (make pistols bigger etc)
    // Key = substring match (case-insensitive), Value = extra scale multiplier
    private (string contains, float mul)[] _boosts = new[]
    {
        ("AP85",    0.60f),
        ("C1911",   0.60f),
        ("M9",      0.60f),
        ("P350",    0.60f),
        ("PT8",     0.60f),
        ("Eder22",  0.60f),
        ("Revolver",0.60f),
        ("590A1"   ,1.0f),
        ("Hunter85",1.0f),
    };

    // Unlit black material used only on the temporary spawned copy
    private static Material _silhouetteMat;

    [MenuItem("Tools/Weapons/Generate Thumbnails (Transparent)")]
    public static void ShowWindow()
    {
        var w = GetWindow<WeaponThumbnailGenerator>("Weapon Thumbnails");
        w.minSize = new Vector2(380, 200);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Generate COD-style silhouettes with transparency", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"Prefabs : {PrefabsFolder}");
        EditorGUILayout.LabelField($"Output  : {OutputFolder}");
        _size = EditorGUILayout.IntField("Image size", _size);
        _padPct = EditorGUILayout.Slider("Auto-frame padding", _padPct, 1.00f, 1.40f);

        if (GUILayout.Button("Generate All", GUILayout.Height(34)))
        {
            GenerateAll();
        }
    }

    private void GenerateAll()
    {
        if (!Directory.Exists(OutputFolder)) Directory.CreateDirectory(OutputFolder);

        // find prefabs under the folder
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabsFolder });
        if (guids == null || guids.Length == 0)
        {
            Debug.LogWarning($"No prefabs found in {PrefabsFolder}");
            return;
        }

        // prepare shared silhouette material
        if (_silhouetteMat == null)
        {
#if USING_URP
            _silhouetteMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
#else
            var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            _silhouetteMat = new Material(sh);
#endif
            _silhouetteMat.SetColor("_BaseColor", Color.black);   // URP Unlit
            if (_silhouetteMat.HasProperty("_Color")) _silhouetteMat.SetColor("_Color", Color.black); // Built-in Unlit/Color
        }

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) continue;

            var fileName = Path.GetFileNameWithoutExtension(path) + ".png";
            var outPath = Path.Combine(OutputFolder, fileName).Replace("\\", "/");

            try
            {
                GenerateForPrefab(prefab, outPath);
                Debug.Log($"✓ {fileName}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed {prefab.name}: {ex.Message}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"Done. Thumbnails in {OutputFolder}");
    }

    private void GenerateForPrefab(GameObject prefab, string outPath)
    {
        // Stage
        var rootGO = new GameObject("~ThumbStage");
        var camGO = new GameObject("~ThumbCam");
        var cam = camGO.AddComponent<Camera>();

        // Ensure transparent background
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.orthographic = true;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 100f;

        // Use a dedicated layer so we only render the spawned instance
        int layer = _layer == 0 ? 30 : (int)Mathf.Log(_layer.value, 2);
        cam.cullingMask = 1 << layer;

        // Spawn a temporary copy
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        inst.transform.SetParent(rootGO.transform, false);
        SetLayerRecursive(inst, layer);

        // Replace materials with black unlit (instance only)
        var renderers = inst.GetComponentsInChildren<Renderer>(true);
        var matsCache = renderers.Select(r => (r, r.sharedMaterials)).ToArray();
        foreach (var r in renderers)
        {
            var arr = Enumerable.Repeat(_silhouetteMat, r.sharedMaterials.Length).ToArray();
            r.sharedMaterials = arr;
        }

        // --- Ensure a consistent profile orientation BEFORE computing bounds ---
        inst.transform.rotation = Quaternion.identity;  // reset any prefab yaw
        AutoOrientToCamera(inst, cam);                  // align longest axis to camera's right
        // ----------------------------------------------------------------------

        // Auto frame (compute after orientation)
        var b = ComputeBounds(inst);
        var max = Mathf.Max(b.size.x, b.size.y, b.size.z);
        float orthoSize = (max * 0.5f) * _padPct;

        // Per-name boost (pistols bigger, etc.)
        float boost = 1f;
        string name = prefab.name.ToLowerInvariant();
        foreach (var (contains, mul) in _boosts)
        {
            if (!string.IsNullOrEmpty(contains) && name.Contains(contains.ToLowerInvariant()))
            {
                boost *= mul;
            }
        }

        cam.orthographicSize = orthoSize / boost;

        // Position camera (orthographic, head-on)
        cam.transform.position = b.center + new Vector3(0, 0, -10f);
        cam.transform.rotation = Quaternion.identity;

        // Render with alpha
        var rt = new RenderTexture(_size, _size, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 8,
            useMipMap = false,
            autoGenerateMips = false
        };

        var tex = new Texture2D(_size, _size, TextureFormat.RGBA32, false, false);

        var prev = RenderTexture.active;
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, _size, _size), 0, 0, false);
        tex.Apply(false, false);

        // Encode RGBA PNG
        var png = tex.EncodeToPNG();
        File.WriteAllBytes(outPath, png);

        // Import settings: keep alpha
        AssetDatabase.ImportAsset(outPath);
        var ti = (TextureImporter)AssetImporter.GetAtPath(outPath);
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Default;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.sRGBTexture = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.wrapMode = TextureWrapMode.Clamp;
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();
        }

        // Restore and cleanup
        foreach (var (r, mats) in matsCache) r.sharedMaterials = mats;

        cam.targetTexture = null;
        RenderTexture.active = prev;
        DestroyImmediate(rt);
        DestroyImmediate(tex);
        DestroyImmediate(rootGO);
        DestroyImmediate(camGO);
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
    }

    private static Bounds ComputeBounds(GameObject go)
    {
        var rends = go.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        var b = new Bounds(rends[0].bounds.center, Vector3.zero);
        foreach (var r in rends) b.Encapsulate(r.bounds);
        return b;
    }

    // Align the longest local axis horizontally so the gun is in clean profile.
    static void AutoOrientToCamera(GameObject go, Camera cam)
    {
        go.transform.localRotation = Quaternion.identity;

        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        var bounds = new Bounds(go.transform.InverseTransformPoint(renderers[0].bounds.center), Vector3.zero);
        foreach (var r in renderers)
            bounds.Encapsulate(go.transform.InverseTransformPoint(r.bounds.center) + r.bounds.extents);

        var size = bounds.size;
        Vector3 longestLocalAxis =
            (size.x >= size.y && size.x >= size.z) ? Vector3.right :
            (size.y >= size.x && size.y >= size.z) ? Vector3.up :
                                                     Vector3.forward;

        var targetRight = cam.transform.right; // profile view
        var current = go.transform.TransformDirection(longestLocalAxis);
        var rot1 = Quaternion.FromToRotation(current, targetRight);

        go.transform.rotation = rot1 * go.transform.rotation;

        // Remove weird roll: keep 'up' vertical
        var fwd = go.transform.forward;
        go.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
    }
}
