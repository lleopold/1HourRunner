using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;


public class UIT_ChoosePlayer : MonoBehaviour
{
    [SerializeField] public PlayerEnum player;
    [SerializeField] public WeaponEnum weapon;
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private PreviewAnimationConfig _previewAnimConfig;
    [SerializeField] private VisualTreeAsset _statRowTemplateRef; // optional inspector override
    private VisualElement _root;

    private WeaponConfig weaponConfig;
    private PlayerConfig playerConfig;

    private IntegerField _int_weapon;
    private Button _btn_jennifer;
    private Button _btn_swat;
    private Button _btn_steel;
    private Button _btn_business_girl;
    private Button _btn_dr;
    private Button _btn_green_hat_basic;
    private Button _btn_choose_level;

    private Animator _presentedAnimator;

    // Selection pulse
    private Button _activePlayerBtn;
    private float _pulseTime;
    private Dictionary<Button, VisualElement> _accentLines = new();
    private Dictionary<Button, Label> _levelLabels = new();
    private List<(Button btn, VisualElement accent)> _allThumbAccents = new();

    // Camera orbit
    private Camera _previewCamera;
    private Vector3 _orbitCenter;
    private float _orbitAngle;
    private const float OrbitRadius = 3.8f;
    private const float OrbitHeight = 2.4f;
    private const float OrbitSpeed = 18f;

    // Perk labels
    private Label _lbl_perk_name;
    private Label _lbl_perk_desc;

    // ── Dynamic stat system ──────────────────────────────────
    private enum StatCategory { Movement, Combat, Survival }

    private class StatDef
    {
        public string Key;
        public string Display;
        public StatCategory Category;
        public Func<PlayerConfig, float> Get;
        public Action<PlayerConfig, float> Set;
        public float Step, Min, Max, BarMax;
    }

    private class StatRowUI
    {
        public StatDef Def;
        public VisualElement Root;
        public Label NameLabel;
        public Label ValueLabel;
        public Button Minus;
        public Button Plus;
        public VisualElement Gauge;
    }

    private VisualTreeAsset _statRowTemplate;
    private VisualElement _statsContainer;
    private Label _lbl_points;
    private Button _btn_level_up;
    private readonly List<StatDef> _statDefs = new();
    private readonly Dictionary<string, StatRowUI> _rows = new();
    private readonly Dictionary<string, int> _pendingClicks = new(); // points allocated but not committed
    private StatCategory _currentCategory = StatCategory.Movement;

    private static readonly Dictionary<PlayerEnum, string> DefaultPerkNames = new()
    {
        { PlayerEnum.Jennifer,                "RUNNER"        },
        { PlayerEnum.Swat,                    "TACTICIAN"     },
        { PlayerEnum.Jackson_Steel_Reynolds,  "TRIGGER HAPPY" },
        { PlayerEnum.BusinessGirl,            "SNIPER"        },
        { PlayerEnum.Dr,                      "MARTIAL ARTS"  },
        { PlayerEnum.GreenHat_basic,          "RUNNER"        },
        { PlayerEnum.Solider,                 "TACTICIAN"     },
    };


