using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class UIT_ChooseWeaponLevelLogic : MonoBehaviour
{
    private VisualElement _root;
    [SerializeField] public WeaponEnum _weapon;
    [SerializeField] private UIDocument _uiDocument;
    private WeaponConfig _weaponConfig;
    private Coroutine _slideCoroutine;
    private bool _isSliding;
    private WeaponButton _activeWeaponButton;
    private Button _btn_play;
    private Button _btn_back;
    private FloatField _damage;
    private FloatField _fire_rate;
    private FloatField _damage_flustuation;
    private FloatField _clip_size;
    private FloatField _precision;
    private FloatField _reload_time;
    private FloatField _simultanious_bullets;
    private FloatField _critical_chance;
    private FloatField _stagger;
    private FloatField _recoil;

    // Changed: Weapon buttons now WeaponButton
    private WeaponButton _btn_WPN_AP85;
    private WeaponButton _btn_WPN_MK18;
    private WeaponButton _btn_WPN_P350;
    private WeaponButton _btn_WPN_Revolver;
    private WeaponButton _btn_WPN_Hunter85;
    private WeaponButton _btn_WPN_M4;
    private WeaponButton _btn_WPN_M9;
    private WeaponButton _btn_WPN_SMG5;
    private WeaponButton _btn_WPN_PT8;
    private WeaponButton _btn_WPN_R90;
    private WeaponButton _btn_WPN_FBS;
    private WeaponButton _btn_WPN_Eder22;
    private WeaponButton _btn_WPN_DT22;
    private WeaponButton _btn_WPN_CV47;
    private WeaponButton _btn_WPN_C1911;
    private WeaponButton _btn_WPN_M16;
    private WeaponButton _btn_WPN_KM4;
    private WeaponButton _btn_WPN_590A1;
    private WeaponButton _btn_WPN_CX8;

    void Awake()
    {
        _root = _uiDocument.rootVisualElement;
        _btn_play = _root.Q<Button>("btn_play");
        _btn_back = _root.Q<Button>("btn_back");
        _damage = _root.Q<FloatField>("fl_damage");
        _fire_rate = _root.Q<FloatField>("fl_fire_rate");
        _damage_flustuation = _root.Q<FloatField>("fl_damage_fluctuation");
        _clip_size = _root.Q<FloatField>("fl_clip_size");
        _precision = _root.Q<FloatField>("fl_precision");
        _reload_time = _root.Q<FloatField>("fl_reload_time");
        _simultanious_bullets = _root.Q<FloatField>("fl_simultanious_bullets");
        _critical_chance = _root.Q<FloatField>("fl_critical_chance");
        _stagger = _root.Q<FloatField>("fl_stagger");
        _recoil = _root.Q<FloatField>("fl_recoil");

        var btnChooseLevel = _root.Q<Button>("btn_choose_level");
        btnChooseLevel?.RegisterCallback<ClickEvent>(_ => Loader.Load(Loader.Scene.ChooseLevel));

        // Query WeaponButton instead of Button
        _btn_WPN_AP85 = _root.Q<WeaponButton>("btn_WPN_AP85");
        _btn_WPN_MK18 = _root.Q<WeaponButton>("btn_WPN_MK18");
        _btn_WPN_P350 = _root.Q<WeaponButton>("btn_WPN_P350");
        _btn_WPN_Revolver = _root.Q<WeaponButton>("btn_WPN_Revolver");
        _btn_WPN_Hunter85 = _root.Q<WeaponButton>("btn_WPN_Hunter85");
        _btn_WPN_M4 = _root.Q<WeaponButton>("btn_WPN_M4");
        _btn_WPN_M9 = _root.Q<WeaponButton>("btn_WPN_M9");
        _btn_WPN_SMG5 = _root.Q<WeaponButton>("btn_WPN_SMG5");
        _btn_WPN_PT8 = _root.Q<WeaponButton>("btn_WPN_PT8");
        _btn_WPN_R90 = _root.Q<WeaponButton>("btn_WPN_R90");
        _btn_WPN_FBS = _root.Q<WeaponButton>("btn_WPN_FBS");
        _btn_WPN_Eder22 = _root.Q<WeaponButton>("btn_WPN_Eder22");
        _btn_WPN_DT22 = _root.Q<WeaponButton>("btn_WPN_DT22");
        _btn_WPN_CV47 = _root.Q<WeaponButton>("btn_WPN_CV47");
        _btn_WPN_C1911 = _root.Q<WeaponButton>("btn_WPN_C1911");
        _btn_WPN_M16 = _root.Q<WeaponButton>("btn_WPN_M16");
        _btn_WPN_KM4 = _root.Q<WeaponButton>("btn_WPN_KM4");
        _btn_WPN_590A1 = _root.Q<WeaponButton>("btn_WPN_590A1");
        _btn_WPN_CX8 = _root.Q<WeaponButton>("btn_WPN_CX8");
        print("Weapon buttons found: ");
        _root.Query<WeaponButton>().ForEach(b => Debug.Log("Found WeaponButton name='" + b.name + "'"));
        print("Total Weapon buttons found: " + _root.Query<WeaponButton>().ToList().Count);

        // (Optional) If you want quick labels you could set inner label via public API you add later.

        // Event wiring (RegisterCallback still works because WeaponButton is a VisualElement)
        _btn_WPN_AP85?.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_AP85));
        _btn_WPN_MK18?.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_MK18));
        _btn_WPN_P350?.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_P350));
        _btn_WPN_Revolver?.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_Revolver));
        _btn_WPN_Hunter85?.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_Hunter85));
        _btn_WPN_M4?.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_M4));
        _btn_WPN_M9?.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_M9));
        _btn_WPN_SMG5?.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_SMG5));
        _btn_WPN_PT8?.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_PT8));
        _btn_WPN_R90?.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_R90));
        _btn_WPN_FBS?.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_FBS));
        _btn_WPN_Eder22?.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_Eder22));
        _btn_WPN_DT22?.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_DT22));
        _btn_WPN_CV47?.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_CV47));
        _btn_WPN_C1911?.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_C1911));
        _btn_WPN_M16?.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_M16));
        _btn_WPN_KM4?.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_KM4));
        _btn_WPN_590A1?.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_590A1));
        _btn_WPN_CX8?.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_CX8));

        _btn_play?.RegisterCallback<ClickEvent>(ev => ClickPlay());
        _btn_back?.RegisterCallback<ClickEvent>(ev => ClickBack());

        _damage?.RegisterCallback<ChangeEvent<float>>(ev => WeaponConfigSingleton.Instance.WeaponConfig.Damage = ev.newValue);
        _fire_rate?.RegisterCallback<ChangeEvent<float>>(ev => WeaponConfigSingleton.Instance.WeaponConfig.FireRate = ev.newValue);
        _damage_flustuation?.RegisterCallback<ChangeEvent<float>>(ev => WeaponConfigSingleton.Instance.WeaponConfig.DamageFluctuation = ev.newValue);
        _clip_size?.RegisterCallback<ChangeEvent<float>>(ev => WeaponConfigSingleton.Instance.WeaponConfig.ClipSize = (int)ev.newValue);
        _precision?.RegisterCallback<ChangeEvent<float>>(ev => WeaponConfigSingleton.Instance.WeaponConfig.Precision = ev.newValue);
        _reload_time?.RegisterCallback<ChangeEvent<float>>(ev => WeaponConfigSingleton.Instance.WeaponConfig.ReloadTime = ev.newValue);
        _simultanious_bullets?.RegisterCallback<ChangeEvent<float>>(ev => WeaponConfigSingleton.Instance.WeaponConfig.SimultaniousBullets = (int)ev.newValue);
        _critical_chance?.RegisterCallback<ChangeEvent<float>>(ev => WeaponConfigSingleton.Instance.WeaponConfig.CritChance = ev.newValue);
        _stagger?.RegisterCallback<ChangeEvent<float>>(ev => WeaponConfigSingleton.Instance.WeaponConfig.Stagger = ev.newValue);
        _recoil?.RegisterCallback<ChangeEvent<float>>(ev => WeaponConfigSingleton.Instance.WeaponConfig.Recoil = ev.newValue);

        if (DataHolder.chosenWeapon == null)
            _weapon = WeaponEnum.WPN_M4;
        else
            _weapon = DataHolder.chosenWeapon;

        LoadWeapon();
        WireWeaponButtonsSelectedState();
        SetupFocusAndBack();
    }

    private void Start()
    {
        ClickAssignData(WeaponEnum.WPN_AP85);
    }

    void Update()
    {
        // Tick active weapon button for pulse animation
        _activeWeaponButton?.Tick(Time.deltaTime);

        if (_isSliding) return;
        GameObject weapon = GameObject.Find("PresentedWeapon");
        if (weapon != null)
        {
            Camera.main.transform.LookAt(weapon.transform);
            weapon.transform.Rotate(0.0f, 0.0f, 0.1f);
        }
    }

    private void ClickBack()
    {
        SceneManager.LoadScene("ChoosePlayer");
    }

    internal static Bounds GetBound(GameObject go)
    {
        Bounds b = new Bounds(go.transform.position, Vector3.zero);
        var rList = go.GetComponentsInChildren(typeof(Renderer));
        foreach (Renderer r in rList)
            b.Encapsulate(r.bounds);
        return b;
    }

    internal static void ZoomFit(Camera c, GameObject go, bool ViewFromRandomDirecion = false)
    {
        Bounds b = GetBound(go);
        Vector3 max = b.size;
        float radius = Mathf.Max(max.x, Mathf.Max(max.y, max.z));
        float dist = radius / (Mathf.Sin(c.fieldOfView * Mathf.Deg2Rad / 2f));
        Debug.Log("Radius = " + radius + " dist = " + dist);

        Vector3 view_direction = ViewFromRandomDirecion ? UnityEngine.Random.onUnitSphere : c.transform.InverseTransformDirection(Vector3.forward);

        Vector3 pos = view_direction * dist + b.center;
        c.transform.position = pos;
        c.transform.LookAt(b.center);
    }

    private void ClickPlay()
    {
        _weaponConfig = WeaponConfigSingleton.Instance.WeaponConfig;
        WeaponConfigSingleton.Instance.SaveConfigToFile();
        Loader.Load(Loader.Scene.Level_1);
    }

    private GameObject LoadWeapon()
    {
        GameObject weaponToDelete = GameObject.Find("PresentedWeapon");
        if (weaponToDelete != null)
            Destroy(weaponToDelete);

        GameObject model = Resources.Load<GameObject>("Models/Weapons/" + _weapon.ToString());
        GameObject positionSpot = GameObject.Find("WeaponPositionSpot");
        GameObject weapon = Instantiate(model, positionSpot.transform.position, Quaternion.identity);

        weapon.transform.Rotate(-70f, 90f, 0f);
        if (_weapon == WeaponEnum.WPN_MK18
            || _weapon == WeaponEnum.WPN_Hunter85
            || _weapon == WeaponEnum.WPN_M4
            || _weapon == WeaponEnum.WPN_SMG5
            || _weapon == WeaponEnum.WPN_R90
            || _weapon == WeaponEnum.WPN_FBS
            || _weapon == WeaponEnum.WPN_CV47
            || _weapon == WeaponEnum.WPN_M16
            || _weapon == WeaponEnum.WPN_KM4
            || _weapon == WeaponEnum.WPN_590A1
            || _weapon == WeaponEnum.WPN_CX8
            || _weapon == WeaponEnum.WPN_DT22)
        {
            weapon.transform.localScale *= 0.3f;
        }
        weapon.name = "PresentedWeapon";
        return weapon;
    }

    void ClickAssignData(WeaponEnum weaponEnum)
    {
        DataHolder.chosenWeapon = weaponEnum;
        _weapon = weaponEnum;
        _weaponConfig = WeaponConfigSingleton.Instance.WeaponConfig;
        if (weaponEnum == WeaponEnum.WPN_M4 || weaponEnum == WeaponEnum.WPN_M16)
            DataHolder.weaponType = WeaponType.H2;
        else
            DataHolder.weaponType = WeaponType.H1;

        LoadSettingsToUI();
        LoadWeaponWithSlide(weaponEnum);
        ActivateButtonForWeapon(weaponEnum);
    }

    void ActivateButtonForWeapon(WeaponEnum weaponEnum)
    {
        var all = new (WeaponEnum e, WeaponButton b)[] {
            (WeaponEnum.WPN_AP85, _btn_WPN_AP85), (WeaponEnum.WPN_MK18, _btn_WPN_MK18),
            (WeaponEnum.WPN_P350, _btn_WPN_P350), (WeaponEnum.WPN_Revolver, _btn_WPN_Revolver),
            (WeaponEnum.WPN_Hunter85, _btn_WPN_Hunter85), (WeaponEnum.WPN_M4, _btn_WPN_M4),
            (WeaponEnum.WPN_M9, _btn_WPN_M9), (WeaponEnum.WPN_SMG5, _btn_WPN_SMG5),
            (WeaponEnum.WPN_PT8, _btn_WPN_PT8), (WeaponEnum.WPN_R90, _btn_WPN_R90),
            (WeaponEnum.WPN_FBS, _btn_WPN_FBS), (WeaponEnum.WPN_Eder22, _btn_WPN_Eder22),
            (WeaponEnum.WPN_DT22, _btn_WPN_DT22), (WeaponEnum.WPN_CV47, _btn_WPN_CV47),
            (WeaponEnum.WPN_C1911, _btn_WPN_C1911), (WeaponEnum.WPN_M16, _btn_WPN_M16),
            (WeaponEnum.WPN_KM4, _btn_WPN_KM4), (WeaponEnum.WPN_590A1, _btn_WPN_590A1),
            (WeaponEnum.WPN_CX8, _btn_WPN_CX8)
        };
        _activeWeaponButton = null;
        foreach (var pair in all)
        {
            bool active = pair.e == weaponEnum;
            pair.b?.SetActive(active);
            if (active) _activeWeaponButton = pair.b;
        }
    }

    void LoadWeaponWithSlide(WeaponEnum weaponEnum)
    {
        if (_slideCoroutine != null)
            StopCoroutine(_slideCoroutine);

        GameObject positionSpot = GameObject.Find("WeaponPositionSpot");
        Vector3 center = positionSpot.transform.position;

        GameObject outWeapon = GameObject.Find("PresentedWeapon");

        GameObject model = Resources.Load<GameObject>("Models/Weapons/" + weaponEnum.ToString());
        GameObject inWeapon = Instantiate(model, center, Quaternion.identity);
        inWeapon.transform.Rotate(-70f, 90f, 0f);
        if (weaponEnum == WeaponEnum.WPN_MK18
            || weaponEnum == WeaponEnum.WPN_Hunter85
            || weaponEnum == WeaponEnum.WPN_M4
            || weaponEnum == WeaponEnum.WPN_SMG5
            || weaponEnum == WeaponEnum.WPN_R90
            || weaponEnum == WeaponEnum.WPN_FBS
            || weaponEnum == WeaponEnum.WPN_CV47
            || weaponEnum == WeaponEnum.WPN_M16
            || weaponEnum == WeaponEnum.WPN_KM4
            || weaponEnum == WeaponEnum.WPN_590A1
            || weaponEnum == WeaponEnum.WPN_CX8
            || weaponEnum == WeaponEnum.WPN_DT22)
        {
            inWeapon.transform.localScale *= 0.3f;
        }
        inWeapon.name = "PresentedWeapon";

        Debug.Log("[Slide] Calling StartCoroutine. outWeapon=" + (outWeapon != null ? outWeapon.name : "NULL"));
        _slideCoroutine = StartCoroutine(SlideWeaponTransition(outWeapon, inWeapon, center));
    }

    private System.Collections.IEnumerator SlideWeaponTransition(GameObject outWeapon, GameObject inWeapon, Vector3 center)
    {
        _isSliding = true;
        Debug.Log("[Slide] COROUTINE STARTED. outWeapon=" + (outWeapon != null ? outWeapon.name : "NULL") + " inWeapon=" + (inWeapon != null ? inWeapon.name : "NULL"));

        float duration = 0.4f;
        float elapsed = 0f;

        // Slide along camera's right so it's always visible on screen
        Vector3 camRight = Camera.main != null ? Camera.main.transform.right : Vector3.right;
        Vector3 slideOffset = camRight * 5f;

        Vector3 outEnd = center - slideOffset;
        Vector3 inStart = center + slideOffset;
        inWeapon.transform.position = inStart;

        // Freeze camera on center so it doesn't follow and cancel the slide
        Quaternion frozenCamRot = Camera.main != null ? Camera.main.transform.rotation : Quaternion.identity;
        if (Camera.main != null)
            Camera.main.transform.LookAt(center);

        int frame = 0;
        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            if (outWeapon != null)
                outWeapon.transform.position = Vector3.Lerp(center, outEnd, t);
            inWeapon.transform.position = Vector3.Lerp(inStart, center, t);
            if (frame == 0)
                Debug.Log("[Slide] FIRST YIELD frame. t=" + t + " inPos=" + inWeapon.transform.position + " inStart=" + inStart + " center=" + center);
            elapsed += Time.deltaTime;
            frame++;
            yield return null;
        }

        Debug.Log("[Slide] LOOP DONE after " + frame + " frames.");
        inWeapon.transform.position = center;
        if (outWeapon != null)
            Destroy(outWeapon);

        _isSliding = false;
        _slideCoroutine = null;
    }

    void LoadSettingsToUI()
    {
        _damage.value = _weaponConfig.Damage;
        _fire_rate.value = _weaponConfig.FireRate;
        _damage_flustuation.value = _weaponConfig.DamageFluctuation;
        _clip_size.value = _weaponConfig.ClipSize;
        _precision.value = _weaponConfig.Precision;
        _reload_time.value = _weaponConfig.ReloadTime;
        _simultanious_bullets.value = _weaponConfig.SimultaniousBullets;
        _critical_chance.value = _weaponConfig.CritChance;
        _stagger.value = _weaponConfig.Stagger;
        _recoil.value = _weaponConfig.Recoil;
    }

    void WireWeaponButtonsSelectedState()
    {
        var weaponButtons = new WeaponButton[] {
            _btn_WPN_AP85, _btn_WPN_MK18, _btn_WPN_P350, _btn_WPN_Revolver, _btn_WPN_Hunter85,
            _btn_WPN_M4, _btn_WPN_M9, _btn_WPN_SMG5, _btn_WPN_PT8, _btn_WPN_R90, _btn_WPN_FBS,
            _btn_WPN_Eder22, _btn_WPN_DT22, _btn_WPN_CV47, _btn_WPN_C1911, _btn_WPN_M16,
            _btn_WPN_KM4, _btn_WPN_590A1, _btn_WPN_CX8
        };

        foreach (var b in weaponButtons)
        {
            if (b == null) continue;
            b.RegisterCallback<ClickEvent>(_ =>
            {
                foreach (var x in weaponButtons) x?.SetActive(false);
                b.SetActive(true);
                b.Focus();
            });
        }
    }

    void ApplyStatsRowClass()
    {
        string[] names = {
            "fl_damage","fl_fire_rate","fl_damage_fluctuation","fl_clip_size","fl_precision",
            "fl_reload_time","fl_simultanious_bullets","fl_critical_chance","fl_stagger","fl_recoil"
        };
        foreach (var n in names)
        {
            var field = _root.Q<FloatField>(n);
            var row = field?.parent;
            row?.AddToClassList("stats-row");
        }
    }

    void SetupFocusAndBack()
    {
        _root.schedule.Execute(() => _btn_WPN_AP85?.Focus()).StartingIn(0);

        _root.RegisterCallback<KeyDownEvent>(e =>
        {
            if (e.keyCode == KeyCode.Escape)
                _btn_back?.SendEvent(new ClickEvent());
        });
    }

    private void ClickChooseLevel()
    {
        Loader.Load(Loader.Scene.ChooseLevel);
    }
}

public enum WeaponEnum
{
    WPN_AP85,
    WPN_MK18,
    WPN_P350,
    WPN_Revolver,
    WPN_Hunter85,
    WPN_M4,
    WPN_M9,
    WPN_SMG5,
    WPN_PT8,
    WPN_R90,
    WPN_FBS,
    WPN_Eder22,
    WPN_DT22,
    WPN_CV47,
    WPN_C1911,
    WPN_M16,
    WPN_KM4,
    WPN_590A1,
    WPN_CX8
}