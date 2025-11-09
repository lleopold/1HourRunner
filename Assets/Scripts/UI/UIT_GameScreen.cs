using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class UIT_GameScreen : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;
    private VisualElement _root;
    private ProgressBar _healthBar;
    private ProgressBar _staminaBar;
    private ProgressBar _xpBar;
    private ProgressBar _ammoBar;
    public Label _bullets;
    private Button _btn_exit;
    private Button _btn_mainScreen;
    private Button _btn_spawn_zombie;
    private Button _btn_end_game;
    private Button _btn_level_up;
    private Button _btn_take_damage;
    private float _totalXP;
    private int _level;
    private float[] _levelBoundaries;


    private void SetLevelBoundaries()
    {
        _levelBoundaries = new float[1000];
        for (int i = 0; i < 1000; i++)
        {
            if (i == 0)
            {
                _levelBoundaries[i] = 100;
                continue;
            }
            _levelBoundaries[i] = _levelBoundaries[i - 1] * 1.5f;
        }
    }
    private void OnEnable()
    {
        SetLevelBoundaries();
        _level = 1;
        _root = _uiDocument.rootVisualElement;
        _healthBar = _root.Q<ProgressBar>("pb_health");
        _staminaBar = _root.Q<ProgressBar>("pb_stamina");
        _xpBar = _root.Q<ProgressBar>("pb_xp");
        _ammoBar = _root.Q<ProgressBar>("pb_reload");
        _bullets = _root.Q<Label>("lab_bullets");
        _btn_exit = _root.Q<Button>("bt_exit");
        _btn_mainScreen = _root.Q<Button>("bt_main_screen");
        _btn_spawn_zombie = _root.Q<Button>("bt_zombie");
        _btn_end_game = _root.Q<Button>("bt_end_game");
        _btn_level_up = _root.Q<Button>("bt_level_up");
        _btn_take_damage = _root.Q<Button>("bt_take_damage");

        _btn_exit.RegisterCallback<ClickEvent>(GoToExitApplication);
        _btn_mainScreen.RegisterCallback<ClickEvent>(GoToMainScreen);
        _btn_spawn_zombie.RegisterCallback<ClickEvent>(GoToSpawnZombie);
        _btn_end_game.RegisterCallback<ClickEvent>(GoToEndGame);
        _btn_level_up.RegisterCallback<ClickEvent>(GoToPowerUp);
        _btn_take_damage.RegisterCallback<ClickEvent>(TestBloodHUD);
        _totalXP = 0;

        var actualBar = _root.Q(className: "unity-progress-bar__progress");
        actualBar.style.backgroundColor = Color.yellow;
        _xpBar.value = 0;
    }

    private void TestBloodHUD(ClickEvent evt)
    {
        // Test BloodHUD with random intensity
        if (BloodHUD.Instance != null)
        {
            float intensity = Random.Range(0.3f, 2.0f);
            BloodHUD.Instance.Hit(intensity);
            Debug.Log($"BloodHUD triggered with intensity: {intensity}");
        }
        else
        {
            Debug.LogError("Pih2 OPAAAA IDEMOOOOO!!!" + "Make sure BloodHUD component is in the scene.!");
        }
    }
    void OnScriptHotReload()
    {
        //do whatever you want to do with access to instance via 'this'
        Debug.LogError("NECE !!!! BloodHUD.Instance is null!!!?!?!?!?! ");
    }

    static void OnScriptHotReloadNoInstance()
    {
        //do whatever you want to do without instance
        //useful if you've added brand new type
        //or want to simply execute some code without |any instance created.
        //Like reload scene, call test function etc
        Debug.LogError("Hotreload smece");
    }

    private void GoToPowerUp(ClickEvent evt)
    {
        //GameObject player = GameObject.Find("Player");
        //player.GetComponent<PlayerControllerInput>().AddCoins(100);
        Debug.LogError("NOT Added 100");
    }

    private void GoToEndGame(ClickEvent evt)
    {
        GameObject scriptLogic = GameObject.Find("UIEndGame");
        if (scriptLogic != null)
        {
            scriptLogic.GetComponent<UIT_EndGamePopUp>().SetEndGamePopUp("Game Over", "10:00", "100", "1000");
            scriptLogic.GetComponent<UIT_EndGamePopUp>()._root.visible = true;
        }
    }

    public bool AddXP(float value)
    {
        _totalXP += value;
        if (_totalXP >= _levelBoundaries[_level])
        {
            _level++;
            _totalXP = 0;
            _xpBar.value = 0;
            _xpBar.title = "Level " + _level;
            return true;
        }
        else
        {
            _totalXP = _totalXP + value;
            _xpBar.value = _totalXP / _levelBoundaries[_level] * 100;
            return false;
        }
    }
    public void SetXPBar(float value)
    {
        _xpBar.value += value;
    }
    public void SetAmmoBar(float value)
    {
        _ammoBar.value = value;
    }
    public void SetHealth(float value)
    {
        _healthBar.value = value;
    }
    public void SetStamina(float value)
    {
        _staminaBar.value = value;
    }
    private void Update()
    {
        //_healthBar.value += 0.5f * Time.deltaTime;
        //_staminaBar.value += 0.3f * Time.deltaTime;
        //_ammoBar.value += 3f * Time.deltaTime;
        ////_bullets.text = "10";
    }

    private void GoToSpawnZombie(ClickEvent evt)
    {
        GameObject scriptLogic = GameObject.Find("ScriptLogic");
        if (scriptLogic != null)
        {
            //            scriptLogic.GetComponent<TestScript>().SpawnZombies("E_Zombie_1");
            //scriptLogic.GetComponent<TestScript>().SpawnZombies("fem_zombie_1");
            scriptLogic.GetComponent<TestScript>().SpawnZombies();
            //scriptLogic.GetComponent<TestScript>().SpawnZombies("fem_zombie_2");
            //scriptLogic.GetComponent<TestScript>().SpawnZombies("fem_zombie_3");
            //scriptLogic.GetComponent<TestScript>().SpawnZombies("male_zombie_1");
            //scriptLogic.GetComponent<TestScript>().SpawnZombies("male_zombie_2");
            //scriptLogic.GetComponent<TestScript>().SpawnZombies("male_zombie_3");
            //scriptLogic.GetComponent<TestScript>().SpawnZombies("male_zombie_4");
        }
    }

    private void GoToExitApplication(ClickEvent evt)
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // If not running in the Unity Editor, quit the application
        Application.Quit();
#endif
    }

    private void GoToMainScreen(ClickEvent evt)
    {
        SceneManager.LoadScene("ChoosePlayer");
    }
}