    private void Awake()
    {
        _root = _uiDocument.rootVisualElement;
        AttachWeaponSelectionStyles();

        if (DataHolder.ChosenPlayer == null)
            player = PlayerEnum.GreenHat_basic;
        else
            player = DataHolder.ChosenPlayer;

        if (DataHolder.chosenWeapon == null)
            DataHolder.chosenWeapon = WeaponEnum.WPN_AP85;
        else
            weapon = DataHolder.chosenWeapon;

        playerConfig = PlayerConfigSingleton.Instance.PlayerConfig;

        GameObject character = LoadModel();

        // Init camera orbit
        var spot = GameObject.Find("CharPositionSpot");
        if (spot != null) _orbitCenter = spot.transform.position;
        _previewCamera = Camera.main;
        if (_previewCamera != null)
        {
            _orbitAngle = 180f;
            UpdateOrbitCamera();
        }

        _int_weapon = _root.Q<IntegerField>("int_weapon");

        _btn_business_girl = _root.Q<Button>("btn_business_girl");
        _btn_dr = _root.Q<Button>("btn_dr");
        _btn_jennifer = _root.Q<Button>("btn_jennifer");
        _btn_swat = _root.Q<Button>("btn_swat");
        _btn_steel = _root.Q<Button>("btn_steel");
        _btn_green_hat_basic = _root.Q<Button>("btn_green_hat");
        _btn_business_girl.RegisterCallback<ClickEvent>(ev => ClickAssignData(PlayerEnum.BusinessGirl));
        _btn_dr.RegisterCallback<ClickEvent>(ev => ClickAssignData(PlayerEnum.Dr));
        _btn_jennifer.RegisterCallback<ClickEvent>(ev => ClickAssignData(PlayerEnum.Jennifer));
        _btn_swat.RegisterCallback<ClickEvent>(ev => ClickAssignData(PlayerEnum.Swat));
        _btn_steel.RegisterCallback<ClickEvent>(ev => ClickAssignData(PlayerEnum.Jackson_Steel_Reynolds));
        _btn_green_hat_basic.RegisterCallback<ClickEvent>(ev => ClickAssignData(PlayerEnum.GreenHat_basic));

        var btnBack = _root.Q<Button>("btn_back");
        btnBack?.RegisterCallback<ClickEvent>(_ => SceneManager.LoadScene("Start"));

        BuildThumbOverlays();

        _lbl_perk_name = _root.Q<Label>("lbl_perk_name");
        _lbl_perk_desc = _root.Q<Label>("lbl_perk_desc");

        SetActivePlayerButton(GetButtonForPlayer(player));

        _btn_choose_level = _root.Q<Button>("btn_choose_weapon");
        _btn_choose_level?.RegisterCallback<ClickEvent>(ev => ClickChooseWeapon());

        InitStats();
        LoadSettingsToUI();
    }

    // ── Thumb button overlays (perk strip, bars, level) ──────
    private void BuildThumbOverlays()
    {
        var thumbData = new (Button btn, PlayerEnum p)[]
        {
            (_btn_jennifer,        PlayerEnum.Jennifer),
            (_btn_swat,            PlayerEnum.Swat),
            (_btn_steel,           PlayerEnum.Jackson_Steel_Reynolds),
            (_btn_business_girl,   PlayerEnum.BusinessGirl),
            (_btn_dr,              PlayerEnum.Dr),
            (_btn_green_hat_basic, PlayerEnum.GreenHat_basic),
        };
        foreach (var (btn, p) in thumbData)
        {
            if (btn == null) continue;
            btn.style.position = Position.Relative;
            btn.style.overflow = Overflow.Visible;

            var topbar = new VisualElement();
            topbar.AddToClassList("thumb__topbar");
            btn.Add(topbar);

            string perkName = DefaultPerkNames.TryGetValue(p, out var n) ? n : "";
            var perkLabel = new Label(perkName);
            perkLabel.AddToClassList("thumb__perk");
            btn.Add(perkLabel);

            float perkProgress = UnityEngine.Random.Range(0.30f, 0.70f);
            var barTrack = new VisualElement();
            barTrack.AddToClassList("thumb__bar-track");
            var barFill = new VisualElement();
            barFill.AddToClassList("thumb__bar-fill");
            barFill.style.width = Length.Percent(perkProgress * 100f);
            barTrack.Add(barFill);
            btn.Add(barTrack);

            var bottombar = new VisualElement();
            bottombar.AddToClassList("thumb__bottombar");
            btn.Add(bottombar);

            var levelLabel = new Label("LVL 1");
            levelLabel.AddToClassList("thumb__level");
            btn.Add(levelLabel);
            _levelLabels[btn] = levelLabel;

            var hoverLine = new VisualElement();
            hoverLine.AddToClassList("thumb__hover-line");
            btn.Add(hoverLine);

            var accentLine = new VisualElement();
            accentLine.AddToClassList("thumb__accent-line");
            btn.Add(accentLine);
            _accentLines[btn] = accentLine;
            _allThumbAccents.Add((btn, accentLine));
        }
    }

    // ── Stat system ──────────────────────────────────────────
    private void InitStats()
    {
        _statRowTemplate = _statRowTemplateRef != null
            ? _statRowTemplateRef
            : Resources.Load<VisualTreeAsset>("UI/StatRow");

        _statsContainer = _root.Q<VisualElement>("stats_container");
        _lbl_points = _root.Q<Label>("lbl_points");
        _btn_level_up = _root.Q<Button>("btn_level_up");
        _btn_level_up?.RegisterCallback<ClickEvent>(_ => CommitLevelUp());

        // Tabs
        _root.Q<Button>("tab_movement")?.RegisterCallback<ClickEvent>(_ => SelectCategory(StatCategory.Movement));
        _root.Q<Button>("tab_combat")?.RegisterCallback<ClickEvent>(_ => SelectCategory(StatCategory.Combat));
        _root.Q<Button>("tab_survival")?.RegisterCallback<ClickEvent>(_ => SelectCategory(StatCategory.Survival));

        BuildStatDefs();
        SelectCategory(_currentCategory);
    }

