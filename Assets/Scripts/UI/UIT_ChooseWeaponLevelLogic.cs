using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class UIT_ChooseWeaponLevelLogic : MonoBehaviour
{
    private VisualElement _root;
    [SerializeField] public WeaponEnum _weapon;
    [SerializeField] private UIDocument _uiDocument;
    private WeaponConfig _weaponConfig;
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
    private Button _btn_pistol;
    private Button _btn_revolver;
    private Button _btn_shotgun;
    private Button _btn_m4;

    private Button _btn_WPN_AP85;
    private Button _btn_WPN_MK18;
    private Button _btn_WPN_P350;
    private Button _btn_WPN_Revolver;
    private Button _btn_WPN_Hunter85;
    private Button _btn_WPN_M4;
    private Button _btn_WPN_M9;
    private Button _btn_WPN_SMG5;
    private Button _btn_WPN_PT8;
    private Button _btn_WPN_R90;
    private Button _btn_WPN_FBS;
    private Button _btn_WPN_Eder22;
    private Button _btn_WPN_DT22;
    private Button _btn_WPN_CV47;
    private Button _btn_WPN_C1911;
    private Button _btn_WPN_M16;
    private Button _btn_WPN_KM4;
    private Button _btn_WPN_590A1;
    private Button _btn_WPN_CX8;


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

        _btn_WPN_AP85 = _root.Q<Button>("btn_WPN_AP85");
        _btn_WPN_AP85.text = "AP85";
        _btn_WPN_MK18 = _root.Q<Button>("btn_WPN_MK18");
        _btn_WPN_MK18.text = "MK18";
        _btn_WPN_P350 = _root.Q<Button>("btn_WPN_P350");
        _btn_WPN_P350.text = "P350";
        _btn_WPN_Revolver = _root.Q<Button>("btn_WPN_Revolver");
        _btn_WPN_Revolver.text = "Revolver";
        _btn_WPN_Hunter85 = _root.Q<Button>("btn_WPN_Hunter85");
        _btn_WPN_Hunter85.text = "Hunter85";
        _btn_WPN_M4 = _root.Q<Button>("btn_WPN_M4");
        _btn_WPN_M4.text = "M4";
        _btn_WPN_M9 = _root.Q<Button>("btn_WPN_M9");
        _btn_WPN_M9.text = "M9";
        _btn_WPN_SMG5 = _root.Q<Button>("btn_WPN_SMG5");
        _btn_WPN_SMG5.text = "SMG5";
        _btn_WPN_PT8 = _root.Q<Button>("btn_WPN_PT8");
        _btn_WPN_PT8.text = "PT8";
        _btn_WPN_R90 = _root.Q<Button>("btn_WPN_R90");
        _btn_WPN_R90.text = "R90";
        _btn_WPN_FBS = _root.Q<Button>("btn_WPN_FBS");
        _btn_WPN_FBS.text = "FBS";
        _btn_WPN_Eder22 = _root.Q<Button>("btn_WPN_Eder22");
        _btn_WPN_Eder22.text = "Eder22";
        _btn_WPN_DT22 = _root.Q<Button>("btn_WPN_DT22");
        _btn_WPN_DT22.text = "DT22";
        _btn_WPN_CV47 = _root.Q<Button>("btn_WPN_CV47");
        _btn_WPN_CV47.text = "CV47";
        _btn_WPN_C1911 = _root.Q<Button>("btn_WPN_C1911");
        _btn_WPN_C1911.text = "C1911";
        _btn_WPN_M16 = _root.Q<Button>("btn_WPN_M16");
        _btn_WPN_M16.text = "M16";
        _btn_WPN_KM4 = _root.Q<Button>("btn_WPN_KM4");
        _btn_WPN_KM4.text = "KM4";
        _btn_WPN_590A1 = _root.Q<Button>("btn_WPN_590A1");
        _btn_WPN_590A1.text = "590A1";
        _btn_WPN_CX8 = _root.Q<Button>("btn_WPN_CX8");
        _btn_WPN_CX8.text = "CX8";



        //_btn_pistol = _root.Q<Button>("btn_pistol");
        //_btn_revolver = _root.Q<Button>("btn_revolver");
        //_btn_shotgun = _root.Q<Button>("btn_shotgun");
        //_btn_m4 = _root.Q<Button>("btn_m4");


        _btn_WPN_AP85.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_AP85));
        _btn_WPN_MK18.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_MK18));
        _btn_WPN_P350.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_P350));
        _btn_WPN_Revolver.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_Revolver));
        _btn_WPN_Hunter85.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_Hunter85));
        _btn_WPN_M4.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_M4));
        _btn_WPN_M9.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_M9));
        _btn_WPN_SMG5.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_SMG5));
        _btn_WPN_PT8.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_PT8));
        _btn_WPN_R90.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_R90));
        _btn_WPN_FBS.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_FBS));
        _btn_WPN_Eder22.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_Eder22));
        _btn_WPN_DT22.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_DT22));
        _btn_WPN_CV47.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_CV47));
        _btn_WPN_C1911.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_C1911));
        _btn_WPN_M16.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_M16));
        _btn_WPN_KM4.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_KM4));
        _btn_WPN_590A1.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_590A1));
        _btn_WPN_CX8.RegisterCallback<ClickEvent>(ev => ClickAssignData(WeaponEnum.WPN_CX8));

        _btn_play.RegisterCallback<ClickEvent>(ev => ClickPlay());
        _btn_back.RegisterCallback<ClickEvent>(ev => ClickBack());

        _damage.RegisterCallback<ChangeEvent<float>>(ev => WeaponConfigSingleton.Instance.WeaponConfig.Damage = ev.newValue);
        _fire_rate.RegisterCallback<ChangeEvent<float>>(ev => WeaponConfigSingleton.Instance.WeaponConfig.FireRate = ev.newValue);
        _damage_flustuation.RegisterCallback<ChangeEvent<float>>(ev => WeaponConfigSingleton.Instance.WeaponConfig.DamageFluctuation = ev.newValue);
        _clip_size.RegisterCallback<ChangeEvent<float>>(ev => WeaponConfigSingleton.Instance.WeaponConfig.ClipSize = (int)ev.newValue);
        _precision.RegisterCallback<ChangeEvent<float>>(ev => WeaponConfigSingleton.Instance.WeaponConfig.Precision = ev.newValue);
        _reload_time.RegisterCallback<ChangeEvent<float>>(ev => WeaponConfigSingleton.Instance.WeaponConfig.ReloadTime = ev.newValue);
        _simultanious_bullets.RegisterCallback<ChangeEvent<float>>(ev => WeaponConfigSingleton.Instance.WeaponConfig.SimultaniousBullets = (int)ev.newValue);
        _critical_chance.RegisterCallback<ChangeEvent<float>>(ev => WeaponConfigSingleton.Instance.WeaponConfig.CritChance = ev.newValue);
        _stagger.RegisterCallback<ChangeEvent<float>>(ev => WeaponConfigSingleton.Instance.WeaponConfig.Stagger = ev.newValue);
        _recoil.RegisterCallback<ChangeEvent<float>>(ev => WeaponConfigSingleton.Instance.WeaponConfig.Recoil = ev.newValue);

        if (DataHolder.chosenWeapon == null)
        {
            _weapon = WeaponEnum.WPN_M4;
        }
        else
        {
            _weapon = DataHolder.chosenWeapon;
        }
        GameObject weapon = LoadWeapon();
        // Simulate a click on _btn_pistol

        WireWeaponButtonsSelectedState();
        //ApplyStatsRowClass();
        SetupFocusAndBack();
    }

    private void Start()
    {
        ClickAssignData(WeaponEnum.WPN_AP85);
    }

    void Update()
    {
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
        {
            b.Encapsulate(r.bounds);
        }
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
        //SceneManager.LoadScene("Level_1");
        Loader.Load(Loader.Scene.Level_1);

    }

    private GameObject LoadWeapon()
    {
        GameObject weaponToDelete = GameObject.Find("PresentedWeapon");
        if (weaponToDelete != null)
        {
            Destroy(weaponToDelete);
        }
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
            || _weapon == WeaponEnum.WPN_DT22
            )
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
        {
            DataHolder.weaponType = WeaponType.H2;
        }
        else
        {
            DataHolder.weaponType = WeaponType.H1;
        }
        LoadSettingsToUI();
        LoadWeapon();
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
    // Call this in Awake after you Q() all weapon buttons
    void WireWeaponButtonsSelectedState()
    {
        // put all your weapon buttons here
        var weaponButtons = new[] {
        _btn_WPN_AP85, _btn_WPN_MK18, _btn_WPN_P350, _btn_WPN_Revolver, _btn_WPN_Hunter85,
        _btn_WPN_M4, _btn_WPN_M9, _btn_WPN_SMG5, _btn_WPN_PT8, _btn_WPN_R90, _btn_WPN_FBS,
        _btn_WPN_Eder22, _btn_WPN_DT22, _btn_WPN_CV47, _btn_WPN_C1911, _btn_WPN_M16,
        _btn_WPN_KM4, _btn_WPN_590A1, _btn_WPN_CX8
    };

        foreach (var b in weaponButtons)
        {
            b.RegisterCallback<ClickEvent>(_ =>
            {
                foreach (var x in weaponButtons) x?.RemoveFromClassList("selected");
                b.AddToClassList("selected");
                b.Focus(); // good for keyboard/gamepad
                           // your existing logic that swaps weapon/config here
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
            var row = field?.parent; // assuming Label + FloatField share a parent
            row?.AddToClassList("stats-row");
        }
    }
    void SetupFocusAndBack()
    {
        // Default focus
        _root.schedule.Execute(() => _btn_WPN_AP85?.Focus()).StartingIn(0);

        // Escape key Back button
        _root.RegisterCallback<KeyDownEvent>(e =>
        {
            if (e.keyCode == KeyCode.Escape)
            {
                _btn_back?.SendEvent(new ClickEvent());
            }
        });
    }
    private void ClickChooseLevel()
    {
        Loader.Load(Loader.Scene.ChooseLevel);
        // or SceneManager.LoadScene("ChooseLevel");
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