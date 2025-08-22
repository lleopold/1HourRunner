using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;


public class UIT_ChoosePlayer : MonoBehaviour
{
    [SerializeField] public PlayerEnum player;
    [SerializeField] public WeaponEnum weapon;
    [SerializeField] private UIDocument _uiDocument;
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
    private Button _btn_solider;
    private Button _btn_green_hat_basic;
    private Button _btn_play;




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
        _btn_solider = _root.Q<Button>("btn_solider");
        _btn_green_hat_basic = _root.Q<Button>("btn_green_hat");
        _btn_play = _root.Q<Button>("btn_play");
        _btn_business_girl.RegisterCallback<ClickEvent>(ev => ClickAssignData(PlayerEnum.BusinessGirl));
        _btn_dr.RegisterCallback<ClickEvent>(ev => ClickAssignData(PlayerEnum.Dr));
        _btn_jennifer.RegisterCallback<ClickEvent>(ev => ClickAssignData(PlayerEnum.Jennifer));
        _btn_swat.RegisterCallback<ClickEvent>(ev => ClickAssignData(PlayerEnum.Swat));
        _btn_steel.RegisterCallback<ClickEvent>(ev => ClickAssignData(PlayerEnum.Jackson_Steel_Reynolds));
        _btn_solider.RegisterCallback<ClickEvent>(ev => ClickAssignData(PlayerEnum.Solider));
        _btn_green_hat_basic.RegisterCallback<ClickEvent>(ev => ClickAssignData(PlayerEnum.GreenHat_basic));
        _btn_play.RegisterCallback<ClickEvent>(ev => ClickPlay());



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



        LoadSettingsToUI();
    }

    private static GameObject LoadModel()
    {
        if (GameObject.Find("PresentedModel") != null)
        {
            Destroy(GameObject.Find("PresentedModel"));
        }
        GameObject model = Resources.Load<GameObject>("Models/" + DataHolder.ChosenPlayer.ToString() + "_model");
        GameObject pos = GameObject.Find("CharPositionSpot");
        GameObject character = Instantiate(model, pos.transform.position, Quaternion.identity);
        character.transform.Rotate(0f, -180f, 0f);
        Animator animator = character.GetComponent<Animator>();
        animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Animators/IdleController");
        character.name = "PresentedModel";

        return character;
    }
    void AttachWeaponSelectionStyles()
    {
        // Load "Assets/Resources/UI/WeaponSelection.uss"
        var ss = Resources.Load<StyleSheet>("UI/WeaponSelection");
        if (ss == null)
        {
            Debug.LogError("UI/WeaponSelection.uss not found in Resources.");
            return;
        }

        // Add the stylesheet to the root visual element
        _root.styleSheets.Add(ss);

        // Add the root class used by the theme (matches .weapon-screen in the USS)
        _root.AddToClassList("weapon-screen");
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
        //_fl_precision.value = playerConfig.aimingPrecision;
        _fl_recoil.value = playerConfig.recoilReduction;
        _fl_reload_speed.value = playerConfig.reloadSpeed;
        _fl_vision.value = playerConfig.vision;
        _fl_injured_penalty.value = playerConfig.InjuredPenalty;
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