    private void BuildStatDefs()
    {
        _statDefs.Clear();

        // Movement
        Add("speed",         "Speed",          StatCategory.Movement, c => c.speed,                   (c, v) => c.speed = v,                   0.1f, 0, 50,  20);
        Add("acceleration",  "Acceleration",   StatCategory.Movement, c => c.acceleration,            (c, v) => c.acceleration = v,            0.1f, 0, 50,  20);
        Add("running_pct",   "Running %",      StatCategory.Movement, c => c.RunningSpeed_pct,         (c, v) => c.RunningSpeed_pct = v,         1f,   0, 100, 100);
        Add("back_move",     "Back move",      StatCategory.Movement, c => c.BackMovementPenalty_pct,  (c, v) => c.BackMovementPenalty_pct = v,  1f,   0, 100, 100);
        Add("stamina_speed", "Stamina speed",  StatCategory.Movement, c => c.staminaRegenDelay,        (c, v) => c.staminaRegenDelay = v,        0.1f, 0, 10,  10);

        // Combat
        Add("strength",      "Strength",       StatCategory.Combat,   c => c.strength,                (c, v) => c.strength = v,                1f,   0, 200, 200);
        Add("aim",           "Aim",            StatCategory.Combat,   c => c.Accuracy,                (c, v) => c.Accuracy = v,                0.5f, 0, 100, 100);
        Add("recoil",        "Recoil",         StatCategory.Combat,   c => c.recoilReduction,         (c, v) => c.recoilReduction = v,         0.5f, 0, 100, 100);
        Add("reload",        "Reload speed",   StatCategory.Combat,   c => c.reloadSpeed,             (c, v) => c.reloadSpeed = v,             0.1f, 0, 10,  10);
        Add("vision",        "Vision",         StatCategory.Combat,   c => c.vision,                  (c, v) => c.vision = v,                  1f,   0, 100, 100);

        // Survival
        Add("health",        "Health",         StatCategory.Survival, c => c.health,                  (c, v) => c.health = v,                  5f,   0, 500, 500);
        Add("weight",        "Weight",         StatCategory.Survival, c => c.weight,                  (c, v) => c.weight = v,                  1f,   0, 500, 200);
        Add("stamina",       "Stamina",        StatCategory.Survival, c => c.stamina,                 (c, v) => c.stamina = v,                 5f,   0, 500, 500);
        Add("stamina_regen", "Stamina regen",  StatCategory.Survival, c => c.staminaRegenSpeed,       (c, v) => c.staminaRegenSpeed = v,       0.1f, 0, 10,  10);
        Add("injured",       "Injured penalty",StatCategory.Survival, c => c.InjuredPenalty,          (c, v) => c.InjuredPenalty = v,          1f,   0, 100, 100);

        void Add(string key, string disp, StatCategory cat, Func<PlayerConfig, float> get, Action<PlayerConfig, float> set, float step, float min, float max, float barMax)
            => _statDefs.Add(new StatDef { Key = key, Display = disp, Category = cat, Get = get, Set = set, Step = step, Min = min, Max = max, BarMax = barMax });
    }

    private void SelectCategory(StatCategory cat)
    {
        _currentCategory = cat;
        SetTabActive("tab_movement", cat == StatCategory.Movement);
        SetTabActive("tab_combat",   cat == StatCategory.Combat);
        SetTabActive("tab_survival", cat == StatCategory.Survival);

        if (_statsContainer == null) return;
        _statsContainer.Clear();
        _rows.Clear();

        foreach (var def in _statDefs)
        {
            if (def.Category != cat) continue;
            CreateRow(def);
        }
        RefreshStats();
        UpdatePointsUI();
        UpdateLevelUpButton();
    }

    private void SetTabActive(string tabName, bool active)
    {
        var t = _root.Q<Button>(tabName);
        if (t != null) t.EnableInClassList("is-tab-active", active);
    }

