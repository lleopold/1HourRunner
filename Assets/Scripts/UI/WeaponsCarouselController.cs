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

    private const string UnlockedWeapon = "WPN_PT8";

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

    private float _zoomFactor = 0.6f;
    private bool _dragging;
    private Vector3 _lastPointer;

    // Idle showcase spin: weapon rotates around Y until user grabs it
    [SerializeField] private float autoSpinSpeed = 72f; // deg/sec -> full circle in 5s
    private bool _autoSpin = true;

    // Stat FloatFields (optional � all queried by name, null if not in UXML)
    private FloatField _fDamage, _fFireRate, _fDamageFluct, _fClip,
                       _fReload, _fSimul, _fCrit, _fStagger, _fRecoil,
                       _fAccuracy, _fWeight, _fOptimalRange, _fMaxRange;

    // Bar-fill elements � parallel array to _statBarFillNames
    private VisualElement[] _statBarFills;

    private static readonly string[] _statBarFillNames =
    {
        "bar_fill_damage",
        "bar_fill_fire_rate",
        "bar_fill_damage_fluctuation",
        "bar_fill_clip_size",
        "bar_fill_reload_time",
        "bar_fill_simultanious_bullets",
        "bar_fill_critical_chance",
        "bar_fill_stagger",
        "bar_fill_recoil",
        "bar_fill_accuracy",
        "bar_fill_weight",
        "bar_fill_optimal_range",
        "bar_fill_max_range",
    };

    // Optional: external observers
    public static event System.Action<WeaponEnum> WeaponChanged;

    public enum Cat { Pistols, SMG, Rifles, Shotguns, Snipers }
    private static readonly Cat[] Order = { Cat.Pistols, Cat.SMG, Cat.Rifles, Cat.Shotguns, Cat.Snipers };

    private struct WeaponDef
    {
        public string id;
        public string display;
        public WeaponDef(string id, string display) { this.id = id; this.display = display; }
    }

    private static readonly Dictionary<Cat, (string slotText, List<WeaponDef> list)> WeaponsByCat =
        new()
        {
            { Cat.Pistols, ("PISTOL", new List<WeaponDef>{
                new("WPN_PT8","PT8"),
                new("WPN_AP85","AP85"),
                new("WPN_C1911","C1911"),
                new("WPN_Eder22","Eder22"),
                new("WPN_M9","M9"),
                new("WPN_P350","P350"),
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

    private Button _btnChooseLevel;
    private Button _activeTab;
    private float _tabPulseTime;

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

        // Big preview panel (we�ll add an Image inside so we can bind a RenderTexture)
        _preview = root.Q<VisualElement>("ws_preview");
        if (_preview == null)
        {
            Debug.LogError("WeaponsCarouselController: 'ws_preview' not found in UXML.");
            enabled = false; return;
        }

        QueryStatFields(root);
        EnsurePreviewImage();

        // Tabs carousel events � registered once in HookCarouselCategoryEvents (called from OnRootReady)
        var named = root.Q<VisualElement>(containerName);
        if (named == null)
        {
            Debug.LogError($"WeaponsCarouselController: container '{containerName}' not found in UXML.");
            enabled = false; return;
        }

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
        if (carousel != null)
        {
            carousel.PrevCategoryRequested -= OnPrevCategory;
            carousel.NextCategoryRequested -= OnNextCategory;
        }
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

        // Hook "Choose Level" here
        _btnChooseLevel = root.Q<Button>("btn_choose_level");
        if (_btnChooseLevel != null)
        {
            _btnChooseLevel.clicked += () => Loader.Load(Loader.Scene.ChooseLevel);
        }
        else
        {
            Debug.LogWarning("WeaponsCarouselController: Button 'btn_choose_level' not found in UXML.");
        }

        var btnBack = root.Q<Button>("btn_back");
        if (btnBack != null)
            btnBack.clicked += () => UnityEngine.SceneManagement.SceneManager.LoadScene("ChoosePlayer");

        BindTabs(root);
        HookCarouselCategoryEvents();
        ShowCategory(_current);

        HookPreviewPointerEvents();

        // Resize RT once layout is known, then again if the panel resizes
        _preview.RegisterCallback<GeometryChangedEvent>(OnPreviewResized);
        ResizeRTToPreview(); // in case geometry is already resolved
    }

    private void QueryStatFields(VisualElement root)
    {
        // Safe: if the fields aren�t on this screen, all stay null and we skip updates.
        _fDamage = root.Q<FloatField>("fl_damage");
        _fFireRate = root.Q<FloatField>("fl_fire_rate");
        _fDamageFluct = root.Q<FloatField>("fl_damage_fluctuation");
        _fClip = root.Q<FloatField>("fl_clip_size");
        _fReload = root.Q<FloatField>("fl_reload_time");
        _fSimul = root.Q<FloatField>("fl_simultanious_bullets");
        _fCrit = root.Q<FloatField>("fl_critical_chance");
        _fStagger = root.Q<FloatField>("fl_stagger");
        _fRecoil = root.Q<FloatField>("fl_recoil");
        _fRecoil = root.Q<FloatField>("fl_recoil");
        _fAccuracy = root.Q<FloatField>("fl_accuracy");
        _fWeight = root.Q<FloatField>("fl_weight");
        _fOptimalRange = root.Q<FloatField>("fl_optimal_range");
        _fMaxRange = root.Q<FloatField>("fl_max_range");
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

        // Update active tab visual
        foreach (var kv in _tabButtons)
        {
            kv.Value.RemoveFromClassList("is-active");
            kv.Value.style.borderBottomColor = StyleKeyword.Null;
        }
        if (_tabButtons.TryGetValue(cat, out var activeTab))
        {
            activeTab.AddToClassList("is-active");
            _activeTab = activeTab;
            _tabPulseTime = 0f;
        }

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

    private Dictionary<Cat, Button> _tabButtons = new();

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
            tab.focusable = false;
            tab.clicked += () => ShowCategory(cat);
            _tabButtons[cat] = tab;
        }
    }

    private void HookCarouselCategoryEvents()
    {
        // Always unsubscribe first to prevent duplicate registrations on re-enable
        carousel.PrevCategoryRequested -= OnPrevCategory;
        carousel.NextCategoryRequested -= OnNextCategory;
        carousel.PrevCategoryRequested += OnPrevCategory;
        carousel.NextCategoryRequested += OnNextCategory;
    }

    private void OnPrevCategory() => ChangeCategory(-1);
    private void OnNextCategory() => ChangeCategory(+1);

    private WeaponButton BuildWeaponButton(WeaponDef w, string slotText)
    {
        var wb = new WeaponButton();
        wb.name = $"btn_{w.id}";
        wb.focusable = false;

        var tex = Resources.Load<Texture2D>($"{ThumbsRoot}/{w.id}")
               ?? Resources.Load<Texture2D>($"{ThumbsRoot}/{w.display}");

        wb.SetData(tex, slotText, w.display, topRight: "GUNSMITH", bottomRight: "LVL 1");
        wb.SetLocked(w.id != UnlockedWeapon);
        wb.Clicked += _ => SelectWeapon(w, wb);

        return wb;
    }

    private void SelectWeapon(WeaponDef w, WeaponButton wb)
    {
        if (wb.IsLocked) return;

        bool sameWeapon = _selectedWeapon.id == w.id && _spawned != null;

        // Deactivate old button
        if (_selectedBtn != null)
            _selectedBtn.SetActive(false);

        _selectedBtn = wb;
        _selectedWeapon = w;
        _selectedBtn.SetActive(true);

        if (sameWeapon) return;

        UpdatePreview_3D(w);

        if (TryMapToEnum(w.id, out var weaponEnum))
            ApplyWeaponSelection(weaponEnum);
        else
            Debug.LogWarning($"No WeaponEnum match for id '{w.id}'");
    }

    private bool TryMapToEnum(string id, out WeaponEnum we)
    {
        // id strings already match enum names (e.g. WPN_AP85)
        return System.Enum.TryParse(id, out we);
    }

    private void ApplyWeaponSelection(WeaponEnum weaponEnum)
    {
        DataHolder.chosenWeapon = weaponEnum;

        // Weapon type logic (former ClickAssignData)
        if (weaponEnum == WeaponEnum.WPN_M4 || weaponEnum == WeaponEnum.WPN_M16)
            DataHolder.weaponType = WeaponType.H2;
        else
            DataHolder.weaponType = WeaponType.H1;

        var cfg = WeaponConfigSingleton.Instance.WeaponConfig;

        // Push current config into UI fields (if present)
        if (_fDamage != null)
        {
            _fDamage.value = cfg.Damage;
            _fFireRate.value = cfg.FireRate;
            _fDamageFluct.value = cfg.DamageFluctuation;
            _fClip.value = cfg.ClipSize;
            _fReload.value = cfg.ReloadTime;
            _fSimul.value = cfg.SimultaniousBullets;
            _fCrit.value = cfg.CritChance;
            _fStagger.value = cfg.Stagger;
            _fRecoil.value = cfg.Recoil;
            _fAccuracy.value = cfg.Accuracy;
            _fWeight.value = cfg.Weight;
            _fOptimalRange.value = cfg.OptimalRange;
            _fMaxRange.value = cfg.MaxEffectiveRange;
        }

        UpdateStatBars(cfg);
        WeaponChanged?.Invoke(weaponEnum);
    }

    private void UpdateStatBars(WeaponConfig cfg)
    {
        if (_statBarFills == null) return;

        // All values normalized 0�100 ? bar width 0%�100%
        float[] values =
        {
            cfg.Damage,
            cfg.FireRate,
            cfg.DamageFluctuation,
            cfg.ClipSize,
            cfg.ReloadTime,
            cfg.SimultaniousBullets,
            cfg.CritChance,
            cfg.Stagger,
            cfg.Recoil,
            cfg.Accuracy,
            cfg.Weight,
            cfg.OptimalRange,
            cfg.MaxEffectiveRange,
        };

        for (int i = 0; i < _statBarFills.Length; i++)
        {
            if (_statBarFills[i] == null) continue;
            float pct = Mathf.Clamp01(values[i] / 100f);
            _statBarFills[i].style.width = Length.Percent(pct * 100f);
        }
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
            _autoSpin = false; // user grabbed it -> stop idle spin
            _lastPointer = e.position; // Vector2
            _preview.CapturePointer(e.pointerId);
        });

        _preview.RegisterCallback<PointerMoveEvent>(e =>
        {
            if (!_dragging || _spawned == null) return;
            Vector2 delta = e.position - _lastPointer; // Vector2 - Vector2 ?
            _lastPointer = e.position;

            // yaw around up axis
            _spawned.transform.Rotate(Vector3.up, -delta.x * rotateSpeed, Space.World);
            // pitch
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
            //Debug.Log($"Zoom factor: {_zoomFactor}");
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
        _cam.cullingMask = 1 << 31; // Only render preview layer
        _cam.transform.position = new Vector3(0, 0, 5);
        _cam.transform.rotation = Quaternion.identity;

        // Exclude preview layer from main camera so rig is invisible to it
        if (Camera.main != null)
            Camera.main.cullingMask &= ~(1 << 31);



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
        if (_slideCoroutine != null)
        {
            StopCoroutine(_slideCoroutine);
            if (_outgoingSpawned) { Destroy(_outgoingSpawned); _outgoingSpawned = null; }
        }

        GameObject prefab =
            Resources.Load<GameObject>($"{ModelsRoot}/{w.id}") ??
            Resources.Load<GameObject>($"{ModelsRoot}/{w.display}");

        if (!prefab)
        {
            Debug.LogWarning($"Prefab not found in Resources/{ModelsRoot}: '{w.id}' or '{w.display}'.");
            if (_spawned) { Destroy(_spawned); _spawned = null; }
            return;
        }

        // Keep old weapon alive as outgoing
        _outgoingSpawned = _spawned;
        _spawned = null;

        // Spawn new weapon
        var newWeapon = Instantiate(prefab, _rig.transform);
        newWeapon.name = $"PREVIEW_{w.id}";
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        newWeapon.transform.localScale = Vector3.one;

        foreach (var r in newWeapon.GetComponentsInChildren<Renderer>(true))
            r.enabled = true;
        SetLayerRecursively(newWeapon, 31);

        _spawned = newWeapon;
        _autoSpin = true; // fresh weapon -> resume idle spin
        _zoomFactor = 1f;
        _fitNextLateUpdate = true;
        PositionCameraForCurrentModel();

        _slideCoroutine = StartCoroutine(SlidePreviewTransition(_outgoingSpawned, _spawned));
    }

    private System.Collections.IEnumerator SlidePreviewTransition(GameObject outWeapon, GameObject inWeapon)
    {
        const float duration = 0.35f;
        const float slideX = 1.5f; // local rig units
        float elapsed = 0f;

        // Capture inWeapon's fitted center position (set by PositionCameraForCurrentModel)
        Vector3 inCenter = inWeapon != null ? inWeapon.transform.localPosition : Vector3.zero;
        Vector3 outCenter = outWeapon != null ? outWeapon.transform.localPosition : Vector3.zero;

        Vector3 inStart = inCenter + new Vector3(slideX, 0f, 0f);
        Vector3 outEnd = outCenter + new Vector3(-slideX, 0f, 0f);

        if (inWeapon != null) inWeapon.transform.localPosition = inStart;

        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            if (outWeapon != null) outWeapon.transform.localPosition = Vector3.Lerp(outCenter, outEnd, t);
            if (inWeapon != null) inWeapon.transform.localPosition = Vector3.Lerp(inStart, inCenter, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (inWeapon != null) inWeapon.transform.localPosition = inCenter;
        if (outWeapon != null) Destroy(outWeapon);

        _outgoingSpawned = null;
        _slideCoroutine = null;
    }

    private bool _fitNextLateUpdate = false;
    private Coroutine _slideCoroutine;
    private GameObject _outgoingSpawned;

    void LateUpdate()
    {
        _selectedBtn?.Tick(Time.deltaTime);

        // Idle spin around Y until user drags to rotate
        if (_autoSpin && !_dragging && _spawned != null)
            _spawned.transform.Rotate(Vector3.up, autoSpinSpeed * Time.deltaTime, Space.World);

        // Pulse active tab underline
        if (_activeTab != null)
        {
            _tabPulseTime += Time.deltaTime;
            float alpha = 0.4f + 0.6f * (0.5f + 0.5f * Mathf.Sin(_tabPulseTime * 2.8f));
            _activeTab.style.borderBottomColor = new StyleColor(new Color(0.369f, 0.882f, 0.647f, alpha));
        }

        if (_fitNextLateUpdate)
        {
            _fitNextLateUpdate = false;
            PositionCameraForCurrentModel();
        }
    }

    private void PositionCameraForCurrentModel()
    {
        if (_spawned == null || _cam == null) return;

        // Only use MeshRenderer / SkinnedMeshRenderer � ignore ParticleSystemRenderer,
        // LineRenderer, etc. that can inflate bounds far beyond the visible gun geometry
        var meshRenderers = _spawned.GetComponentsInChildren<MeshRenderer>();
        var skinnedRenderers = _spawned.GetComponentsInChildren<SkinnedMeshRenderer>();

        bool hasBounds = false;
        Bounds b = default;

        foreach (var r in meshRenderers)
        {
            if (!hasBounds) { b = r.bounds; hasBounds = true; }
            else b.Encapsulate(r.bounds);
        }
        foreach (var r in skinnedRenderers)
        {
            if (!hasBounds) { b = r.bounds; hasBounds = true; }
            else b.Encapsulate(r.bounds);
        }

        if (!hasBounds) return;

        // Centre the model pivot on world origin only if it isn't already there
        // (avoid double-offset on repeated calls e.g. after RT resize)
        float distToOrigin = b.center.magnitude;
        if (distToOrigin > 0.001f)
        {
            _spawned.transform.position -= b.center;
            b.center = Vector3.zero;
        }

        float radius = Mathf.Max(0.001f, b.extents.magnitude);

        // Distance based on FOV so the whole object fits
        float fovRad = _cam.fieldOfView * Mathf.Deg2Rad;
        float dist = (radius * fitPadding * _zoomFactor) / Mathf.Tan(fovRad * 0.5f);

        // Adjust near clip so model never clips when zooming in close
        _cam.nearClipPlane = Mathf.Max(0.001f, dist * 0.01f);
        _cam.transform.position = new Vector3(0f, 0f, dist);
        _cam.transform.LookAt(Vector3.zero, Vector3.up);
    }

    private void OnPreviewResized(GeometryChangedEvent _) => ResizeRTToPreview();

    private void ResizeRTToPreview()
    {
        if (_preview == null || _cam == null) return;

        int w = Mathf.Max(1, Mathf.RoundToInt(_preview.resolvedStyle.width));
        int h = Mathf.Max(1, Mathf.RoundToInt(_preview.resolvedStyle.height));

        // Skip if layout not resolved yet
        if (w <= 1 || h <= 1) return;

        // Rebuild RT only if size actually changed
        if (_rt != null && _rt.width == w && _rt.height == h) return;

        if (_rt != null) { _rt.Release(); Destroy(_rt); }
        _rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
        {
            name = "WeaponPreviewRT",
            useMipMap = false,
            autoGenerateMips = false
        };
        _rt.Create();
        _cam.targetTexture = _rt;
        _cam.aspect = (float)w / h;
        if (_previewImage != null) _previewImage.image = _rt;

        // Refit camera for current model
        PositionCameraForCurrentModel();
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    // ---------- Keyboard category switch fallback ----------
}
