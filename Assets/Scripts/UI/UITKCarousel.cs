using MoreMountains.Feedbacks;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.StandaloneInputModule;

public class UITKCarousel : MonoBehaviour
{
    [Header("UI Query")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string containerName = "ws_tabs";   // VisualElement that holds Button items

    [Header("Layout")]
    [SerializeField, Tooltip("Horizontal spacing between items (px).")]
    private float itemSpacing = 61.7f;

    [SerializeField, Tooltip("Max side items visible per side.")]
    private int maxSideItems = 2;

    [SerializeField, Tooltip("Y-axis rotation per step (degrees).")]
    private float yAnglePerStep = 10.6f;

    [SerializeField, Tooltip("Side item scale.")]
    private float sideScale = 0.62f;

    [SerializeField, Tooltip("Vertical offset for side items (px).")]
    private float sideYOffset = 0f;

    [SerializeField, Tooltip("Side item opacity (0..1).")]
    private float sideOpacity = 0.07f;

    [SerializeField, Tooltip("Hide items beyond maxSideItems instead of just fading.")]
    private bool hideBeyondMax = true;

    [Header("Animation")]
    [SerializeField] private bool animate = true;
    [SerializeField, Tooltip("Tween duration (s).")]
    private float tweenTime = 0.15f;

    [Header("Input")]
    [SerializeField] private KeyCode prevKey = KeyCode.Q;  // map to LB if desired
    [SerializeField] private KeyCode nextKey = KeyCode.E;  // map to RB if desired
    [SerializeField] private bool useControllerLB_RB = true; // LB=JoystickButton4, RB=JoystickButton5
    public enum InputMode { NavigateItems, SwitchCategory }

    [Header("Input Mode")]
    [SerializeField] private InputMode keysDo = InputMode.SwitchCategory;
    // Set to NavigateItems if you want keys to move within the current list.

    // Pulse settings for the active UITK tab
    [Header("Pulse (UITK)")]
    [SerializeField] private float pulseAmplitude = 1.28f;   // +8% scale at peak
    [SerializeField] private float pulseDuration = 1.10f;   // seconds
    [SerializeField]
    private AnimationCurve pulseCurve =
        new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 8f),   // start
            new Keyframe(0.18f, 1f, 0f, 0f),   // peak +100% amplitude
            new Keyframe(0.40f, 0.25f, 0f, 0f),  // settle
            new Keyframe(1f, 0f, 0f, 0f)    // back to baseline
        );
    private float _pulseTime = -1f; // <0 = inactive
    // Internal state
    private VisualElement container;
    private readonly List<Button> items = new List<Button>();

    private int centerIndex = 0;   // discrete index 0..n-1
    private float centerPos = 0f;  // continuous logical position (wrap-aware)
    private float animFromPos, animToPos;
    private float animT = 1f;

    // Pixel center of the container (updated on geometry change)
    private float containerCenterX = 0f;
    [Header("Feel / MMF Players")]
    [SerializeField] private MMF_Player onSelectFX;
    [SerializeField] private MMF_Player onMoveLeftFX;
    [SerializeField] private MMF_Player onMoveRightFX;
    [SerializeField] private MMF_Player onWrapFX;

    private int lastCenterIndex = -1; // to detect selection change
    private bool _selectFxQueued = false;

    public event Action PrevCategoryRequested;
    public event Action NextCategoryRequested;

    [SerializeField] public bool handleInput = true; // default off


    void Awake()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("UITKCarousel: UIDocument not found.");
            enabled = false; return;
        }

        var root = uiDocument.rootVisualElement;

        // 1) query by name
        var named = string.IsNullOrEmpty(containerName)
            ? root
            : root.Q<VisualElement>(containerName);

        if (named == null)
        {
            Debug.LogError($"UITKCarousel: Container '{containerName}' not found.");
            enabled = false; return;
        }

        // 2) if it's a ScrollView, use its contentContainer
        container = ResolveContainer(named);

        // 3) required styling for absolute-positioned buttons
        container.style.overflow = Overflow.Visible;
        container.style.position = Position.Relative;

        // We need to see partial neighbors
        container.style.overflow = Overflow.Visible;
        // Ensure container is the positioning context for absolute children
        container.style.position = Position.Relative;

        // Track size so we can center in pixels
        container.RegisterCallback<GeometryChangedEvent>(e =>
        {
            containerCenterX = container.resolvedStyle.width * 0.5f;
            ApplyLayout(1f);
        });

        // Collect buttons and force absolute positioning so Flex doesn't move them
        items.Clear();
        container.Query<Button>().ForEach(b =>
        {
            items.Add(b);
            b.RegisterCallback<ClickEvent>(_ => SnapTo(items.IndexOf(b)));

            // Make transforms pivot around the center
            b.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50), 0);

            // Take them out of Flex flow so we control exact placement
            b.style.position = Position.Absolute;
            b.style.left = 0;
            b.style.top = 0;
        });

        centerIndex = Mathf.Clamp(centerIndex, 0, Mathf.Max(0, items.Count - 1));
        centerPos = centerIndex;
        animFromPos = animToPos = centerPos;

        // Initial center measurement (in case geometry event hasn’t fired yet)
        containerCenterX = container.resolvedStyle.width > 0f
            ? container.resolvedStyle.width * 0.5f
            : 0f;

        ApplyLayout(1f);
    }


    void Update()
    {
        if (handleInput)
        {
            HandleInput();
        }

        //HandleInput();

        // advance pulse timer FIRST
        bool pulseActive = false;
        if (_pulseTime >= 0f)
        {
            _pulseTime += Time.deltaTime;
            if (_pulseTime >= pulseDuration) _pulseTime = -1f;
            else pulseActive = true;
        }

        // tween branch
        if (animate && animT < 1f)
        {
            animT = Mathf.Min(1f, animT + Time.deltaTime / Mathf.Max(0.0001f, tweenTime));
            ApplyLayout(EaseOutCubic(animT));

            // when tween finishes, fire FX ONCE if queued
            if (animT >= 1f && _selectFxQueued)
            {
                _selectFxQueued = false;
                onSelectFX?.PlayFeedbacks();
            }
            return;
        }

        // no tween: still refresh during pulse
        if (pulseActive)
        {
            ApplyLayout(1f);
        }
    }
    private void HandleInput()
    {
        bool prevPressed = Input.GetKeyDown(prevKey) || (useControllerLB_RB && Input.GetKeyDown(KeyCode.JoystickButton4));
        bool nextPressed = Input.GetKeyDown(nextKey) || (useControllerLB_RB && Input.GetKeyDown(KeyCode.JoystickButton5));

        if (!prevPressed && !nextPressed) return;

        if (keysDo == InputMode.NavigateItems)
        {
            if (prevPressed) FocusPrev();
            if (nextPressed) FocusNext();
        }
        else // SwitchCategory
        {
            if (prevPressed) PrevCategoryRequested?.Invoke();
            if (nextPressed) NextCategoryRequested?.Invoke();
        }
    }



    public void FocusPrev() => SnapBy(-1);
    public void FocusNext() => SnapBy(+1);

    private void SnapBy(int step)
    {
        if (items.Count == 0) return;
        int targetIndex = Mod(centerIndex + step, items.Count);
        StartTweenTo(targetIndex, step);
    }

    public void SnapTo(int targetIndex)
    {
        if (items.Count == 0) return;
        targetIndex = Mod(targetIndex, items.Count);

        int step = ShortestSignedStep(centerIndex, targetIndex, items.Count);
        StartTweenTo(targetIndex, step);
    }

    private void StartTweenTo(int targetIndex, int signedStep)
    {
        animFromPos = centerPos;
        animToPos = centerPos + signedStep;
        centerPos = animToPos;
        centerIndex = targetIndex;

        // queue FX to play once when tween ends
        _selectFxQueued = true;

        animT = animate ? 0f : 1f;
        ApplyLayout(animT);

        // if no animation, play immediately
        if (!animate && _selectFxQueued)
        {
            _selectFxQueued = false;
            onSelectFX?.PlayFeedbacks();
        }
    }
    private void ApplyLayout(float t)
    {
        if (items.Count == 0) return;

        float logicalCenter = Mathf.Lerp(animFromPos, animToPos, t);
        if (containerCenterX <= 0f && container.resolvedStyle.width > 0f)
            containerCenterX = container.resolvedStyle.width * 0.5f;

        for (int i = 0; i < items.Count; i++)
        {
            var btn = items[i];
            float offset = CircularDelta(i, logicalCenter, items.Count);

            bool beyond = Mathf.Abs(offset) > maxSideItems + 0.999f;
            bool isActive = Mathf.Abs(offset) < 0.25f;   // tighter threshold → only 1 active

            btn.EnableInClassList("is-active", isActive);
            btn.EnableInClassList("is-side", !isActive);
            btn.EnableInClassList("is-hidden", hideBeyondMax && beyond);

            if (hideBeyondMax && beyond) { btn.style.visibility = Visibility.Hidden; continue; }
            btn.style.visibility = Visibility.Visible;

            float x = containerCenterX + offset * itemSpacing;
            float y = Mathf.Sign(offset) != 0
                ? sideYOffset * Mathf.InverseLerp(0f, maxSideItems, Mathf.Abs(offset))
                : 0f;

            float yAngle = -yAnglePerStep * offset;
            float s = Mathf.Lerp(1f, sideScale, Mathf.InverseLerp(0f, maxSideItems, Mathf.Abs(offset)));
            float alpha = Mathf.Lerp(1f, sideOpacity, Mathf.InverseLerp(0f, maxSideItems, Mathf.Abs(offset)));

            // pulse on the single active tab
            if (isActive && _pulseTime >= 0f && pulseDuration > 0f)
            {
                float tNorm = Mathf.Clamp01(_pulseTime / pulseDuration);
                float extra = 1f + pulseAmplitude * pulseCurve.Evaluate(tNorm);
                s *= extra;
                //Debug.Log(extra);
            }

            if (isActive)
            {
                s = 1.6f; // 160% — ako ovo ne vidiš, nešto drugo override-uje stil
            }

            btn.style.left = 0;
            btn.style.top = 0;
            btn.style.translate = new Translate(x, y, 0);
            btn.style.rotate = new Rotate(new Angle(yAngle, AngleUnit.Degree));
            btn.style.scale = new Scale(new Vector2(s, s));
            btn.style.opacity = alpha;
        }

        // stacking unchanged
        var sorted = new List<Button>(items);
        sorted.Sort((a, b) =>
        {
            float aDist = Mathf.Abs(CircularDelta(items.IndexOf(a), logicalCenter, items.Count));
            float bDist = Mathf.Abs(CircularDelta(items.IndexOf(b), logicalCenter, items.Count));
            return bDist.CompareTo(aDist);
        });
        foreach (var btn in sorted) btn.BringToFront();

        // IMPORTANT: remove any onSelectFX.PlayFeedbacks() from here
    }

    private static int Mod(int x, int m) => (x % m + m) % m;

    private static int ShortestSignedStep(int from, int to, int n)
    {
        int raw = to - from;
        int wrapped = ((raw % n) + n) % n;               // 0..n-1
        return wrapped <= n / 2 ? wrapped : wrapped - n; // shortest [-floor(n/2), ceil(n/2)]
    }

    private static float CircularDelta(int index, float center, int n)
    {
        float d = index - center;
        d = Mathf.Repeat(d + n * 0.5f, n) - n * 0.5f;
        return d;
    }

    private static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);

    public void RequeryButtons()
    {
        if (container == null) return;

        items.Clear();
        container.Query<Button>().ForEach(b =>
        {
            items.Add(b);
            b.RegisterCallback<ClickEvent>(_ => SnapTo(items.IndexOf(b)));
            b.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50), 0);
            b.style.position = Position.Absolute;
            b.style.left = 0; b.style.top = 0;
        });

        centerIndex = Mathf.Clamp(centerIndex, 0, Mathf.Max(0, items.Count - 1));
        centerPos = centerIndex;
        animFromPos = animToPos = centerPos;

        containerCenterX = container.resolvedStyle.width * 0.5f;
        ApplyLayout(1f);
    }
    // Called by Feel (MMFEvents) to pulse the current active tab
    public void PulseActiveTab()
    {
        _pulseTime = 0f;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Live updates when tweaking values in the Inspector during Play Mode
        if (Application.isPlaying && isActiveAndEnabled)
        {
            ApplyLayout(animT >= 1f ? 1f : EaseOutCubic(animT));
        }
    }
#endif
    private static VisualElement ResolveContainer(VisualElement ve)
    {
        if (ve is ScrollView sv)
            return sv.contentContainer;
        return ve;
    }
}