    private void CreateRow(StatDef def)
    {
        VisualElement row;
        if (_statRowTemplate != null)
        {
            var tc = _statRowTemplate.Instantiate();
            tc.style.flexShrink = 0;
            row = tc;
        }
        else
        {
            row = BuildRowFallback();
        }

        var ui = new StatRowUI
        {
            Def = def,
            Root = row,
            NameLabel = row.Q<Label>("stat_name"),
            ValueLabel = row.Q<Label>("stat_value"),
            Minus = row.Q<Button>("btn_minus"),
            Plus = row.Q<Button>("btn_plus"),
            Gauge = row.Q<VisualElement>("stat_gauge"),
        };
        if (ui.NameLabel != null) ui.NameLabel.text = def.Display;
        ui.Minus?.RegisterCallback<ClickEvent>(_ => OnStep(def.Key, -1));
        ui.Plus?.RegisterCallback<ClickEvent>(_ => OnStep(def.Key, +1));

        _statsContainer.Add(row);
        _rows[def.Key] = ui;
    }

    // Code-built fallback if the StatRow template asset is missing.
    private VisualElement BuildRowFallback()
    {
        var root = new VisualElement();
        root.AddToClassList("stat-row");

        var gauge = new VisualElement { name = "stat_gauge" };
        gauge.AddToClassList("stat-row__gauge");
        root.Add(gauge);

        var content = new VisualElement();
        content.AddToClassList("stat-row__content");

        var name = new Label("Stat") { name = "stat_name" };
        name.AddToClassList("stat-name");
        content.Add(name);

        var edit = new VisualElement();
        edit.AddToClassList("stat-edit");

        var minus = new Button { name = "btn_minus", text = "−" };
        minus.AddToClassList("step");
        minus.AddToClassList("step--minus");
        var val = new Label("0") { name = "stat_value" };
        val.AddToClassList("stat-val");
        var plus = new Button { name = "btn_plus", text = "+" };
        plus.AddToClassList("step");
        plus.AddToClassList("step--plus");

        edit.Add(minus);
        edit.Add(val);
        edit.Add(plus);
        content.Add(edit);
        root.Add(content);
        return root;
    }

    private int GetClicks(string key) => _pendingClicks.TryGetValue(key, out var c) ? c : 0;

    private int TotalPendingClicks()
    {
        int sum = 0;
        foreach (var kv in _pendingClicks) sum += kv.Value;
        return sum;
    }

    private int AvailablePoints() => playerConfig.AttributePoints - TotalPendingClicks();

    private void OnStep(string key, int dir)
    {
        if (!_rows.TryGetValue(key, out var ui)) return;
        var def = ui.Def;
        int clicks = GetClicks(key);

        if (dir > 0)
        {
            if (AvailablePoints() <= 0) return;
            float baseVal = def.Get(playerConfig);
            if (baseVal + (clicks + 1) * def.Step > def.Max) return;
            clicks++;
        }
        else
        {
            if (clicks <= 0) return; // minus only undoes pending allocation, never base
            clicks--;
        }
        _pendingClicks[key] = clicks;

        RefreshRow(ui);
        UpdatePointsUI();
        UpdateLevelUpButton();
    }

    private void RefreshStats()
    {
        foreach (var kv in _rows) RefreshRow(kv.Value);
    }

    private void RefreshRow(StatRowUI ui)
    {
        var def = ui.Def;
        int clicks = GetClicks(def.Key);
        float val = def.Get(playerConfig) + clicks * def.Step;

        if (ui.ValueLabel != null) ui.ValueLabel.text = val.ToString("0.#");

        float frac = def.BarMax > 0 ? Mathf.Clamp01(val / def.BarMax) : 0f;
        if (ui.Gauge != null) ui.Gauge.style.width = Length.Percent(frac * 100f);

        bool pending = clicks > 0;
        ui.ValueLabel?.EnableInClassList("is-pending", pending);
        ui.Gauge?.EnableInClassList("is-pending", pending);

        // Rule 3: steppers shown only when the player has points to spend.
        bool showSteppers = playerConfig.AttributePoints > 0;
        var disp = showSteppers ? DisplayStyle.Flex : DisplayStyle.None;
        if (ui.Minus != null) ui.Minus.style.display = disp;
        if (ui.Plus != null) ui.Plus.style.display = disp;
    }

    private void UpdatePointsUI()
    {
        if (_lbl_points == null) return;
        int avail = AvailablePoints();
        _lbl_points.text = avail.ToString();
        _lbl_points.EnableInClassList("is-depleted", avail <= 0);
    }

