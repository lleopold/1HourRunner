// BloodHUD.cs
// UI Toolkit blood-on-screen effect for existing UIDocuments.
// Attach to ScriptLogicPlayerSpawnSpot, assign PNGs, call BloodHUD.Instance.Hit(intensity01).

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public class BloodHUD : MonoBehaviour
{
    [Header("Assets")]
    public Texture2D[] Splats;

    [Header("If no UIDocument exists")]
    public PanelSettings FallbackPanelSettings; // optional

    [Header("Timings (seconds)")]
    public float FadeIn = 0.06f;
    public float Hold = 0.05f;
    public float FadeOut = 0.35f;

    [Header("Randomization")]
    public Vector2 ScaleRange = new(0.8f, 1.6f);
    public float EdgePadding = 64f;
    public float AlphaMin = 0.25f, AlphaMax = 0.6f;

    [Header("Pool")]
    public int PoolSize = 12;

    public static BloodHUD Instance { get; private set; }

    UIDocument _uiDoc;
    VisualElement _root;
    readonly Queue<VisualElement> _pool = new();
    readonly Dictionary<VisualElement, List<IVisualElementScheduledItem>> _activeSchedules = new();

    static long Ms(float seconds) => (long)Mathf.Round(seconds * 1000f);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BindToUIDocumentOrCreate();
        EnsureRoot();
        PrewarmPool();
    }

    void BindToUIDocumentOrCreate()
    {
        var docs = FindObjectsOfType<UIDocument>(includeInactive: true);
        _uiDoc = docs.OrderByDescending(d => d.sortingOrder).FirstOrDefault();

        if (_uiDoc != null) return;

        var go = new GameObject("Auto_BloodHUD_UIDocument");
        DontDestroyOnLoad(go);
        _uiDoc = go.AddComponent<UIDocument>();
        _uiDoc.sortingOrder = 9999;
        if (FallbackPanelSettings != null) _uiDoc.panelSettings = FallbackPanelSettings;
    }

    void EnsureRoot()
    {
        _root = _uiDoc.rootVisualElement.Q<VisualElement>("BloodHUD");
        if (_root != null) return;

        _root = new VisualElement { name = "BloodHUD" };
        _root.pickingMode = PickingMode.Ignore;
        _root.style.position = Position.Absolute;
        _root.style.left = 0;
        _root.style.right = 0;
        _root.style.top = 0;
        _root.style.bottom = 0;
        _root.style.opacity = 1f;
        _root.style.visibility = Visibility.Visible;
        _uiDoc.rootVisualElement.Add(_root);
    }

    void PrewarmPool()
    {
        _pool.Clear();
        _activeSchedules.Clear();
        for (int i = 0; i < PoolSize; i++)
        {
            var ve = CreateSplatElement();
            _activeSchedules[ve] = new List<IVisualElementScheduledItem>();
            _pool.Enqueue(ve);
        }
    }

    VisualElement CreateSplatElement()
    {
        var ve = new VisualElement
        {
            pickingMode = PickingMode.Ignore
        };
        ve.style.position = Position.Absolute;
        ve.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
        ve.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        ve.style.opacity = 0f;
        ve.style.visibility = Visibility.Hidden;

        // Animate ONLY opacity
        ve.style.transitionProperty = new List<StylePropertyName>
    {
        new StylePropertyName("opacity")
    };
        ve.style.transitionDuration = new List<TimeValue>
    {
        new TimeValue(FadeIn, TimeUnit.Second)
    };

        _root.Add(ve);
        return ve;
    }


    VisualElement Get()
    {
        if (_pool.Count == 0)
        {
            var ve = CreateSplatElement();
            _activeSchedules[ve] = new List<IVisualElementScheduledItem>();
            return ve;
        }

        var element = _pool.Dequeue();
        element.style.visibility = Visibility.Visible;
        return element;
    }

    void Release(VisualElement ve)
    {
        // Cancel ALL scheduled items for this element
        if (_activeSchedules.ContainsKey(ve))
        {
            foreach (var schedule in _activeSchedules[ve])
            {
                schedule?.Pause();
            }
            _activeSchedules[ve].Clear();
        }

        // Reset all styles to default state
        ve.style.visibility = Visibility.Hidden;
        ve.style.opacity = 0f;
        ve.style.transitionDuration = new List<TimeValue>
        {
            new TimeValue(FadeIn, TimeUnit.Second),
            new TimeValue(FadeIn, TimeUnit.Second)
        };

        _pool.Enqueue(ve);
    }

    public void Hit(float intensity01 = 1f)
    {
        if (_root == null || Splats == null || Splats.Length == 0) return;

        var ve = Get();
        if (!_activeSchedules.ContainsKey(ve)) _activeSchedules[ve] = new();

        ve.style.backgroundImage = new StyleBackground(Splats[Random.Range(0, Splats.Length)]);

        float w = _root.resolvedStyle.width > 0 ? _root.resolvedStyle.width : Screen.width;
        float h = _root.resolvedStyle.height > 0 ? _root.resolvedStyle.height : Screen.height;

        // Temporarily disable transition to avoid animated movement while placing/resizing
        ve.style.transitionDuration = new List<TimeValue> { new TimeValue(0f, TimeUnit.Second) };

        // Position on an edge
        float x, y; int edge = Random.Range(0, 4);
        switch (edge)
        {
            case 0: x = Random.Range(-EdgePadding, EdgePadding * 2); y = Random.Range(0, h); break;
            case 1: x = Random.Range(w - EdgePadding * 2, w + EdgePadding); y = Random.Range(0, h); break;
            case 2: x = Random.Range(0, w); y = Random.Range(-EdgePadding, EdgePadding * 2); break;
            default: x = Random.Range(0, w); y = Random.Range(h - EdgePadding * 2, h + EdgePadding); break;
        }
        ve.style.left = x;
        ve.style.top = y;

        float baseSize = Mathf.Min(w, h) * 0.35f;
        float scale = Random.Range(ScaleRange.x, ScaleRange.y) * Mathf.Lerp(0.8f, 1.4f, Mathf.Clamp01(intensity01));
        float size = Mathf.Clamp(baseSize * scale, 96f, 800f);
        ve.style.width = size;
        ve.style.height = size;

        // Re-enable opacity transition and animate only opacity
        ve.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("opacity") };
        ve.style.transitionDuration = new List<TimeValue> { new TimeValue(FadeIn, TimeUnit.Second) };

        float targetA = Mathf.Clamp01(Random.Range(AlphaMin, AlphaMax) * Mathf.Max(0.15f, intensity01));
        ve.style.opacity = 0f;

        var s1 = ve.schedule.Execute(() =>
        {
            ve.style.opacity = targetA;

            var s2 = ve.schedule.Execute(() =>
            {
                ve.style.transitionDuration = new List<TimeValue> { new TimeValue(FadeOut, TimeUnit.Second) };
                ve.style.opacity = 0f;

                var s3 = ve.schedule.Execute(() => Release(ve)).StartingIn(Ms(FadeOut));
                _activeSchedules[ve].Add(s3);
            }).StartingIn(Ms(Hold));
            _activeSchedules[ve].Add(s2);
        }).StartingIn(0);
        _activeSchedules[ve].Add(s1);
    }

}
