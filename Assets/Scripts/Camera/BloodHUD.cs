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
        for (int i = 0; i < PoolSize; i++)
            _pool.Enqueue(CreateSplatElement());
    }

    VisualElement CreateSplatElement()
    {
        var ve = new VisualElement();
        ve.pickingMode = PickingMode.Ignore;
        ve.style.position = Position.Absolute;
        ve.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
        ve.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        ve.style.opacity = 0f;
        ve.style.visibility = Visibility.Hidden;
        ve.style.transitionDuration = new List<TimeValue>
        {
            new TimeValue(FadeIn, TimeUnit.Second),
            new TimeValue(FadeIn, TimeUnit.Second)
        };
        _root.Add(ve);
        return ve;
    }

    VisualElement Get()
    {
        if (_pool.Count == 0) _pool.Enqueue(CreateSplatElement());
        var ve = _pool.Dequeue();
        ve.style.visibility = Visibility.Visible;
        return ve;
    }

    void Release(VisualElement ve)
    {
        ve.style.visibility = Visibility.Hidden;
        ve.style.opacity = 0f;
        _pool.Enqueue(ve);
    }

    public void Hit(float intensity01 = 1f)
    {
        if (_root == null || Splats == null || Splats.Length == 0)
        {
            Debug.LogWarning("BloodHUD: Cannot Hit() because no root or splats assigned.");
            return;
        }

        var ve = Get();

        var tex = Splats[Random.Range(0, Splats.Length)];
        ve.style.backgroundImage = new StyleBackground(tex);

        float w = _root.resolvedStyle.width;
        if (w <= 0) w = Screen.width;
        float h = _root.resolvedStyle.height;
        if (h <= 0) h = Screen.height;

        float x = Random.Range(EdgePadding, Mathf.Max(EdgePadding, w - EdgePadding));
        float y = Random.Range(EdgePadding, Mathf.Max(EdgePadding, h - EdgePadding));
        ve.style.left = x;
        ve.style.top = y;

        float baseSize = Mathf.Min(w, h) * 0.35f;
        float scale = Random.Range(ScaleRange.x, ScaleRange.y) *
                      Mathf.Lerp(0.8f, 1.4f, Mathf.Clamp01(intensity01));
        float size = Mathf.Clamp(baseSize * scale, 96f, 800f);
        ve.style.width = size;
        ve.style.height = size;

        // fixed rotation line for all Unity versions
        float deg = Random.Range(0f, 360f);
        ve.style.rotate = new UnityEngine.UIElements.Rotate(new Angle(deg, AngleUnit.Degree));

        float targetA = Mathf.Clamp01(Random.Range(AlphaMin, AlphaMax) *
                                      Mathf.Max(0.15f, intensity01));

        ve.style.transitionDuration = new List<TimeValue>
        {
            new TimeValue(FadeIn, TimeUnit.Second),
            new TimeValue(FadeIn, TimeUnit.Second)
        };
        ve.style.opacity = 0f;

        ve.schedule.Execute(() =>
        {
            ve.style.opacity = targetA;

            ve.schedule.Execute(() =>
            {
                ve.style.transitionDuration = new List<TimeValue>
                {
                    new TimeValue(FadeOut, TimeUnit.Second),
                    new TimeValue(FadeOut, TimeUnit.Second)
                };
                ve.style.opacity = 0f;
                ve.schedule.Execute(() => Release(ve)).StartingIn(Ms(FadeOut));
            }).StartingIn(Ms(Hold));

        }).StartingIn(0L);
    }
}