    private void UpdateLevelUpButton()
    {
        if (_btn_level_up == null) return;
        _btn_level_up.SetEnabled(TotalPendingClicks() > 0);
    }

    private void CommitLevelUp()
    {
        if (TotalPendingClicks() <= 0) return;

        foreach (var def in _statDefs)
        {
            int clicks = GetClicks(def.Key);
            if (clicks <= 0) continue;
            def.Set(playerConfig, def.Get(playerConfig) + clicks * def.Step);
        }
        playerConfig.AttributePoints -= TotalPendingClicks();
        _pendingClicks.Clear();

        RefreshStats();
        UpdatePointsUI();
        UpdateLevelUpButton();
    }

    private void Update()
    {
        if (_presentedAnimator != null && _presentedAnimator.isActiveAndEnabled
            && _presentedAnimator.runtimeAnimatorController != null)
        {
            float blend = Mathf.PingPong(Time.time / 5f, 1f);
            _presentedAnimator.SetFloat("Blend", blend);
        }

        if (_previewCamera != null)
        {
            _orbitAngle += OrbitSpeed * Time.deltaTime;
            UpdateOrbitCamera();
        }

        if (_activePlayerBtn != null && _accentLines.TryGetValue(_activePlayerBtn, out var activeAccent))
        {
            _pulseTime += Time.deltaTime;
            float opacity = 0.3f + 0.7f * (0.5f + 0.5f * Mathf.Sin(_pulseTime * 3.5f));
            activeAccent.style.opacity = opacity;
        }
    }

    private void UpdateOrbitCamera()
    {
        float rad = _orbitAngle * Mathf.Deg2Rad;
        Vector3 camPos = _orbitCenter + new Vector3(
            Mathf.Sin(rad) * OrbitRadius,
            OrbitHeight,
            Mathf.Cos(rad) * OrbitRadius);
        _previewCamera.transform.position = camPos;
        _previewCamera.transform.LookAt(_orbitCenter + Vector3.up * 1.0f);
    }

    private void SetActivePlayerButton(Button btn)
    {
        if (_activePlayerBtn != null)
        {
            _activePlayerBtn.RemoveFromClassList("is-active");
            if (_accentLines.TryGetValue(_activePlayerBtn, out var oldAccent))
                oldAccent.style.opacity = 0f;
        }

        _activePlayerBtn = btn;
        _pulseTime = 0f;

        if (_activePlayerBtn != null)
            _activePlayerBtn.AddToClassList("is-active");
    }

    private Button GetButtonForPlayer(PlayerEnum p) => p switch
    {
        PlayerEnum.Jennifer => _btn_jennifer,
        PlayerEnum.Swat => _btn_swat,
        PlayerEnum.Jackson_Steel_Reynolds => _btn_steel,
        PlayerEnum.BusinessGirl => _btn_business_girl,
        PlayerEnum.Dr => _btn_dr,
        PlayerEnum.GreenHat_basic => _btn_green_hat_basic,
        _ => _btn_jennifer,
    };

