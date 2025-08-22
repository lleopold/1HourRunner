using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class WeaponsCarouselController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDoc;
    [SerializeField] private UITKCarousel carousel;
    [SerializeField] private string containerName = "ws_tabs";

    [Header("Input")]
    [SerializeField] private KeyCode prevKey = KeyCode.Q;
    [SerializeField] private KeyCode nextKey = KeyCode.E;
    [SerializeField] private bool useControllerLB_RB = true;

    // Paths under Resources/
    private const string ThumbsRoot = "Thumbnails/Weapons"; // e.g. Resources/Thumbnails/Weapons/M9.png
    private const string ModelsRoot = "Models/Weapons";     // e.g. Resources/Models/Weapons/WPN_AP85

    private VisualElement _container;      // ws_weapon_list (ScrollView content)
    private VisualElement _preview;        // ws_preview panel (we add an Image inside)
    private Image _previewImage;           // child Image that displays the RenderTexture

    // Preview rig
    private GameObject _rig;               // parent object
    private Camera _cam;
    private Light _keyLight;
    private GameObject _spawned;           // current weapon instance
    private RenderTexture _rt;

    // Preview settings
    [Header("Preview")]
    [SerializeField] private Vector2Int renderSize = new Vector2Int(1024, 1024);
    [SerializeField] private float cameraFov = 30f;
    [SerializeField] private Color cameraBg = new Color(0.05f, 0.05f, 0.08f, 1f);
    [SerializeField] private float fitPadding = 1.25f;  // zoom out factor
    [SerializeField] private float rotateSpeed = 0.35f; // degrees per pixel
    [SerializeField] private float zoomSpeed = 0.1f;    // scroll wheel scaling
    [SerializeField] private float minZoom = 0.6f;
    [SerializeField] private float maxZoom = 1.8f;

    private float _zoomFactor = 1f;
    private bool _dragging;
    private Vector3 _lastPointer;

    // ----- Data (all in one file) -----
    public enum Cat { Pistols, SMG, Rifles, Shotguns, Snipers }
    private static readonly Cat[] Order = { Cat.Pistols, Cat.SMG, Cat.Rifles, Cat.Shotguns, Cat.Snipers };

    private struct WeaponDef
    {
        public string id;      // internal/model id (also prefab name)
        public string display; // button label
        public WeaponDef(string id, string display) { this.id = id; this.display = display; }
    }

    private static readonly Dictionary<Cat, (string slotText, List<WeaponDef> list)> WeaponsByCat =
        new()
        {
            { Cat.Pistols, ("PISTOL", new List<WeaponDef>{
                new("WPN_AP85","AP85"),
                new("WPN_C1911","C1911"),
                new("WPN_Eder22","Eder22"),
                new("WPN_M9","M9"),
                new("WPN_P350","P350"),
                new("WPN_PT8","PT8"),
                new("WPN_Revolver","Revolver"),
            })},
            { Cat.SMG, ("SMG", new List<WeaponDef>{
                new("WPN_SMG5","SMG5"),
                new("WPN_R90","R90"),
                new("WPN_KM4","KM4"),
            })},
            { Cat.Rifles, ("RIFLE", new List<WeaponDef>{
                new("WPN_KM4","KM4"),
                new("WPN_CV47","CV47"),
                new("WPN_CX8","CX8"),
                new("WPN_M16","M16"),
                new("WPN_MK18","MK18"),
            })},
            { Cat.Shotguns, ("SHOTGUN", new List<WeaponDef>{
                new("WPN_FBS","FBS"),
                new("WPN_DT22","DT22"),
                new("WPN_590A1","590A1"),
                new("WPN_M4","M4"),
            })},
            { Cat.Snipers, ("SNIPER", new List<WeaponDef>{
                new("WPN_Hunter85","Hunter85"),
            })},
        };

    private readonly List<WeaponButton> _itemButtons = new();
    private WeaponButton _selectedBtn;
    private WeaponDef _selectedWeapon;
    private Cat _current = Cat.Pistols;

    void Awake()
    {
        if (!uiDoc) uiDoc = GetComponent<UIDocument>() ?? GetComponentInParent<UIDocument>();
        if (!carousel) carousel = GetComponent<UITKCarousel>() ?? GetComponentInParent<UITKCarousel>();

        if (!uiDoc) { Debug.LogError("WeaponsCarouselController: UIDocument not found."); enabled = false; return; }
        if (!carousel) { Debug.LogError("WeaponsCarouselController: UITKCarousel not found."); enabled = false; return; }

        var root = uiDoc.rootVisualElement;

        // Container for item buttons
        var found = root.Q<VisualElement>("ws_weapon_list");
        _container = found is ScrollView sv ? sv.contentContainer : found;

        // Big preview panel (we’ll add an Image inside so we can bind a RenderTexture)
        _preview = root.Q<VisualElement>("ws_preview");
        if (_preview == null)
        {
            Debug.LogError("WeaponsCarouselController: 'ws_preview' not found in UXML.");
            enabled = false; return;
        }

        EnsurePreviewImage();

        // Tabs carousel events
        var named = root.Q<VisualElement>(containerName);
        if (named == null)
        {
            Debug.LogError($"WeaponsCarouselController: container '{containerName}' not found in UXML.");
            enabled = false; return;
        }
        carousel.PrevCategoryRequested += () => ChangeCategory(-1);
        carousel.NextCategoryRequested += () => ChangeCategory(+1);

        // Build preview rig now
        BuildPreviewRig();
    }

    void OnEnable()
    {
        var root = uiDoc.rootVisualElement;
        root.RegisterCallback<GeometryChangedEvent>(OnRootReady);
    }

    private void OnDisable()
    {
        var root = uiDoc?.rootVisualElement;
        if (root != null) root.UnregisterCallback<GeometryChangedEvent>(OnRootReady);
    }

    private void OnDestroy()
    {
        if (_spawned) Destroy(_spawned);
        if (_rig) Destroy(_rig);
        if (_rt) { _rt.Release(); Destroy(_rt); }
    }

    private void OnRootReady(GeometryChangedEvent _)
    {
        var root = uiDoc.rootVisualElement;
        root.UnregisterCallback<GeometryChangedEvent>(OnRootReady);

        BindTabs(root);
        HookCarouselCategoryEvents();
        ShowCategory(_current);

        HookPreviewPointerEvents();
    }

    // ---------- Tabs & list ----------
    private void ChangeCategory(int step)
    {
        int idx = System.Array.IndexOf(Order, _current);
        if (idx < 0) idx = 0;
        idx = (idx + step + Order.Length) % Order.Length;
        ShowCategory(Order[idx]);
    }

    public void ShowCategory(Cat cat)
    {
        _current = cat;

        int tabIndex = System.Array.IndexOf(Order, cat);
        if (tabIndex >= 0) carousel?.SnapTo(tabIndex);

        if (!WeaponsByCat.TryGetValue(cat, out var pack))
        {
            Debug.LogWarning($"No data for {cat}");
            return;
        }

        string slotText = pack.slotText;
        var list = pack.list;

        _container.Clear();
        _itemButtons.Clear();
        _selectedBtn = null;

        foreach (var w in list)
        {
            var btn = BuildWeaponButton(w, slotText);
            _itemButtons.Add(btn);
            _container.Add(btn);
        }

        if (_container?.parent is ScrollView scrollView)
            scrollView.scrollOffset = Vector2.zero;

        if (list.Count > 0)
            SelectWeapon(list[0], _itemButtons[0]);
    }

    private void BindTabs(VisualElement root)
    {
        var tabsRow = root.Q<VisualElement>("ws_tabs");
        if (tabsRow == null) { Debug.LogError("Tabs row 'ws_tabs' not found."); return; }

        var map = new Dictionary<string, Cat>
        {
            { "tab_pistols",  Cat.Pistols  },
            { "tab_smgs",     Cat.SMG      },
            { "tab_rifles",   Cat.Rifles   },
            { "tab_shotguns", Cat.Shotguns },
            { "tab_snipers",  Cat.Snipers  },
        };

        foreach (var kv in map)
        {
            var tab = tabsRow.Q<Button>(kv.Key);
            if (tab == null) { Debug.LogWarning($"Tab '{kv.Key}' not found in UXML."); continue; }
            var cat = kv.Value;
            tab.focusable = false; // not clickable as per your design
            tab.clicked += () => ShowCategory(cat);
        }
    }

    private void HookCarouselCategoryEvents()
    {
        carousel.PrevCategoryRequested += () => ChangeCategory(-1);
        carousel.NextCategoryRequested += () => ChangeCategory(+1);
    }

    private WeaponButton BuildWeaponButton(WeaponDef w, string slotText)
    {
        var wb = new WeaponButton();
        wb.name = $"btn_{w.id}";
        wb.focusable = false;

        var tex = Resources.Load<Texture2D>($"{ThumbsRoot}/{w.id}")
               ?? Resources.Load<Texture2D>($"{ThumbsRoot}/{w.display}");

        wb.SetData(tex, slotText, w.display, topRight: "GUNSMITH", bottomRight: "LVL 1");
        wb.Clicked += _ => SelectWeapon(w, wb);

        return wb;
    }

    private void SelectWeapon(WeaponDef w, WeaponButton wb)
    {
        if (_selectedBtn != null) _selectedBtn.RemoveFromClassList("is-active");
        _selectedBtn = wb;
        _selectedWeapon = w;
        _selectedBtn.AddToClassList("is-active");

        UpdatePreview_3D(w); // swap from thumbnails to real 3D
    }

    // ---------- Preview image holder (UI Toolkit) ----------
    private void EnsurePreviewImage()
    {
        // Make sure ws_preview has an Image child that fills it
        _previewImage = _preview.Q<Image>("rt_preview_image");
        if (_previewImage == null)
        {
            _previewImage = new Image { name = "rt_preview_image" };
            _preview.Add(_previewImage);
        }
        _previewImage.style.position = Position.Absolute;
        _previewImage.style.left = 0;
        _previewImage.style.top = 0;
        _previewImage.style.right = 0;
        _previewImage.style.bottom = 0;
        _previewImage.scaleMode = ScaleMode.ScaleToFit;

        // Remove any background tint so the RT is shown correctly
        _preview.style.backgroundImage = StyleKeyword.None;
        _preview.style.unityBackgroundImageTintColor = Color.white;
    }

    private void HookPreviewPointerEvents()
    {
        // Drag to rotate
        _preview.RegisterCallback<PointerDownEvent>(e =>
        {
            _dragging = true;
            _lastPointer = e.position; // Vector2
            _preview.CapturePointer(e.pointerId);
        });

        _preview.RegisterCallback<PointerMoveEvent>(e =>
        {
            if (!_dragging || _spawned == null) return;
            Vector2 delta = e.position - _lastPointer; // Vector2 - Vector2 ✓
            _lastPointer = e.position;

            // yaw around up axis
            _spawned.transform.Rotate(Vector3.up, -delta.x * rotateSpeed, Space.World);
            // pitch: clamp a bit to avoid flipping
            _spawned.transform.Rotate(Vector3.right, delta.y * rotateSpeed, Space.Self);
        });

        _preview.RegisterCallback<PointerUpEvent>(e =>
        {
            _dragging = false;
            _preview.ReleasePointer(e.pointerId);
        });

        _preview.RegisterCallback<PointerLeaveEvent>(_ =>
        {
            _dragging = false;
        });

        // Scroll to zoom
        _preview.RegisterCallback<WheelEvent>(e =>
        {
            float dir = Mathf.Sign(e.delta.y); // up is positive on most mice
            _zoomFactor = Mathf.Clamp(_zoomFactor + dir * zoomSpeed, minZoom, maxZoom);
            PositionCameraForCurrentModel();
        });
    }

    // ---------- 3D preview rig ----------
    private void BuildPreviewRig()
    {
        // RenderTexture
        _rt = new RenderTexture(renderSize.x, renderSize.y, 24, RenderTextureFormat.ARGB32)
        {
            name = "WeaponPreviewRT",
            useMipMap = false,
            autoGenerateMips = false
        };
        _rt.Create();

        // World objects
        _rig = new GameObject("UI3DPreviewRig");
        _rig.hideFlags = HideFlags.DontSave;

        var camGO = new GameObject("PreviewCamera");
        camGO.transform.SetParent(_rig.transform, false);
        _cam = camGO.AddComponent<Camera>();
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = new Color(0, 0, 0, 0); //cameraBg;
        RenderSettings.skybox = null;
        //_cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // alpha = 0 (transparent)
        _cam.fieldOfView = cameraFov;
        _cam.nearClipPlane = 0.05f;
        _cam.farClipPlane = 100f;
        _cam.targetTexture = _rt;
        _cam.transform.position = new Vector3(0, 0, 5);
        _cam.transform.rotation = Quaternion.identity;



        var lightGO = new GameObject("KeyLight");
        lightGO.transform.SetParent(_rig.transform, false);
        _keyLight = lightGO.AddComponent<Light>();
        _keyLight.type = LightType.Directional;
        _keyLight.color = Color.white;
        _keyLight.intensity = 1.3f;
        lightGO.transform.rotation = Quaternion.Euler(35f, 140f, 0f);

        // Optional fill light for nicer metals
        var fillGO = new GameObject("FillLight");
        fillGO.transform.SetParent(_rig.transform, false);
        var fillLight = fillGO.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.intensity = 0.6f;
        fillGO.transform.rotation = Quaternion.Euler(15f, -30f, 0f);

        // Bind to UI
        _previewImage.image = _rt; // Image supports Texture, including RenderTexture
        _preview.style.unityBackgroundImageTintColor = Color.white;
        _preview.style.backgroundColor = StyleKeyword.Null; //igors
        _previewImage.style.opacity = 1f;
    }

    private void UpdatePreview_3D(WeaponDef w)
    {
        // Destroy last
        if (_spawned) { Destroy(_spawned); _spawned = null; }

        // Try id first (your prefabs are named like WPN_AP85), then display as fallback
        GameObject prefab =
            Resources.Load<GameObject>($"{ModelsRoot}/{w.id}") ??
            Resources.Load<GameObject>($"{ModelsRoot}/{w.display}");

        if (!prefab)
        {
            Debug.LogWarning($"Prefab not found in Resources/{ModelsRoot}: '{w.id}' or '{w.display}'.");
            // still clear the preview RT (camera will just render background)
            return;
        }

        _spawned = Instantiate(prefab, _rig.transform);
        _spawned.name = $"PREVIEW_{w.id}";
        _spawned.transform.localPosition = Vector3.zero;
        _spawned.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        _spawned.transform.localScale = Vector3.one;

        // Ensure renderers are enabled (in case prefab is disabled)
        foreach (var r in _spawned.GetComponentsInChildren<Renderer>(true))
            r.enabled = true;

        // Reset zoom and fit camera
        _zoomFactor = 1f;
        PositionCameraForCurrentModel();
    }

    private void PositionCameraForCurrentModel()
    {
        if (_spawned == null || _cam == null) return;

        // Bounds of the model
        var renderers = _spawned.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        // Move model to origin for stable rotation feel
        Vector3 offset = b.center;
        _spawned.transform.position -= offset;
        b.center -= offset;

        float radius = b.extents.magnitude;
        radius = Mathf.Max(0.001f, radius);

        // Distance based on FOV so the whole object fits
        float fovRad = _cam.fieldOfView * Mathf.Deg2Rad;
        float dist = (radius * fitPadding * _zoomFactor) / Mathf.Tan(fovRad * 0.5f);

        _cam.transform.position = new Vector3(0f, 0f, dist);
        _cam.transform.LookAt(Vector3.zero, Vector3.up);
    }

    // ---------- Keyboard category switch fallback ----------
    void Update()
    {
        bool prev = Input.GetKeyDown(prevKey) || (useControllerLB_RB && Input.GetKeyDown(KeyCode.JoystickButton4));
        bool next = Input.GetKeyDown(nextKey) || (useControllerLB_RB && Input.GetKeyDown(KeyCode.JoystickButton5));
        if (prev) ChangeCategory(-1);
        if (next) ChangeCategory(+1);
    }
}
