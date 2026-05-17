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
    private VisualElement _root;

    [Obsolete("Used for drop down")]
    //[SerializeField] private Toggle toggleSwat, toggleBusinessGirl, toggleDr, toggleJennifer, toggleSolider, toggleGreenHatBasic;
    private WeaponConfig weaponConfig;
    private PlayerConfig playerConfig;
    private FloatField _fl_health;
    private FloatField _fl_weight;
    private FloatField _fl_strength;
    private FloatField _fl_stamina;
    private FloatField _fl_stamina_regen;
    private FloatField _fl_stamina_speed;
    private FloatField _fl_speed;
    private FloatField _fl_acceleration;
    private FloatField _fl_running_pct;
    private FloatField _fl_running_bck;
    private FloatField _fl_precision;
    private FloatField _fl_recoil;
    private FloatField _fl_reload_speed;
    private FloatField _fl_vision;
    private FloatField _fl_injured_penalty;
    private IntegerField _int_weapon;
    private Button _btn_jennifer;
    private Button _btn_swat;
    private Button _btn_steel;
    private Button _btn_business_girl;
    private Button _btn_dr;
    private Button _btn_green_hat_basic;
    private Button _btn_choose_level;

    private readonly Dictionary<string, Label> _statLabels = new();
    private Animator _presentedAnimator;

    // Selection pulse
    private Button _activePlayerBtn;
    private VisualElement _activeAccentLine;
    private float _pulseTime;
    private Dictionary<Button, VisualElement> _accentLines = new();
    private Dictionary<Button, Label> _levelLabels = new();
    // All buttons list for bulk pulse update
    private List<(Button btn, VisualElement accent)> _allThumbAccents = new();

    // Perk labels in stats panel
    private Label _lbl_perk_name;
    private Label _lbl_perk_desc;

    // Default perk names per character (can be overridden by PlayerConfig.PerkName)
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
        {
            player = PlayerEnum.GreenHat_basic;
        }
        else
        {
            player = DataHolder.ChosenPlayer;
        }
        if (DataHolder.chosenWeapon == null)
        {
            DataHolder.chosenWeapon = WeaponEnum.WPN_AP85;
        }
        else
        {
            weapon = DataHolder.chosenWeapon;
        }
        playerConfig = PlayerConfigSingleton.Instance.PlayerConfig;

        GameObject character = LoadModel();


        _fl_health = _root.Q<FloatField>("fl_health");
        _fl_weight = _root.Q<FloatField>("fl_weight");
        _fl_strength = _root.Q<FloatField>("fl_strength");
        _fl_stamina = _root.Q<FloatField>("fl_stamina");
        _fl_stamina_regen = _root.Q<FloatField>("fl_stamina_regen");
        _fl_stamina_speed = _root.Q<FloatField>("fl_stamina_speed");
        _fl_speed = _root.Q<FloatField>("fl_speed");
        _fl_acceleration = _root.Q<FloatField>("fl_acceleration");
        _fl_running_pct = _root.Q<FloatField>("fl_running_pct");
        _fl_running_bck = _root.Q<FloatField>("fl_back_move");
        _fl_precision = _root.Q<FloatField>("fl_aim");
        _fl_recoil = _root.Q<FloatField>("fl_recoil");
        _fl_reload_speed = _root.Q<FloatField>("fl_reload");
        _fl_vision = _root.Q<FloatField>("fl_vision");
        _fl_injured_penalty = _root.Q<FloatField>("fl_injured");


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

        // Inject WeaponButton-style overlays into all thumb buttons
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

            // Dark topbar strip
            var topbar = new VisualElement();
            topbar.AddToClassList("thumb__topbar");
            btn.Add(topbar);

            // Perk name — top-left on strip
            string perkName = DefaultPerkNames.TryGetValue(p, out var n) ? n : "";
            var perkLabel = new Label(perkName);
            perkLabel.AddToClassList("thumb__perk");
            btn.Add(perkLabel);

            // Perk progress bar (random 30–70% placeholder)
            float perkProgress = UnityEngine.Random.Range(0.30f, 0.70f);
            var barTrack = new VisualElement();
            barTrack.AddToClassList("thumb__bar-track");
            var barFill = new VisualElement();
            barFill.AddToClassList("thumb__bar-fill");
            barFill.style.width = Length.Percent(perkProgress * 100f);
            barTrack.Add(barFill);
            btn.Add(barTrack);

            // Bottom bar strip
            var bottombar = new VisualElement();
            bottombar.AddToClassList("thumb__bottombar");
            btn.Add(bottombar);

            // Level — centered on bottom bar
            var levelLabel = new Label("LVL 1");
            levelLabel.AddToClassList("thumb__level");
            btn.Add(levelLabel);
            _levelLabels[btn] = levelLabel;

            // Hover line — left side
            var hoverLine = new VisualElement();
            hoverLine.AddToClassList("thumb__hover-line");
            btn.Add(hoverLine);

            // Accent line — right side, always pulsing when not active
            var accentLine = new VisualElement();
            accentLine.AddToClassList("thumb__accent-line");
            btn.Add(accentLine);
            _accentLines[btn] = accentLine;
            _allThumbAccents.Add((btn, accentLine));
        }

        _lbl_perk_name = _root.Q<Label>("lbl_perk_name");
        _lbl_perk_desc = _root.Q<Label>("lbl_perk_desc");

        // Set initial selection based on saved player
        SetActivePlayerButton(GetButtonForPlayer(player));



        _fl_health.RegisterCallback<ChangeEvent<float>>(ev => PlayerConfigSingleton.Instance.PlayerConfig.health = ev.newValue);
        _fl_weight.RegisterCallback<ChangeEvent<float>>(ev => PlayerConfigSingleton.Instance.PlayerConfig.weight = ev.newValue);
        _fl_strength.RegisterCallback<ChangeEvent<float>>(ev => PlayerConfigSingleton.Instance.PlayerConfig.strength = ev.newValue);
        _fl_stamina.RegisterCallback<ChangeEvent<float>>(ev => PlayerConfigSingleton.Instance.PlayerConfig.stamina = ev.newValue);
        _fl_stamina_regen.RegisterCallback<ChangeEvent<float>>(ev => PlayerConfigSingleton.Instance.PlayerConfig.staminaRegenSpeed = ev.newValue);
        _fl_stamina_speed.RegisterCallback<ChangeEvent<float>>(ev => PlayerConfigSingleton.Instance.PlayerConfig.staminaRegenSpeed = ev.newValue);
        _fl_speed.RegisterCallback<ChangeEvent<float>>(ev => PlayerConfigSingleton.Instance.PlayerConfig.speed = ev.newValue);
        _fl_acceleration.RegisterCallback<ChangeEvent<float>>(ev => PlayerConfigSingleton.Instance.PlayerConfig.acceleration = ev.newValue);
        _fl_running_pct.RegisterCallback<ChangeEvent<float>>(ev => PlayerConfigSingleton.Instance.PlayerConfig.RunningSpeed_pct = ev.newValue);
        _fl_running_bck.RegisterCallback<ChangeEvent<float>>(ev => PlayerConfigSingleton.Instance.PlayerConfig.BackMovementPenalty_pct = ev.newValue);
        //_fl_precision.RegisterCallback<ChangeEvent<float>>(ev => PlayerConfigSingleton.Instance.PlayerConfig.aimingPrecision = ev.newValue);
        _fl_recoil.RegisterCallback<ChangeEvent<float>>(ev => PlayerConfigSingleton.Instance.PlayerConfig.recoilReduction = ev.newValue);
        _fl_reload_speed.RegisterCallback<ChangeEvent<float>>(ev => PlayerConfigSingleton.Instance.PlayerConfig.reloadSpeed = ev.newValue);
        _fl_vision.RegisterCallback<ChangeEvent<float>>(ev => PlayerConfigSingleton.Instance.PlayerConfig.vision = ev.newValue);
        _fl_injured_penalty.RegisterCallback<ChangeEvent<float>>(ev => PlayerConfigSingleton.Instance.PlayerConfig.InjuredPenalty = ev.newValue);

        _btn_choose_level = _root.Q<Button>("btn_choose_weapon");
        _btn_choose_level?.RegisterCallback<ClickEvent>(ev => ClickChooseWeapon());

        LoadSettingsToUI();
        RandomizeStatBars();

        // Wire steppers: fieldName, minusBtn, plusBtn, step, min, max
        WireStepper("fl_health", "btn_fl_health_minus", "btn_fl_health_plus", 5f, 0f, 500f);
        WireStepper("fl_weight", "btn_fl_weight_minus", "btn_fl_weight_plus", 1f, 0f, 500f);
        WireStepper("fl_strength", "btn_fl_strength_minus", "btn_fl_strength_plus", 1f, 0f, 200f);
        WireStepper("fl_stamina", "btn_fl_stamina_minus", "btn_fl_stamina_plus", 5f, 0f, 500f);
        WireStepper("fl_stamina_regen", "btn_fl_stamina_regen_minus", "btn_fl_stamina_regen_plus", 0.1f, 0f, 10f);
        WireStepper("fl_stamina_speed", "btn_fl_stamina_speed_minus", "btn_fl_stamina_speed_plus", 0.1f, 0f, 10f);
        WireStepper("fl_speed", "btn_fl_speed_minus", "btn_fl_speed_plus", 0.1f, 0f, 50f);
        WireStepper("fl_acceleration", "btn_fl_acceleration_minus", "btn_fl_acceleration_plus", 0.1f, 0f, 50f);
        WireStepper("fl_running_pct", "btn_fl_running_pct_minus", "btn_fl_running_pct_plus", 1f, 0f, 100f);
        WireStepper("fl_back_move", "btn_fl_back_move_minus", "btn_fl_back_move_plus", 1f, 0f, 100f);
        WireStepper("fl_aim", "btn_fl_aim_minus", "btn_fl_aim_plus", 0.5f, 0f, 100f);
        WireStepper("fl_recoil", "btn_fl_recoil_minus", "btn_fl_recoil_plus", 0.5f, 0f, 100f);
        WireStepper("fl_reload", "btn_fl_reload_minus", "btn_fl_reload_plus", 0.1f, 0f, 10f);
        WireStepper("fl_vision", "btn_fl_vision_minus", "btn_fl_vision_plus", 1f, 0f, 100f);
        WireStepper("fl_injured", "btn_fl_injured_minus", "btn_fl_injured_plus", 1f, 0f, 100f);

    }
    void WireStepper(string floatFieldName, string minusBtnName, string plusBtnName, float step, float min, float max)
    {
        var ff = _root.Q<FloatField>(floatFieldName);
        var minus = _root.Q<Button>(minusBtnName);
        var plus = _root.Q<Button>(plusBtnName);
        if (ff == null || minus == null || plus == null) return;

        void ClampAndSet(float v) => ff.value = Mathf.Clamp(v, min, max);

        minus.RegisterCallback<ClickEvent>(_ => ClampAndSet(ff.value - step));
        plus.RegisterCallback<ClickEvent>(_ => ClampAndSet(ff.value + step));
    }
    private void Update()
    {
        if (_presentedAnimator != null && _presentedAnimator.isActiveAndEnabled
            && _presentedAnimator.runtimeAnimatorController != null)
        {
            // Slowly oscillate between pistol idle (0) and pistol run (1)
            float blend = Mathf.PingPong(Time.time / 5f, 1f);
            _presentedAnimator.SetFloat("Blend", blend);
        }

        // Pulse accent line only on active thumb
        if (_activePlayerBtn != null && _accentLines.TryGetValue(_activePlayerBtn, out var activeAccent))
        {
            _pulseTime += Time.deltaTime;
            float opacity = 0.3f + 0.7f * (0.5f + 0.5f * Mathf.Sin(_pulseTime * 3.5f));
            activeAccent.style.opacity = opacity;
        }
    }

    private void SetActivePlayerButton(Button btn)
    {
        // Deactivate old — hide accent line
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
        // accent-line opacity for the new active is driven by Update() pulse
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
        // If you have a dedicated scene picker:
        // Loader.Load(Loader.Scene.LevelSelect);

        // Temporary: jump to Level1 directly (rename to your scene):
        UnityEngine.SceneManagement.SceneManager.LoadScene("ChooseWeapon");
    }

    private GameObject LoadModel()
    {
        _presentedAnimator = null;
        var existing = GameObject.Find("PresentedModel");
        if (existing != null)
        {
            Destroy(existing);
        }
        GameObject model = Resources.Load<GameObject>("Models/Player/" + DataHolder.ChosenPlayer.ToString() + "_model");
        GameObject pos = GameObject.Find("CharPositionSpot");
        GameObject character = Instantiate(model, pos.transform.position, Quaternion.identity);
        character.transform.Rotate(0f, -180f, 0f);
        Animator animator = character.GetComponent<Animator>();
        var baseController = Resources.Load<RuntimeAnimatorController>("Animators/IdleController");
        if (_previewAnimConfig != null && _previewAnimConfig.pistolIdle != null)
        {
            var overrideController = new AnimatorOverrideController(baseController);
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrideController.GetOverrides(overrides);
            for (int i = 0; i < overrides.Count; i++)
            {
                var orig = overrides[i].Key;
                if (orig == null) continue;
                AnimationClip replacement = orig.name switch
                {
                    "idle 0h"              => _previewAnimConfig.pistolIdle,
                    "walking 0h"          => _previewAnimConfig.pistolWalk   ?? _previewAnimConfig.pistolIdle,
                    "running 0h"          => _previewAnimConfig.pistolRun    ?? _previewAnimConfig.pistolIdle,
                    "Running Backward 0h" => _previewAnimConfig.pistolRunBackward ?? _previewAnimConfig.pistolRun,
                    _                     => null
                };
                if (replacement != null)
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, replacement);
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

        // Attach weapon to right hand
        AttachPreviewWeapon(character);

        return character;
    }

    private void AttachPreviewWeapon(GameObject character)
    {
        // Clean up any existing preview weapon
        var existingWeapon = character.transform.Find("PreviewWeapon");
        if (existingWeapon != null) Destroy(existingWeapon.gameObject);

        // Load WPN_AP85 (pistol)
        GameObject weaponPrefab = Resources.Load<GameObject>("Models/Weapons/WPN_AP85");
        if (weaponPrefab == null) return;

        // Find right hand bone (handles mixamorig1, mixamorig2, etc.)
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
        if (choosePlayer != null) _root.styleSheets.Add(choosePlayer); // added LAST to override
    }


    private void ClickPlay()
    {
        playerConfig = PlayerConfigSingleton.Instance.PlayerConfig;
        //SceneManager.LoadScene("ChooseWeapon");
        Loader.Load(Loader.Scene.ChooseWeapon);
        PlayerConfigSingleton.Instance.SaveConfigToFile();
    }

    void ClickAssignData(PlayerEnum player)
    {
        DataHolder.ChosenPlayer = player;
        playerConfig = PlayerConfigSingleton.Instance.PlayerConfig;
        LoadSettingsToUI();
        LoadModel();
        SetActivePlayerButton(GetButtonForPlayer(player));
    }
    void LoadSettingsToUI()
    {
        _fl_health.value = playerConfig.health;
        _fl_weight.value = playerConfig.weight;
        _fl_strength.value = playerConfig.strength;
        _fl_stamina.value = playerConfig.stamina;
        _fl_stamina_regen.value = playerConfig.stamina;
        _fl_stamina_speed.value = playerConfig.staminaRegenSpeed;
        _fl_speed.value = playerConfig.speed;
        _fl_acceleration.value = playerConfig.acceleration;
        _fl_running_pct.value = playerConfig.RunningSpeed_pct;
        _fl_running_bck.value = playerConfig.BackMovementPenalty_pct;
        //stfl_precision.value = playerConfig.aimingPrecision;
        _fl_recoil.value = playerConfig.recoilReduction;
        _fl_reload_speed.value = playerConfig.reloadSpeed;
        _fl_vision.value = playerConfig.vision;
        _fl_injured_penalty.value = playerConfig.InjuredPenalty;

        // Perk section
        string perkName = !string.IsNullOrEmpty(playerConfig.PerkName)
            ? playerConfig.PerkName
            : (DefaultPerkNames.TryGetValue(DataHolder.ChosenPlayer, out var def) ? def : "—");
        if (_lbl_perk_name != null) _lbl_perk_name.text = perkName;
        if (_lbl_perk_desc != null) _lbl_perk_desc.text = playerConfig.PerkDescription ?? "";

        // Level label on the active thumb button
        if (_activePlayerBtn != null && _levelLabels.TryGetValue(_activePlayerBtn, out var lvlLabel))
            lvlLabel.text = $"LVL {playerConfig.Level}";
    }
    public void ChoosePlayer(int index)
    {
        if (index == 0)
        {
            DataHolder.ChosenPlayer = PlayerEnum.Swat;
            player = PlayerEnum.Swat;

        }
        if (index == 1)
        {
            DataHolder.ChosenPlayer = PlayerEnum.BusinessGirl;
            player = PlayerEnum.BusinessGirl;
        }
        if (index == 2)
        {
            DataHolder.ChosenPlayer = PlayerEnum.Dr;
            player = PlayerEnum.Dr;
        }
        if (index == 3)
        {
            DataHolder.ChosenPlayer = PlayerEnum.Jennifer;
            player = PlayerEnum.Jennifer;
        }
        if (index == 4)
        {
            DataHolder.ChosenPlayer = PlayerEnum.Solider;
            player = PlayerEnum.Solider;
        }
        if (index == 5)
        {
            DataHolder.ChosenPlayer = PlayerEnum.GreenHat_basic;
            player = PlayerEnum.GreenHat_basic;
        }
    }

    private void RandomizeStatBars()
    {
        var statsPanel = _root.Q<VisualElement>("stats");
        if (statsPanel == null) return;
        foreach (var row in statsPanel.Query<VisualElement>(className: "stat-row").ToList())
        {
            var fill = row.Q<VisualElement>(className: "stat-bar-fill");
            if (fill == null) continue;
            float pct = UnityEngine.Random.Range(30f, 70f);
            fill.style.width = Length.Percent(pct);
        }
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