    private void ClickChooseWeapon()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("ChooseWeapon");
    }

    private GameObject LoadModel()
    {
        _presentedAnimator = null;
        var existing = GameObject.Find("PresentedModel");
        if (existing != null)
            Destroy(existing);

        GameObject model = Resources.Load<GameObject>("Models/Player/" + DataHolder.ChosenPlayer.ToString() + "_model");
        GameObject pos = GameObject.Find("CharPositionSpot");
        GameObject character = Instantiate(model, pos.transform.position, Quaternion.identity);
        character.transform.Rotate(0f, -180f, 0f);
        Animator animator = character.GetComponent<Animator>();
        var baseController = Resources.Load<RuntimeAnimatorController>("Animators/IdleController");
        if (_previewAnimConfig != null && _previewAnimConfig.animation1 != null)
        {
            var overrideController = new AnimatorOverrideController(baseController);
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrideController.GetOverrides(overrides);

            AnimationClip[] slots = new[]
            {
                _previewAnimConfig.animation1,
                _previewAnimConfig.animation2,
                _previewAnimConfig.animation3,
                _previewAnimConfig.animation4,
                _previewAnimConfig.animation5,
                _previewAnimConfig.animation6,
            };

            int slotIndex = 0;
            for (int i = 0; i < overrides.Count; i++)
            {
                if (overrides[i].Key == null) continue;
                if (slotIndex < slots.Length && slots[slotIndex] != null)
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, slots[slotIndex]);
                slotIndex++;
            }
            overrideController.ApplyOverrides(overrides);
            animator.runtimeAnimatorController = overrideController;
        }
        else
        {
            animator.runtimeAnimatorController = baseController;
        }
        animator.SetFloat("Blend", 0f);
        character.name = "PresentedModel";
        _presentedAnimator = animator;

        AttachPreviewWeapon(character);

        return character;
    }

    private void AttachPreviewWeapon(GameObject character)
    {
        var existingWeapon = character.transform.Find("PreviewWeapon");
        if (existingWeapon != null) Destroy(existingWeapon.gameObject);

        GameObject weaponPrefab = Resources.Load<GameObject>("Models/Weapons/WPN_AP85");
        if (weaponPrefab == null) return;

        Transform rightHand = FindBoneRecursive(character.transform, "RightHand");
        if (rightHand == null) return;

        GameObject weapon = Instantiate(weaponPrefab, rightHand);
        weapon.name = "PreviewWeapon";
        weapon.transform.localPosition = new Vector3(-0.0464068204f, 0.186311349f, 0.041510012f);
        weapon.transform.localRotation = Quaternion.Euler(279.578644f, 325.888763f, 119.821121f);
        weapon.transform.localScale = Vector3.one;
    }

    private Transform FindBoneRecursive(Transform parent, string boneNameContains)
    {
        if (parent.name.Contains(boneNameContains)) return parent;
        foreach (Transform child in parent)
        {
            var result = FindBoneRecursive(child, boneNameContains);
            if (result != null) return result;
        }
        return null;
    }

    void AttachWeaponSelectionStyles()
    {
        var weaponSel = Resources.Load<StyleSheet>("UI/WeaponSelection");
        var choosePlayer = Resources.Load<StyleSheet>("UI/ChoosePlayer");

        if (weaponSel != null) _root.styleSheets.Add(weaponSel);
        if (choosePlayer != null) _root.styleSheets.Add(choosePlayer);
    }

    private void ClickPlay()
    {
        playerConfig = PlayerConfigSingleton.Instance.PlayerConfig;
        Loader.Load(Loader.Scene.ChooseWeapon);
        PlayerConfigSingleton.Instance.SaveConfigToFile();
    }

    void ClickAssignData(PlayerEnum player)
    {
        DataHolder.ChosenPlayer = player;
        playerConfig = PlayerConfigSingleton.Instance.PlayerConfig;
        _pendingClicks.Clear(); // pending allocations don't carry across characters
        LoadSettingsToUI();
        LoadModel();
        SetActivePlayerButton(GetButtonForPlayer(player));
    }

    void LoadSettingsToUI()
    {
        // Perk section
        string perkName = !string.IsNullOrEmpty(playerConfig.PerkName)
            ? playerConfig.PerkName
            : (DefaultPerkNames.TryGetValue(DataHolder.ChosenPlayer, out var def) ? def : "—");
        if (_lbl_perk_name != null) _lbl_perk_name.text = perkName;
        if (_lbl_perk_desc != null) _lbl_perk_desc.text = playerConfig.PerkDescription ?? "";

        if (_activePlayerBtn != null && _levelLabels.TryGetValue(_activePlayerBtn, out var lvlLabel))
            lvlLabel.text = $"LVL {playerConfig.Level}";

        RefreshStats();
        UpdatePointsUI();
        UpdateLevelUpButton();
    }

    public void ChoosePlayer(int index)
    {
        if (index == 0) { DataHolder.ChosenPlayer = PlayerEnum.Swat; player = PlayerEnum.Swat; }
        if (index == 1) { DataHolder.ChosenPlayer = PlayerEnum.BusinessGirl; player = PlayerEnum.BusinessGirl; }
        if (index == 2) { DataHolder.ChosenPlayer = PlayerEnum.Dr; player = PlayerEnum.Dr; }
        if (index == 3) { DataHolder.ChosenPlayer = PlayerEnum.Jennifer; player = PlayerEnum.Jennifer; }
        if (index == 4) { DataHolder.ChosenPlayer = PlayerEnum.Solider; player = PlayerEnum.Solider; }
        if (index == 5) { DataHolder.ChosenPlayer = PlayerEnum.GreenHat_basic; player = PlayerEnum.GreenHat_basic; }
    }
}

public enum PlayerEnum
{
    Swat,
    BusinessGirl,
    Dr,
    Jennifer,
    Solider,
    Jackson_Steel_Reynolds,
    GreenHat_basic
}
