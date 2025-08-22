using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PreLoad : MonoBehaviour
{
    // ---------- Static cache that survives scene load ----------
    public static class Precache
    {
        // Keep strong refs so GC/UnloadUnusedAssets won’t evict them.
        private static readonly Dictionary<string, Object> _cache = new();

        public static bool Has(string key) => _cache.ContainsKey(key);
        public static T Get<T>(string key) where T : Object => _cache.TryGetValue(key, out var o) ? o as T : null;
        public static void Put(string key, Object obj) { if (obj) _cache[key] = obj; }
        public static int Count => _cache.Count;
    }
    // Add these fields (top of class)
    [SerializeField] private float minShowTime = 2.0f;     // already there; increase to 2–3s
    [SerializeField] private HoldMode holdWhenReady = HoldMode.FixedSeconds;
    [SerializeField] private float readyHoldSeconds = 1.5f; // extra time AFTER loading is ready
    [SerializeField] private bool showPressAnyKey = false;  // if true, overrides FixedSeconds

    private enum HoldMode { None, FixedSeconds, PressAnyKey }
    [SerializeField] private float preloadBudgetSeconds = 1.5f; // max time to spend preloading before we switch scenes
    [SerializeField] private bool continuePreloadAfterActivation = true;

    [Header("Scene")]
    [SerializeField] private string sceneToLoad = "Start";

    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;      // auto-grab if null
    [SerializeField] private string loadingLabelName = "loading_text"; // in UXML

    [Header("Smoke (optional)")]
    [Tooltip("Resources path to a smoke/FX prefab. Leave empty to skip.")]
    [SerializeField] private string smokePrefabPath = "FX/Smoke";
    [SerializeField] private Vector3 smokeOffset = new(0, 0, 3);
    [SerializeField] private bool smokeFollowsCamera = true;

    [Header("Pre-cache (background; nothing is shown)")]
    [Tooltip("Prefab IDs under Resources/Models/Weapons that should be preloaded and cached.")]
    [SerializeField]
    private string[] weaponIdsToPrecache =
        { "WPN_AP85", "WPN_C1911", "WPN_Eder22", "WPN_M9", "WPN_P350", "WPN_PT8", "WPN_Revolver",
          "WPN_SMG5", "WPN_R90", "WPN_KM4", "WPN_CV47", "WPN_CX8", "WPN_M16", "WPN_MK18",
          "WPN_FBS", "WPN_DT22", "WPN_590A1", "WPN_M4", "WPN_Hunter85" };

    [Tooltip("If true, we also load shared materials/textures next to models if they exist (lightweight).")]
    [SerializeField] private bool preloadLikelyDeps = true;

    // Internals
    private Label _loadingLabel;
    private static bool _instanceAlive;

    void Awake()
    {
        if (_instanceAlive) { Destroy(gameObject); return; }
        _instanceAlive = true;
        DontDestroyOnLoad(gameObject); // keep this alive until we activate next scene

        if (!uiDocument) uiDocument = FindFirstObjectByType<UIDocument>();
        if (uiDocument != null)
            _loadingLabel = uiDocument.rootVisualElement?.Q<Label>(loadingLabelName);

        TrySpawnSmoke();
    }

    void Start()
    {
        Debug.Log("Running from Start.");
        StartCoroutine(LoadFlow());
    }

    private IEnumerator LoadFlow()
    {
        float t0 = Time.unscaledTime;

        // Start loading the Start scene
        var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneToLoad);
        op.allowSceneActivation = false;

        // Kick off ALL requests at once (don’t wait per-item)
        var (reqs, keys) = KickAllPreloads();

        // While scene is loading to 0.9, spend up to preloadBudgetSeconds warming assets
        float budget = preloadBudgetSeconds;
        // Wait until async load reaches "ready" (0.9)
        while (op.progress < 0.9f)
        {
            AnimateDots("Loading…");
            yield return null;
        }

        // Ensure minimum visible time overall
        float tStart = Time.unscaledTime;
        while (Time.unscaledTime - tStart < minShowTime)
        {
            AnimateDots("Loading…");
            yield return null;
        }

        // Optional hold(s)
        if (showPressAnyKey || holdWhenReady == HoldMode.PressAnyKey)
        {
            SetText("Press any key to continue");
            while (!Input.anyKeyDown) yield return null;
        }
        else if (holdWhenReady == HoldMode.FixedSeconds && readyHoldSeconds > 0f)
        {
            float t = 0f;
            while (t < readyHoldSeconds) { t += Time.unscaledDeltaTime; yield return null; }
        }

        // (Optional) quick fade out so transition feels smooth
        yield return StartCoroutine(FadeOutUITK(0.25f));

        op.allowSceneActivation = true;
    }
    private IEnumerator FadeOutUITK(float duration)
    {
        var root = uiDocument ? uiDocument.rootVisualElement : null;
        if (root == null || duration <= 0f) yield break;

        float t = 0f;
        float start = root.resolvedStyle.opacity;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(start, 0f, t / duration);
            root.style.opacity = a;
            yield return null;
        }
        root.style.opacity = 0f;
    }

    private IEnumerator PrecacheWeapons(System.Action onItemDone)
    {
        Debug.Log("Starting weapon precache.");
        if (weaponIdsToPrecache == null || weaponIdsToPrecache.Length == 0)
            yield break;

        for (int i = 0; i < weaponIdsToPrecache.Length; i++)
        {
            string id = weaponIdsToPrecache[i];
            string key = $"Models/Weapons/{id}";
            Debug.Log($"[Preload] ({i + 1}/{weaponIdsToPrecache.Length}) {key}");

            // Skip if already cached
            if (Precache.Has(key))
            {
                onItemDone?.Invoke();
                yield return null;
                continue;
            }

            // 1) Do the async request (yield CANNOT be inside try/catch)
            var req = Resources.LoadAsync<GameObject>(key);

            // Optional safety: timeout in case the editor gets weird
            float waited = 0f;
            while (!req.isDone && waited < 10f) // 10s cap
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            // 2) Now guard the logic with try/catch
            try
            {
                var prefab = req.asset as GameObject;

                if (prefab == null)
                {
                    Debug.LogWarning($"[Preload] NOT FOUND: Resources/{key}. " +
                                     $"Check the path & name (must be under Assets/Resources/Models/Weapons/ and no extension).");
                }
                else
                {
                    Precache.Put(key, prefab);
                    Debug.Log($"[Preload] Cached: {key}");
                }

                if (preloadLikelyDeps)
                {
                    // fire-and-forget; no assumptions, no yield here
                    Resources.LoadAsync<Material>($"Materials/{id}");
                    Resources.LoadAsync<Texture2D>($"Textures/{id}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Preload] Cache error on {key}: {ex}");
                // keep going
            }

            onItemDone?.Invoke();
            yield return null; // spread work across frames
        }

        Debug.Log("[Preload] Weapon precache finished.");
    }
    private void AnimateDots(string prefix)
    {
        Debug.Log("Animating dots.");
        int dots = (int)((Time.unscaledTime * 3f) % 4f); // 0..3
        SetText(prefix + new string('.', dots));
    }

    private void SetText(string s)
    {
        if (_loadingLabel != null) _loadingLabel.text = s;
    }

    private void TrySpawnSmoke()
    {
        Debug.Log("Trying to spawn smoke.");
        if (string.IsNullOrWhiteSpace(smokePrefabPath)) return;

        var smoke = Resources.Load<GameObject>(smokePrefabPath);
        if (!smoke) return;

        var cam = Camera.main;
        Vector3 spawnAt = transform.position + smokeOffset;
        if (smokeFollowsCamera && cam)
            spawnAt = cam.transform.position + cam.transform.forward * smokeOffset.z
                      + cam.transform.right * smokeOffset.x
                      + cam.transform.up * smokeOffset.y;

        var inst = Instantiate(smoke, spawnAt, Quaternion.identity);
        inst.name = "PreloadSmoke";
        if (smokeFollowsCamera && cam)
        {
            // Parent to camera so it stays on screen subtly
            inst.transform.SetParent(cam.transform, worldPositionStays: true);
        }
    }
    private (List<ResourceRequest> reqs, List<string> keys) KickAllPreloads()
    {
        var reqs = new List<ResourceRequest>();
        var keys = new List<string>();

        if (weaponIdsToPrecache != null)
        {
            for (int i = 0; i < weaponIdsToPrecache.Length; i++)
            {
                string id = weaponIdsToPrecache[i];
                string key = $"Models/Weapons/{id}";

                if (!Precache.Has(key))
                {
                    var rr = Resources.LoadAsync<GameObject>(key);
                    reqs.Add(rr);
                    keys.Add(key);

                    if (preloadLikelyDeps)
                    {
                        // fire-and-forget; no yield here
                        Resources.LoadAsync<Material>($"Materials/{id}");
                        Resources.LoadAsync<Texture2D>($"Textures/{id}");
                    }
                }
            }
        }
        return (reqs, keys);
    }

    private IEnumerator FinishPreloadsInBackground(List<ResourceRequest> reqs, List<string> keys)
    {
        // Let all outstanding loads complete while the Start scene is already running
        for (int i = 0; i < reqs.Count; i++)
        {
            var rr = reqs[i];
            // wait until this request is done, but don’t lock the frame
            while (!rr.isDone) yield return null;

            var asset = rr.asset as UnityEngine.Object;
            if (asset != null) Precache.Put(keys[i], asset);
            // spread a little to keep frame time smooth
            yield return null;
        }

        // If this object only exists to preload, you can self‑destruct here:
        Destroy(gameObject);
    }
}
// ---------- Safe, minimal cache (inside PreLoad) ----------
public static class Precache
{
    private static readonly Dictionary<string, Object> _cache = new Dictionary<string, Object>();

    public static bool Has(string key) => _cache.ContainsKey(key);

    public static T Get<T>(string key) where T : Object
        => _cache.TryGetValue(key, out var o) ? o as T : null;

    public static void Put(string key, Object obj)
    {
        if (!obj) return;
        _cache[key] = obj; // overwrite safely
    }
}