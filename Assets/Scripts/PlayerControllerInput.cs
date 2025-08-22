using Assets.Scripts.Game;
using Assets.Scripts.HealthSystem;
using CodeMonkey.HealthSystemCM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using UnityEngine.Windows;
using static StickDirectionAnalyzer;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]


public class PlayerControllerInput : MonoBehaviour, IGetHealthSystem
{
    [SerializeField] private float _sweepDuration = 2f;
    [SerializeField] private float _sweepLineWidthPct = 0.05f;
    [SerializeField] private float _sweepTrailWidthPct = 0.6f;
    [SerializeField] private float _sweepLineAlpha = 3.0f;
    [SerializeField] private float _sweepTrailAlpha = 0.05f;
    [SerializeField] private float _sweepPauseDuration = 1f;
    [SerializeField] private Color _sweepColor = Color.green;
    [SerializeField] private int _resolution = 30;
    [SerializeField] private float _outerRadius = 10f;
    [SerializeField] private float _pointyTipFactor = 0.25f; // Controls the sharpness of the top

    // laser
    [SerializeField] float baseWidth = 0.045f;
    [SerializeField] float scrollSpeed = 1.6f;     // brzina “strujanja” lasera
    [SerializeField] float pulseSpeed = 2.2f;     // puls širine
    [SerializeField] float pulseMin = 0.85f;    // min multiplikator širine
    [SerializeField] float pulseMax = 1.15f;    // max multiplikator širine
    [SerializeField] Color laserColor = new Color(1f, 0.2f, 0.05f); // žarko crvena
    [SerializeField] float emissionBase = 1.4f;    // osnovna jačina žara
    [SerializeField] float emissionPulse = 1.0f;   // dodatni puls emisije

    Material laserMat;
    Texture2D laserTex;

    // --- tweakables --- laser
    [SerializeField] int aimLinePixels = 0;        // target screen thickness in px
    [SerializeField] float minWorldWidth = 0.02f;  // safety clamps
    [SerializeField] float maxWorldWidth = 0.25f;

    // Prefab path (inside Resources folder) and ParticleSystem references
    [SerializeField] string laserSmokePrefabPath = "VFX/LaserSmoke";
    ParticleSystem _laserSmokeLeft;
    ParticleSystem _laserSmokeRight;

    private float _lastSweepEndTime = -999f;
    private bool _isPausedAtEdge = false;
    private bool _pauseOnRightEdge = false;

    private CharacterController _characterController;
    private Vector3 _direction;
    private float _velocityX;
    private float _velocityZ;
    private float _rotationSpeed = 500f;
    private Camera _mainCamera;
    private readonly float _gravity = -9.81f;
    private float _velocity;
    private float _jumpPower = 3;
    private int _numberOfJumps = 0;
    private int _maxNumberOfJumps = 1;
    private Movement movement;
    private HealthSystem _healthSystem;


    private Animator _animator;
    private bool _aim;
    private bool _aimRightStick;
    private bool _aimLeftTrigger;
    private bool _shoot;
    //private bool _hit = false;
    private float _nextFireTime;
    private GameObject _bulletPrefab;
    //private GameObject _stickPrefab;
    private float _currentHelth;
    private float _reloadTimeLeft;
    private bool _reloadingInProgress = false;
    private UIT_GameScreen _uiT_GameScreen;
    private UIT_EndGamePopUp _uiT_EndGamePopUp;
    private UIT_LevelUp _uiT_LevelUp;
    private int _bulletsInClip;
    private int _reloadingProgress = 0; //If less than 100 then reloading
    private float _currentStamina;
    private bool _waitingToRestoreStamina;
    //private int _roll;
    public ParticleSystem _muzzleFlash;
    public GameObject _impactEffect;
    private bool isGamePaused = false;
    PlayerInput _playerInput;
    private Vector2 _input; //Get rid of this

    //tringle 
    public LineRenderer lineRendererLeft; // Dodajte u Inspectoru
    public LineRenderer lineRendererRight; // Dodajte u Inspectoru
    public float baseRadius = 2f; // Base length of the triangle
    public float lengthMultiplier = 3f; // Triangle is 3x longer
    public float maxAngle = 90f; // Wide V (least precise)
    public float minAngle = 0.1f;  // Narrow V (most precise)

    private GameStats gameStats;
    private float _currentAngleLineRenderers; //current angle of the line renderers, in degrees
    private float _recoil;

    //mesh, jebeni
    public float innerRadius = 1f;  // Inner cut-out
    //public float outerRadius = 15f;  // Outer radius WATCH!!!
    //public float angle = 60f;       // Sector angle in degrees
    //public int resolution = 30;     // Number of segments (higher = smoother) WATCH!!!
    //public PlayerMovement playerMovement; // Reference to PlayerMovement script
    //private MeshFilter meshFilter;
    GameObject _gameObjectAimingCircle;
    private Mesh _meshAimingCircle;
    private MeshFilter _meshFilterAimingCircle;
    private MeshRenderer _meshRendererAimingCircle;
    private MeshCollider _meshColliderAimingCircle;
    private readonly float __gameObjectAimingCircleHeight = 0.11f;
    private AimingCircleTrigger _aimingCircleTrigger;
    private Coroutine _movementPenaltyCoroutine;

    /// <summary>
    /// dash
    /// </summary>
    private float dashDistance = 4f; // Distance of the dash
    private float dashDuration = 0.2f; // Time it takes to dash
    private float dashCooldown = 1f; // Cooldown between dashes
    private bool isDashing = false;
    private float lastDashTime = -Mathf.Infinity;
    //----------------------------------------------
    /// <summary>
    /// ghost from dashing 
    /// </summary>
    private GameObject ghostPrefab;
    private Material ghostMaterial;
    //private float ghostSpawnRate = 0.02f;
    //private float ghostLifeTime = 0.1f;

    //private CharacterController _characterController;
    //private Vector3 _direction = Vector3.zero; // Replace with your actual movement direction logic
    //--------------------------------------------------
    private Volume postProcessVolume;
    private MotionBlur motionBlur;
    public AimingType _aimingType;
    Vector2 _aimControllerInput;
    private GameObject _closestZombie;
    private GameObject[] onScreenZombies; //Zombies on screen

    private GameObject _currentTarget;
    private GameObject _nextTarget;
    private Vector2 previousAimInput = Vector2.zero;

    /// <summary>
    /// for checking if right stick moved some amount of degrees to switch target.
    /// </summary>
    private float rotationThresholdDegrees = 30f; // Minimum stick turn in degrees to register
    public StickDirectionAnalyzer stickAnalyzer;
    private float _lastLogTime;
    private RotationCW _stick_direction;
    private float _stickTargetSwitchCooldown = 0.3f;
    private float _lastStickSwitchTime = -Mathf.Infinity;
    private List<StickInputSample> _stickInputBuffer = new List<StickInputSample>();
    private float _inputBufferDuration = 0.5f;
    RaycastHit _raycastHit;

    public float Health
    {
        get { return _currentHelth; }
        set
        {
            _currentHelth = value;
            if (_currentHelth <= 0)
            {
                GameObject scriptLogic = GameObject.Find("UIEndGame");
                _uiT_EndGamePopUp.enabled = true;
                scriptLogic.GetComponent<UIT_EndGamePopUp>().SetEndGamePopUp("Game Over", "10:00", DataHolder.EnemiesKilled.ToString(), "1000");
                _uiT_EndGamePopUp._root.visible = true;
                _uiT_EndGamePopUp.enabled = true;
                Debug.Log("Player died");
            }
        }
    }
    public float Stamina
    {
        get
        {
            if (_currentStamina <= 0)
            {
                return 0;
            }
            else
            {
                return _currentStamina;
            }
        }
        set
        {
            _currentStamina = value;
            if (_currentStamina <= 0)
            {
                Debug.Log("Zero stamina");
                _currentStamina = 0;
            }
        }
    }

    void Start()
    {
        gameObject.tag = "Player";
        _bulletPrefab = Resources.Load<GameObject>("Weapons/bullet_1");
        if (_bulletPrefab == null)
            Debug.LogError("Bullet prefab is null");
        //InstantiateSticks();
        //Cursor.visible = false;
        //Cursor.lockState = CursorLockMode.Locked;

        GameObject leftLine = new GameObject("LeftLine");
        GameObject rightLine = new("RightLine");

        // Postavite ih kao child objekte glavnog objekta
        leftLine.transform.parent = transform;
        rightLine.transform.parent = transform;

        lineRendererLeft = leftLine.AddComponent<LineRenderer>();
        lineRendererRight = rightLine.AddComponent<LineRenderer>();

        ConfigureLineRenderer(lineRendererLeft);
        ConfigureLineRenderer(lineRendererRight);

        lineRendererLeft.enabled = !_aim;
        lineRendererRight.enabled = !_aim;

        SetLineAlpha(lineRendererLeft, 0);
        SetLineAlpha(lineRendererRight, 0);

        gameStats = new GameStats
        {
            _precisionMin = PlayerConfigSingleton.Instance.PlayerConfig.PrecisionAngleMin,
            _precisionMax = PlayerConfigSingleton.Instance.PlayerConfig.PrecisionAngleMax,
            _precisionStartingAim = PlayerConfigSingleton.Instance.PlayerConfig.PrecisionAimingAngleStarting,
            _aimingSpeed = PlayerConfigSingleton.Instance.PlayerConfig.AimingSpeed
        };
        gameStats._precisionMin = PlayerConfigSingleton.Instance.PlayerConfig.PrecisionAngleMin;
        gameStats._precisionMax = PlayerConfigSingleton.Instance.PlayerConfig.PrecisionAngleMax;
        gameStats._precisionStartingAim = PlayerConfigSingleton.Instance.PlayerConfig.PrecisionAimingAngleStarting;
        gameStats._aimingSpeed = PlayerConfigSingleton.Instance.PlayerConfig.AimingSpeed;

        _currentAngleLineRenderers = gameStats._precisionMax;


        if (_gameObjectAimingCircle == null)
        {
            _gameObjectAimingCircle = new GameObject("AnnularSector");
            _meshFilterAimingCircle = _gameObjectAimingCircle.AddComponent<MeshFilter>();
            _meshRendererAimingCircle = _gameObjectAimingCircle.AddComponent<MeshRenderer>();

            _meshColliderAimingCircle = _gameObjectAimingCircle.AddComponent<MeshCollider>();
            _meshColliderAimingCircle.convex = true;
            _meshColliderAimingCircle.isTrigger = true;

            _meshRendererAimingCircle.material = new Material(Shader.Find("Sprites/Default"))
            {
                color = new Color(0, 1, 0, 0.5f) // Green, 50% transparent
            };

            //meshFilter = GetComponent<MeshFilter>();
            _meshAimingCircle = new();
            _meshAimingCircle.name = "AnnularSector";
            _meshFilterAimingCircle.mesh = _meshAimingCircle;
            _meshColliderAimingCircle.sharedMesh = _meshAimingCircle;
            _gameObjectAimingCircle.AddComponent<AimingCircleTrigger>();

            Rigidbody rb = _gameObjectAimingCircle.AddComponent<Rigidbody>();
            rb.isKinematic = true;  // Prevents physics from affecting it
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            _meshRendererAimingCircle.enabled = true;
            _aimingCircleTrigger = _gameObjectAimingCircle.AddComponent<AimingCircleTrigger>();

        }
        GenerateMesh2(_currentAngleLineRenderers);
        //ghost:
        // Create ghost material programmatically
        SkinnedMeshRenderer skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null)
        {
            Debug.LogError("❌ No SkinnedMeshRenderer found on Player!");
            return;
        }

        // ✅ Create a new ghost material with URP transparency
        ghostMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        ghostMaterial.SetColor("_BaseColor", new Color(1, 1, 1, 0.3f)); // 30% opacity
        ghostMaterial.SetFloat("_Surface", 1); // Transparent
        ghostMaterial.SetFloat("_Blend", 2); // Alpha blending
        ghostMaterial.SetFloat("_Cull", 0); // Render both sides

        // ✅ Create ghost object dynamically
        ghostPrefab = new GameObject("Ghost");

        // ✅ Add MeshFilter & MeshRenderer
        MeshFilter ghostMeshFilter = ghostPrefab.AddComponent<MeshFilter>();
        ghostMeshFilter.mesh = skinnedMeshRenderer.sharedMesh; // ✅ Assign correct mesh

        ghostPrefab.AddComponent<MeshRenderer>().material = ghostMaterial;
        ghostPrefab.transform.localScale = transform.localScale;

        Debug.Log("✅ Ghost prefab created with mesh: " + ghostMeshFilter.mesh.name);

        //blur 
        // Find PostProcessing Volume dynamically
        postProcessVolume = GameObject.Find("PostProcessing")?.GetComponent<Volume>();
        if (postProcessVolume != null)
        {
            postProcessVolume.profile.TryGet(out motionBlur);
        }
        else
        {
            Debug.LogError("❌ PostProcessing Volume not found!");
        }
        stickAnalyzer = new StickDirectionAnalyzer();
        //laser
        laserTex = MakeLaserTexture(64, 1.8f);

        // 2) Materijal: URP particles unlit (fallback na legacy additive)
        var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Particles/Additive");
        laserMat = new Material(sh);
        laserMat.mainTexture = laserTex;
        laserMat.SetColor("_BaseColor", laserColor);           // URP varijanta
        laserMat.SetColor("_Color", laserColor);               // Legacy fallback
        laserMat.EnableKeyword("_EMISSION");
        laserMat.SetColor("_EmissionColor", laserColor * emissionBase);

        // 3) Podesi oba line renderera kao “laser”
        SetupLaser(lineRendererLeft);
        SetupLaser(lineRendererRight);
        //QuickLineDebugInit();
        EnsureLineRenderersOnTop();
        InitLaserSmoke();

    }

    private void Awake()
    {
        _playerInput = new PlayerInput();
        if (_playerInput != null)
        {
            _playerInput.Gameplay.Move.performed += Move;
            _playerInput.Gameplay.Move.canceled += Move;
            _playerInput.Gameplay.Move.started += Move;
            _playerInput.Gameplay.Jump.performed += Jump;
            _playerInput.Gameplay.Jump.started += Jump;
            _playerInput.Gameplay.Sprint.performed += Sprint;
            _playerInput.Gameplay.Sprint.canceled += Sprint;
            _playerInput.Gameplay.Aim.started += Aiming;
            _playerInput.Gameplay.Aim.canceled += Aiming;
            _playerInput.Gameplay.Aim.performed += Aiming;
            _playerInput.Gameplay.AimControllerRightStick.started += AimingControllerRightStick;
            _playerInput.Gameplay.AimControllerRightStick.performed += AimingControllerRightStick;
            _playerInput.Gameplay.AimControllerRightStick.canceled += AimingControllerRightStick;
            _playerInput.Gameplay.AimingTrigger.performed += AimingControllerTrigger;
            _playerInput.Gameplay.AimingTrigger.canceled += AimingControllerTrigger;
            _playerInput.Gameplay.AimingTrigger.started += AimingControllerTrigger;

            _playerInput.Gameplay.Fire.performed += Shooting;
            _playerInput.Gameplay.Fire.started += Shooting;
            _playerInput.Gameplay.Fire.canceled += Shooting;
            _playerInput.Gameplay.Enable();
            _playerInput.UI.Enable();
        }
        _characterController = GetComponent<CharacterController>();
        _mainCamera = Camera.main;
        _animator = GetComponent<Animator>();
        _currentHelth = PlayerConfigSingleton.Instance.PlayerConfig.health;
        _healthSystem = new HealthSystem(PlayerConfigSingleton.Instance.PlayerConfig.health);
        _uiT_GameScreen = FindFirstObjectByType<UIT_GameScreen>();
        _bulletsInClip = WeaponConfigSingleton.Instance.WeaponConfig.ClipSize;
        _uiT_GameScreen._bullets.text = _bulletsInClip.ToString();
        _uiT_GameScreen.SetHealth(100);
        Stamina = PlayerConfigSingleton.Instance.PlayerConfig.stamina;
        _uiT_GameScreen.SetStamina(100);


        _uiT_EndGamePopUp = FindObjectOfType<UIT_EndGamePopUp>();
        _uiT_EndGamePopUp._root.visible = false;
        _uiT_EndGamePopUp.enabled = false;

    }


    private void OnDestroy()
    {
        _playerInput.Gameplay.Move.performed -= Move;
        _playerInput.Gameplay.Move.canceled -= Move;
        _playerInput.Gameplay.Move.started -= Move;
        _playerInput.Gameplay.Jump.performed -= Jump;
        _playerInput.Gameplay.Jump.started -= Jump;
        _playerInput.Gameplay.Sprint.performed -= Sprint;
        _playerInput.Gameplay.Sprint.canceled -= Sprint;
        _playerInput.Gameplay.Aim.started -= Aiming;
        _playerInput.Gameplay.Aim.canceled -= Aiming;
        _playerInput.Gameplay.Aim.performed -= Aiming;
        _playerInput.Gameplay.AimControllerRightStick.started -= AimingControllerRightStick;
        _playerInput.Gameplay.AimControllerRightStick.canceled -= AimingControllerRightStick;
        _playerInput.Gameplay.AimControllerRightStick.performed -= AimingControllerRightStick;
        _playerInput.Gameplay.AimingTrigger.performed -= AimingControllerTrigger;
        _playerInput.Gameplay.AimingTrigger.canceled -= AimingControllerTrigger;
        _playerInput.Gameplay.AimingTrigger.started -= AimingControllerTrigger;
        _playerInput.Gameplay.Fire.performed -= Shooting;
        _playerInput.Gameplay.Fire.started -= Shooting;
        _playerInput.Gameplay.Fire.canceled -= Shooting;
        _playerInput.Gameplay.Disable();
        _playerInput.UI.Disable();
    }
    private void Update()
    {
        //QuickLineDebugTick();

        if (_animator != null)
        {
            _animator = GetComponent<Animator>();
        }
        onScreenZombies = GameObject.FindGameObjectsWithTag("Zombie"); // Find all zombies

        RotateTowardsSomething();
        ApplyGravity();
        ApplyMovement();
        Aim();
        Shoot();
        RestoreStamina();
        //Roll();
        UpdateLineRenderersTriangle();
        RefreshAimLineWidth();

        //FollowPlayer();
        //_stick_direction = stickAnalyzer.AnalyzeInput(_aimControllerInput);
        if (_stick_direction == RotationCW.NONE)
        {
            //_stick_direction = stickAnalyzer.CheckDirectInputTurn(_aimControllerInput, transform.forward);
        }
        else
        {
            //_stick_direction = stickAnalyzer.AnalyzeInput(_aimControllerInput);
        }



        if (_uiT_EndGamePopUp._root.visible && !isGamePaused)
        {
            // Pause the game
            PauseGame(true);
        }
        //AnimateLaser(lineRendererLeft, laserMat);
        //AnimateLaser(lineRendererRight, laserMat);
        UpdateAimingVisuals();
    }

    public void Move(InputAction.CallbackContext context)
    {
        _input = context.ReadValue<Vector2>();
        if (_input == null) Debug.LogError("_input is null");
        _direction = new Vector3(_input.x, 0, _input.y);
        _direction.Normalize();
        _velocityZ = Vector3.Dot(_direction.normalized, transform.forward);
        _velocityX = Vector3.Dot(_direction.normalized, transform.right);


    }


    public static float DegreesToRadians(float degrees)
    {
        var radians = degrees * (float)Math.PI / 180f;
        return radians;
    }
    /// <summary>
    /// First step of rotation
    /// </summary>
    /// <returns></returns>
    private Quaternion ProcessRotation()
    {
        Quaternion rotationL = Quaternion.identity;
        if (_aim && AimingType.Mouse == _aimingType && 1 == 2)
        {
            rotationL = MouseRotation();
        }


        if (_aim && AimingType.ControllerRightStick == _aimingType)
        {
            rotationL = ControllerRotation();
        }
        return rotationL;
    }

    private Quaternion ControllerRotation()
    {
        try
        {
            //#if UNITY_EDITOR
            //            var logEntries = System.Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
            //            var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            //            clearMethod.Invoke(null, null);
            //#endif
            Quaternion return_val = transform.rotation;
            Vector3 directionController = new(_aimControllerInput.x, 0, _aimControllerInput.y);
            if (directionController.sqrMagnitude < 0.01f)
            {
                return transform.rotation;
            }
            directionController.Normalize();
            if (_currentTarget == null && _nextTarget == null) //nemas nista naciljano, trazi najblizeg
            {
                _nextTarget = GetClosestZombie();
                return_val = RotateTowardsGameObject(_nextTarget);
            }
            if (_currentTarget != null && _nextTarget == null) //imas naciljano, ali ne mislis nikoga da ciljas
            {
                return_val = RotateTowardsGameObject(_currentTarget);
            }
            if (_currentTarget == null && _nextTarget != null) //nema naciljano, ali mislis nekoga da ciljas
            {
                return_val = RotateTowardsGameObject(_nextTarget);
            }
            if (_currentTarget != null && _nextTarget != null && _currentTarget.name != _nextTarget.name) //switching mode
            {
                return_val = RotateTowardsGameObject(_nextTarget);
            }
            if (_currentTarget != null && _nextTarget != null && _currentTarget.name == _nextTarget.name) //switching mode
            {
                return_val = RotateTowardsGameObject(_nextTarget);
            }
            if (_currentTarget == null && _nextTarget == null) //niti imas naciljano, niti mislis nekoga da ciljas
            {
                return RotateTowardsDirection(directionController);
            }
            return return_val;
        }
        catch (Exception ex)
        {
            Debug.LogError("Error in ControllerRotation: " + ex.Message);
            return Quaternion.identity;
        }
    }

    private Quaternion RotateTowardsDirection(Vector3 direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        return Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
    }

    private Quaternion MouseRotation()
    {
        Debug.Log("Mouse rotation");
        var ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        int layerMask = LayerMask.GetMask("Ground"); // Make sure the ground has this layer

        if (Physics.Raycast(ray, out _raycastHit, Mathf.Infinity, layerMask))
        {
            var target = _raycastHit.point;
            target.y = transform.position.y; // Keep player at the same height

            var direction = target - transform.position;
            direction.y = 0f; // Ignore vertical movement
            direction.Normalize();

            var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            return Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }
        return Quaternion.identity;
    }


    Quaternion RotateTowardsGameObject(GameObject zombie)
    {
        Vector3 directionToZombie = zombie.transform.position - transform.position;
        directionToZombie.y = 0f;

        Quaternion targetRotation = Quaternion.LookRotation(directionToZombie, Vector3.up);
        float angle = Quaternion.Angle(transform.rotation, targetRotation);

        if (angle < 0.5f)
        {
            if (_currentTarget != _nextTarget && _nextTarget != null) //Aimed, clean it once _currentTarget calculater on different method, on controller motion
            {
                _currentTarget = _nextTarget;
                _nextTarget = null; // Reset next target
                if (_currentTarget != null)
                    Debug.Log("Aimed at target: " + _currentTarget.name);
                else
                    Debug.Log("Target cleared.");
            }
        }

        return Quaternion.Slerp(transform.rotation, targetRotation, 7f * Time.deltaTime);
    }
    void OnGUI()
    {
        int y = 10;
        if (_currentTarget != null)
        {
            GUI.Label(new Rect(10, y, 300, 20), "_current " + _currentTarget.name);
            y += 20;
        }
        else
        {
            GUI.Label(new Rect(10, y, 300, 20), "_current is null ");
            y += 20;
        }

        if (_nextTarget != null)
        {
            GUI.Label(new Rect(10, y, 300, 20), "_current " + _nextTarget.name);
            y += 20;
        }
        else
        {
            GUI.Label(new Rect(10, y, 300, 20), "_next is null ");
            y += 20;
        }

        GUI.Label(new Rect(10, y, 300, 20), "rotation:" + _stick_direction);
        y += 20;

        if (onScreenZombies != null)
        {
            foreach (GameObject zombie in onScreenZombies)
            {
                if (zombie == null) continue;

                float finalAngle = GetZombieAngle(zombie);

                string text = $"Zombie: {zombie.name}, Angle: {finalAngle:F1}°";
                GUI.Label(new Rect(10, y, 600, 20), text);
                y += 20;
            }
        }
        GUI.Label(new Rect(10, y, 300, 20), "Raycast hit:" + _raycastHit.ToString());
        y += 20;
        //GUI.Label(new Rect(10, y, 300, 20), "pause: " + _pauseTimeElapsed);
    }

    private float GetZombieAngle(GameObject zombie)
    {
        Vector3 playerForward = transform.forward;
        playerForward.y = 0;
        playerForward.Normalize();

        Vector3 toZombie = zombie.transform.position - transform.position;
        toZombie.y = 0;
        toZombie.Normalize();
        float angle = Vector3.Angle(playerForward, toZombie);
        Vector3 cross = Vector3.Cross(playerForward, toZombie);
        bool isCW = cross.y < 0;
        float finalAngle = isCW ? 360f - angle : angle;
        return finalAngle;
    }
    float GetAngleBetween(Vector3 forward, Vector3 flickDirection)
    {
        forward.y = 0;
        forward.Normalize();

        flickDirection.y = 0;
        flickDirection.Normalize();

        float angle = Vector3.Angle(forward, flickDirection);
        Vector3 cross = Vector3.Cross(forward, flickDirection);
        bool isCW = cross.y > 0;
        float finalAngle = isCW ? 360f - angle : angle;
        // uvek vrati pogresno, da probam samo da reverzujem
        return finalAngle;
    }


    /// <summary>
    /// Main rotation processing
    /// </summary>
    private void RotateTowardsSomething()
    {

        if (_aim)
        {
            //var ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

            //int layerMask = LayerMask.GetMask("Ground"); // Make sure the ground has this layer
            Quaternion rot = ProcessRotation();
            transform.rotation = rot;// Quaternion.Lerp(transform.rotation, rot, Time.deltaTime);
            _gameObjectAimingCircle.transform.rotation = transform.rotation;
            if (_meshRendererAimingCircle.enabled == false)
            {
                _meshRendererAimingCircle.enabled = true;
            }
            GenerateMesh2(_currentAngleLineRenderers);

            if (!_laserSmokeLeft.isPlaying) _laserSmokeLeft.Play();
            if (!_laserSmokeRight.isPlaying) _laserSmokeRight.Play();

            AlignEmitterToLine(_laserSmokeLeft, lineRendererLeft, 0.02f);
            AlignEmitterToLine(_laserSmokeRight, lineRendererRight, 0.02f);
        }
        else
        {
            if (_input.sqrMagnitude == 0) return;
            Vector3 direction = _direction;
            direction.y = 0f;
            Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, _rotationSpeed * Time.deltaTime);
            //_meshAimingCircle.Clear();
            if (_meshRendererAimingCircle.enabled == true)
            {
                //Debug.Log("Clearing mesh, not aiming, rotate");
                _meshRendererAimingCircle.enabled = false;
            }
            //Debug.Log("Clearing mesh");
            _currentTarget = null;
            _nextTarget = null;

            if (_laserSmokeLeft.isPlaying) _laserSmokeLeft.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (_laserSmokeRight.isPlaying) _laserSmokeRight.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        }
    }

    private void ApplyMovement()
    {

        //if (_input.sqrMagnitude > 0){
        if (!isDashing)
        {
            int sprinting = movement.isSprinting ? 1 : 0;
            var targetSpeed = GetMoveSpeed(_input.magnitude, sprinting);
            movement.currentSpeed = Mathf.MoveTowards(movement.currentSpeed, targetSpeed, PlayerConfigSingleton.Instance.PlayerConfig.speed * Time.deltaTime);
            //_characterController.Move(_direction * movement.currentSpeed * Time.deltaTime);
            float moveStep = movement.currentSpeed * Time.deltaTime;//reorder for better performance
            _characterController.Move(_direction * moveStep);
        }

        MoveAimingCircle();

        //verovatno ne treba
        //float angle = GetAngleBetweenVectors(_direction, GetNormalizedMousePosition(transform.position));
        //angle = DegreesToRadians(angle);


        _animator.SetFloat("MoveZ", _velocityZ, 0.2f, Time.deltaTime);
        _animator.SetFloat("MoveX", _velocityX, 0.2f, Time.deltaTime);
        _animator.SetFloat("moveAmount", _input.magnitude, 0.2f, Time.deltaTime);

        // Update aiming penalty based on movement
        bool isWalking = _input.magnitude > 0 && !movement.isSprinting;
        bool isRunning = movement.isSprinting;
        bool isStrafing = _input.magnitude > 0; // Can be separate check if needed

        UpdateMovementPenalty(isWalking, isRunning, isStrafing);
    }

    private void MoveAimingCircle()
    {
        _gameObjectAimingCircle.transform.position = new Vector3(
            _characterController.transform.position.x,
            _characterController.transform.position.y - _characterController.height / 2f + __gameObjectAimingCircleHeight, // Align to ground
            _characterController.transform.position.z
        );
    }

    private void ApplyGravity()
    {
        if (IsGrounded() && _velocity < 0)
        {
            _velocity = -1f;
        }
        else
        {
            _velocity += _gravity * Time.deltaTime;
            _direction.y = _velocity;
        }
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        if (!IsGrounded() && _numberOfJumps >= _maxNumberOfJumps) return;
        if (Stamina <= 0) return;
        if (Stamina <= 30) return;
        if (_numberOfJumps == 0) StartCoroutine(WaitForLanding());

        _numberOfJumps++;
        //_velocity = _jumpPower;
        _animator.SetTrigger("RollTrigger");
        //_animator.CrossFade("DiveRoll", 0.5f);
        Stamina -= 30;
        _uiT_GameScreen.SetStamina(Stamina); //TODO : Stamina cost for jumping
        Debug.Log("Jumping");
        if (Time.time >= lastDashTime + dashCooldown && !isDashing && _input.magnitude > 0)
        {
            StartCoroutine(DashCoroutine());
        }
    }
    public void Sprint(InputAction.CallbackContext context)
    {
        movement.isSprinting = context.started || context.performed;
        if (context.canceled) movement.isSprinting = false;
    }
    private IEnumerator WaitForLanding()
    {
        yield return new WaitUntil(() => !IsGrounded());
        yield return new WaitUntil(IsGrounded);
        _numberOfJumps = 0;
    }
    private bool IsGrounded() => _characterController.isGrounded;
    private float GetMoveSpeed(float moveAmount, int runningLocal)
    {
        float localWalkingSpeedForward;
        float localRunningSpeedForward;
        float localWalkingSpeedBack;
        float localRunningSpeedBack;

        float returnSpeed = PlayerConfigSingleton.Instance.PlayerConfig.speed;
        float tempWalking = runningLocal == 1 ? 0 : 1;
        float fwdBck = Vector3.Dot(transform.forward, _direction);

        if (Math.Sign(fwdBck) == 1)
        {
            localWalkingSpeedForward = ((PlayerConfigSingleton.Instance.PlayerConfig.speed / 100) + 1) * tempWalking;
            localRunningSpeedForward = ((PlayerConfigSingleton.Instance.PlayerConfig.RunningSpeed_pct / 100) + 1) * runningLocal;
            //returnSpeed = returnSpeed * (localWalkingSpeedForward + localRunningSpeedForward);
            returnSpeed *= (localWalkingSpeedForward + localRunningSpeedForward);
        }
        if (Math.Sign(fwdBck) == -1)
        {
            localWalkingSpeedBack = ((PlayerConfigSingleton.Instance.PlayerConfig.speed / 100) + 1) * tempWalking;
            localRunningSpeedBack = ((PlayerConfigSingleton.Instance.PlayerConfig.BackMovementPenalty_pct * PlayerConfigSingleton.Instance.PlayerConfig.speed / 100) + 1) * runningLocal;
            //returnSpeed = returnSpeed * (localWalkingSpeedBack + localRunningSpeedBack);
            returnSpeed *= (localWalkingSpeedBack + localRunningSpeedBack);

        }
        return returnSpeed;
    }
    //re
    public void Aiming(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _aimingType = AimingType.Mouse;
            _aim = true;
        }
        if (context.canceled || !context.performed)
        {
            _aim = false;
        }
    }
    public void AimingControllerRightStick(InputAction.CallbackContext context)
    {
        Vector2 currentInput = context.ReadValue<Vector2>();

        if (currentInput.magnitude > 0.1f)
        {
            _aim = true; //can be true because of left trigger
            _aimingType = AimingType.ControllerRightStick;
            _aimControllerInput = currentInput;
        }
        if (_aim == true)
        {
            _stickInputBuffer.Add(new StickInputSample(currentInput, Time.time));
            _stickInputBuffer.RemoveAll(sample => Time.time - sample.time > _inputBufferDuration);
        }
        if (currentInput.magnitude <= 0.1f && _aimLeftTrigger == false)
        {
            _aim = false;
            _currentTarget = null;
        }
        if (currentInput.magnitude <= 0.1f)
        {
            _aimRightStick = false;
        }

        ///for testing:
        if (_currentTarget == null && _aim == true)
        {
            _currentTarget = GetClosestZombie();
        }
        _stick_direction = AnalyzeFlickDirection(transform.forward);
        if (_stick_direction != RotationCW.NONE)
        {
            SetNextTarget();
        }
        _stick_direction = AnalyzeStickRotation45(transform.forward);
        if (_stick_direction != RotationCW.NONE)
        {
            SetNextTarget();
        }
    }
    private RotationCW AnalyzeFlickDirection(Vector3 playerForward)
    {
        if (_stickInputBuffer.Count < 2)
            return RotationCW.NONE;

        StickInputSample neutralSample = null;
        StickInputSample strongSample = null;

        for (int i = 0; i < _stickInputBuffer.Count; i++)
        {
            //Debug.Log("Input buffer: " + _stickInputBuffer.Count);
            if (neutralSample == null && _stickInputBuffer[i].input.magnitude < 0.2f)
            {
                neutralSample = _stickInputBuffer[i];
            }

            if (_stickInputBuffer[i].input.magnitude >= 0.7f)
            {
                strongSample = _stickInputBuffer[i];
            }
        }

        if (neutralSample != null && strongSample != null && strongSample.time > neutralSample.time)
        {
            Debug.Log("Flick detected, analyzing direction");
            Vector2 inputDir = strongSample.input.normalized;
            Vector3 flickDir = new Vector3(inputDir.x, 0, inputDir.y); // Convert to world-space

            float angle = GetAngleBetween(playerForward, flickDir);
            Debug.Log($"Flick angle: {angle}");

            _stickInputBuffer.Clear();

            if (angle < 15f || angle > 345f)
            {
                Debug.Log("Flick too straight, not rotating");
                return RotationCW.NONE; // too straight
            }

            var retval = angle <= 180f ? RotationCW.CCW : RotationCW.CW;
            Debug.Log("Returning: " + retval);
            //return angle <= 180f ? RotationCW.CCW : RotationCW.CW;
            return retval;
        }

        //Debug.Log("Flick not detected");
        return RotationCW.NONE;
    }

    private RotationCW AnalyzeStickRotation45(Vector3 playerForward)
    {
        if (_stickInputBuffer.Count < 2)
            return RotationCW.NONE;

        StickInputSample? startSample = null;
        float startAngle = 0f;

        foreach (var sample in _stickInputBuffer)
        {
            if (sample.input.magnitude >= 0.7f)
            {
                Vector3 dir = new Vector3(sample.input.x, 0f, sample.input.y).normalized;
                float angle = GetAngleBetween(playerForward, dir);

                if (startSample == null)
                {
                    startSample = sample;
                    startAngle = angle;
                }
                else
                {
                    float angleDelta = Mathf.DeltaAngle(startAngle, angle); // -180 to 180
                    float absDelta = Mathf.Abs(angleDelta);

                    if (absDelta >= 45f)
                    {
                        _stickInputBuffer.Clear();
                        Debug.Log("Stick rotated " + absDelta + " degrees: " + (angleDelta > 0 ? "CCW" : "CW"));
                        return angleDelta > 0 ? RotationCW.CCW : RotationCW.CW;
                    }
                }
            }
        }

        return RotationCW.NONE;
    }


    private void SetNextTarget()
    {
        if (_stick_direction != RotationCW.NONE)
        {
            Debug.Log("Aiming with right stick - switching target.");
            GameObject newFoundTarget;
            if (_stick_direction == RotationCW.CW)
            {
                newFoundTarget = GetNextTarget(true);
                Debug.Log("Clockwise rotation, get target = " + newFoundTarget.name);
            }
            else
            {
                newFoundTarget = GetNextTarget(false);
                Debug.Log("Counter-clockwise rotation, get target" + newFoundTarget.name);
            }
            if (newFoundTarget != _currentTarget)
            {
                _nextTarget = newFoundTarget;
                _lastStickSwitchTime = Time.time; // Reset cooldown
            }
        }
    }

    private void AimingControllerTrigger(InputAction.CallbackContext context)
    {
        if (context.performed) // LT Pressed
        {
            //_isAiming = true;
            _aim = true;
            _aimLeftTrigger = true;
            //currentTarget = GetClosestZombie(); // Start by locking onto the closest zombie
        }
        if (context.canceled)
        {
            _aimLeftTrigger = false;
        }
        if (context.canceled && !_aimRightStick)
        {
            _aim = false;
            _currentTarget = null; // Reset target
        }
    }

    private GameObject GetNextTarget(bool rotationIsClockwise)
    {
        Debug.Log("GetNextTarget");
        GameObject[] zombies = onScreenZombies;
        if (zombies.Length == 1 || _currentTarget == null || zombies.Length == 0)
        {
            Debug.Log("Only one zombie or no target");
            return null;
        }

        GameObject nextTarget = null;
        float closestAngle = Mathf.Infinity;

        Vector3 aimDirection = new Vector3(_aimControllerInput.x, 0, _aimControllerInput.y).normalized;
        if (aimDirection == Vector3.zero)
        {
            aimDirection = transform.forward; // fallback if no input
            Debug.LogError("Aim direction is zero, using forward");
        }
        List<Transform> targets = GetZombies(zombies);
        nextTarget = GetNextZombieInDirection(targets, aimDirection, rotationIsClockwise ? RotationCW.CW : RotationCW.CCW).gameObject;
        return nextTarget;
    }
    private List<Transform> GetZombies(GameObject[] zombies)
    {
        List<Transform> zombieTransforms = new List<Transform>();
        foreach (GameObject zombie in zombies)
        {
            if (zombie != null && zombie.name != _currentTarget.name)
            {
                zombieTransforms.Add(zombie.transform);
            }
        }
        return zombieTransforms;
    }
    Transform GetNextZombieInDirection(List<Transform> zombies, Vector2 currentAimDirection, RotationCW direction)
    {
        try
        {
            Transform bestZombie = null;
            float bestAngle = direction == RotationCW.CW ? float.MaxValue : float.MinValue;

            foreach (Transform zombie in zombies)
            {
                if (zombie == null) continue;
                if (zombie.gameObject.name == _currentTarget.name) continue;
                float angle = GetZombieAngle(zombie.gameObject);

                if (direction == RotationCW.CW)
                {
                    if (angle < bestAngle)
                    {
                        bestAngle = angle;
                        bestZombie = zombie;
                    }
                }
                else // CCW
                {
                    if (angle > bestAngle)
                    {
                        bestAngle = angle;
                        bestZombie = zombie;
                    }
                }
            }
            return bestZombie;
        }
        catch (Exception ex)
        {
            Debug.LogError("Error in GetNextZombieInDirection: " + ex.Message);
            return null;
        }
    }
    float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0)
            angle += 360f;
        return angle;
    }
    public void Aim()
    {
        if (_aim)
        {
            _animator.SetFloat("AimH", DataHolder.weaponType == WeaponType.H1 ? 1 : 0);
            _animator.SetFloat("AimHH", DataHolder.weaponType == WeaponType.H2 ? 1 : 0);
        }
        else
        {
            _animator.SetFloat("AimH", 0);
            _animator.SetFloat("AimHH", 0);
            _meshRendererAimingCircle.enabled = false;
            _aimingCircleTrigger?.ClearAllOutlinedZombies(); // Null-check shorthand
        }
    }

    public void Shooting(InputAction.CallbackContext context)
    {
        if ((context.started) && !context.canceled)
        {
            if (_aim) //zato sto ne postoji modifier da je pokrenut aim na kontroleru, jebemImMater
                _shoot = true;
        }
        if (context.canceled)
        {
            _shoot = false;
        }
    }
    public void Shoot()
    {
        try
        {
            if (_shoot && _bulletsInClip > 0)
            {
                if (Time.time >= _nextFireTime /*&& Input.GetButton("Fire1")*/)
                {
                    _bulletsInClip--;
                    Transform gunBarrel = GetWeaponPostitionFromPlayer();
                    // Calculate the next allowed shooting time
                    _nextFireTime = Time.time + 1f / WeaponConfigSingleton.Instance.WeaponConfig.FireRate; //TODO MODIFIER OF PLAYER CONFIG for faster shooting
                                                                                                           //GameObject hitParticleGameObject = Resources.Load<GameObject>("Weapons/Hit_01");
                    try
                    {
                        ParticleSystem muzzleFlash = GetComponentInChildren<ParticleSystem>();
                        muzzleFlash.Play();
                    }
                    catch (Exception)
                    {
                        Debug.Log("No muzzle flash");
                    }
                    float randomAngle = UnityEngine.Random.Range(-_currentAngleLineRenderers, _currentAngleLineRenderers);
                    Vector3 forceDirection = Quaternion.Euler(0f, randomAngle, 0f) * transform.forward;

                    //RayCastHit(gunBarrel, hitParticleGameObject, forceDirection);
                    SpawnBulletAndShoot(gunBarrel, forceDirection, _currentAngleLineRenderers);
                    Recoil(_currentAngleLineRenderers);
                    SoundFXManager.Instance.PlaySoundFXClip(WeaponConfigSingleton.Instance.WeaponConfig.shootingClip, transform, 1f);
                }
            }
            if (!_shoot)
            {
            }
            //Nema veze sa pucanjem, non stop se okida:
            if (_bulletsInClip == 0 && !_reloadingInProgress) //Start reloading
            {
                _reloadTimeLeft = GetTotalReloadTime();
                _nextFireTime = Time.time + _reloadTimeLeft;
                _reloadingInProgress = true;
                _uiT_GameScreen.SetAmmoBar(0);
            }
            if ((_bulletsInClip > 0) && (!_reloadingInProgress)) //Not reloading progres, show number of bullets
            {
                _uiT_GameScreen.SetAmmoBar(GetPercentageOfClipLeft()); //
                _uiT_GameScreen._bullets.text = _bulletsInClip.ToString();
            }
            if (_reloadingInProgress)
            {
                ReloadingProgress();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error in shooting: " + e.Message);
        }
    }

    private void SpawnBulletAndShoot(Transform gunBarrel, Vector3 forceDirection, float currentAngle)
    {
        // Ensure the direction is normalized
        forceDirection = forceDirection.normalized;
        forceDirection.y = 0;

        // Create a rotation to align the bullet's forward direction with the force direction
        Quaternion bulletRotation = Quaternion.LookRotation(forceDirection);
        bulletRotation *= Quaternion.Euler(-90, 0, 0); // Apply random rotation

        // Instantiate the bullet with the correct position and rotation
        GameObject bullet = Instantiate(_bulletPrefab, gunBarrel.position, bulletRotation);

        // Pass the force direction to the BulletBehaviour script
        BulletBehaviour bulletBehaviour = bullet.GetComponent<BulletBehaviour>();
        if (bulletBehaviour != null)
        {
            bulletBehaviour.Initialize(forceDirection);
        }

    }

    /// <summary>
    /// Applies recoil to the weapon, increasing the aiming angle (reducing precision).
    /// Weapon recoil is countered by the player's recoil reduction.
    /// </summary>
    /// <param name="currentAngle">The current aiming angle before applying recoil.</param>
    public void Recoil(float currentAngle)
    {
        float weaponRecoil = WeaponConfigSingleton.Instance.WeaponConfig.Recoil;
        float playerRecoil = PlayerConfigSingleton.Instance.PlayerConfig.recoilReduction;
        float totalRecoil = Mathf.Max(weaponRecoil - playerRecoil, 0);


        //goes from min to max, can be bigger than starting
        minAngle = gameStats._precisionMin;
        maxAngle = gameStats._precisionMax;

        float totalAngleRange = maxAngle - minAngle; // Total possible deviation
        float newAngle = Mathf.Min(_currentAngleLineRenderers + (totalAngleRange * (totalRecoil / 100)), maxAngle);
        _recoil = newAngle;
        Debug.Log($"Recoil Applied: Weapon={weaponRecoil}, Player={playerRecoil}, Total={totalRecoil}, NewAngle={newAngle}");
    }


    public void ReloadingProgress()
    {
        _reloadingProgress = (int)(100 - (100 * (_reloadTimeLeft / GetTotalReloadTime()))) + 1;
        _reloadTimeLeft -= Time.deltaTime;
        _uiT_GameScreen.SetAmmoBar(_reloadingProgress);
        if (_reloadingProgress >= 100)
        {
            _bulletsInClip = WeaponConfigSingleton.Instance.WeaponConfig.ClipSize;
            _reloadingProgress = 100;
            _reloadingInProgress = false;
        }
    }

    private float GetPercentageOfClipLeft()
    {
        float b = (float)_bulletsInClip;
        float c = (float)WeaponConfigSingleton.Instance.WeaponConfig.ClipSize;
        return b / c * 100.0f;
    }
    private float GetTotalReloadTime()
    {
        return WeaponConfigSingleton.Instance.WeaponConfig.ReloadTime * (1 + PlayerConfigSingleton.Instance.PlayerConfig.reloadSpeed / 100); //Weapon reload time * player reload speed 1 sec from gun + 30% from player
    }
    public Transform GetWeaponPostitionFromPlayer()
    {
        string bonename = "Weapon";
        Transform weaponTransform = FindRecursive(transform, bonename);
        if (weaponTransform == null)
        {
            bonename = "Pistol(Clone)";
            weaponTransform = FindRecursive(transform, bonename);
        }
        Transform newTransform = AddOnZAxis(weaponTransform, 1f);

        return newTransform; //    Weapon to be a bit in front of the player
    }
    public Transform GetBonePostitionFromObject(string boneName, GameObject searchObject)
    {
        Transform boneTransform = FindRecursive(searchObject.transform, boneName);
        if (boneTransform == null)
        {
            Debug.LogError("Bone:" + boneName + "from: " + searchObject.name + "not found");
        }
        return boneTransform;
    }

    public Transform AddOnZAxis(Transform transform, float z)
    {
        Vector3 temp = transform.position;
        temp.z += z;
        Transform newTransform = DeepCopyTransform(transform);
        newTransform.forward = newTransform.forward * 1.01f; //1% in front
        return newTransform;
    }
    public Transform DeepCopyTransform(Transform original)
    {
        GameObject copyGameObject = new GameObject(original.name); // Create a new GameObject with the same name
        Transform copyTransform = copyGameObject.transform; // Get the Transform component of the new GameObject

        copyTransform.position = original.position;
        copyTransform.rotation = original.rotation;
        copyTransform.localScale = original.localScale;

        Destroy(copyGameObject);

        return copyTransform; // Return the deep copy of the Transform
    }

    Transform FindRecursive(Transform parent, string pattern) //duplicated method from Player.cs
    {
        Regex regex = new(pattern);

        if (regex.IsMatch(parent.name))
        {
            return parent;
        }

        foreach (Transform child in parent)
        {
            Transform result = FindRecursive(child, pattern);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    public void RestoreStamina()
    {
        if (_waitingToRestoreStamina) return;
        if (Stamina == 0 && !_waitingToRestoreStamina)
        {
            _waitingToRestoreStamina = true;
            Invoke("StopWaitingForRestoreStamina", 1f);
        }
        if (movement.isSprinting == false && _currentStamina < PlayerConfigSingleton.Instance.PlayerConfig.stamina)
        {
            Stamina += PlayerConfigSingleton.Instance.PlayerConfig.staminaRegenSpeed * Time.deltaTime;
            float staminapct = (_currentStamina / PlayerConfigSingleton.Instance.PlayerConfig.stamina) * 100;
            _uiT_GameScreen.SetStamina(staminapct);
        }
    }

    public void HitReceived(float damage, string bodyPart)
    {
        Health -= damage;
        _healthSystem.Damage(damage);
        float healthPct = (_healthSystem.GetHealth() / _healthSystem.GetHealthMax() * 100);
        _uiT_GameScreen.SetHealth(healthPct);
        Debug.Log("Player hit in " + bodyPart + " for " + damage + " damage. Current health: " + Health);
        //_hit = true;
        _animator.Play("HeadHit");
        SoundFXManager.Instance.PlaySoundFXClip(PlayerConfigSingleton.Instance.PlayerConfig.hitReceived, transform, 1f);
    }

    public HealthSystem GetHealthSystem()
    {
        return _healthSystem;
    }

    public void PauseGame(bool pause)
    {
        if (pause)
        {
            Time.timeScale = 0f;
            isGamePaused = true;
        }
        else
        {
            Time.timeScale = 1f;
            isGamePaused = false;
        }
    }

    internal void AddCoins(int coinValue)
    {
        if (_uiT_GameScreen.AddXP(coinValue)) //level up
        {
            GameObject levelUpPrefab = Resources.Load<GameObject>("LevelUp/UILevelUp");
            //_LevelUpGameObject = Instantiate(levelUpPrefab, transform.position + new Vector3(0f, 1f, 0f), Quaternion.identity);
            var v = Instantiate(levelUpPrefab, transform.position + new Vector3(0f, 1f, 0f), Quaternion.identity);
            _uiT_LevelUp = FindObjectOfType<UIT_LevelUp>();
            _uiT_LevelUp._root.visible = true;
            _uiT_LevelUp.enabled = true;
            _uiT_LevelUp._root.SetEnabled(true);
            //PauseGame(true);
        }
        Debug.Log("Player received " + coinValue + " coins");
    }

    void UpdateLineRenderersTriangle()
    {
        if (_aim)
        {
            if (!lineRendererLeft.enabled) lineRendererLeft.enabled = true;
            if (!lineRendererRight.enabled) lineRendererRight.enabled = true;

            _currentAngleLineRenderers = Mathf.MoveTowards(
                _currentAngleLineRenderers,
                gameStats._precisionMin,
                gameStats._aimingSpeed * Time.deltaTime
            );
            if (_recoil > 0) _currentAngleLineRenderers = _recoil;
            _recoil = 0;

            UpdateLinePoints(_currentAngleLineRenderers);
            ApplyVisuals(lineRendererLeft, _meshRendererAimingCircle, _currentAngleLineRenderers);
            ApplyVisuals(lineRendererRight, _meshRendererAimingCircle, _currentAngleLineRenderers);
        }
        else
        {
            if (lineRendererLeft.enabled) lineRendererLeft.enabled = false;
            if (lineRendererRight.enabled) lineRendererRight.enabled = false;

            _currentAngleLineRenderers = gameStats._precisionStartingAim;
        }
    }
    void UpdateLinePoints(float angle)
    {
        float baseRadius = 2f;
        float lengthMultiplier = 5f;
        float currentRadius = baseRadius * lengthMultiplier;
        float halfAngle = angle / 2f;

        float yOffset = 0.03f; // <<< NOVO: da “pluta” iznad tla

        Vector3 playerPosition = transform.position;
        Quaternion playerRotation = transform.rotation;

        Vector3 triangleBase = new(playerPosition.x,
                                   _gameObjectAimingCircle.transform.position.y + yOffset,
                                   playerPosition.z);

        Vector3 leftPoint = triangleBase + playerRotation * Quaternion.Euler(0, -halfAngle, 0) * Vector3.forward * currentRadius;
        Vector3 rightPoint = triangleBase + playerRotation * Quaternion.Euler(0, halfAngle, 0) * Vector3.forward * currentRadius;

        lineRendererLeft.positionCount = 2;
        lineRendererLeft.SetPositions(new[] { triangleBase, leftPoint });

        lineRendererRight.positionCount = 2;
        lineRendererRight.SetPositions(new[] { triangleBase, rightPoint });
    }

    void SetLineAlpha(LineRenderer lineRenderer, float alpha)
    {
        //Debug.Log("Setting alpha to: " + alpha);
        // Postavite alpha vrednost za LineRenderer
        Color startColor = lineRenderer.startColor;
        Color endColor = lineRenderer.endColor;

        startColor.a = alpha;
        endColor.a = alpha;

        lineRenderer.startColor = startColor;
        lineRenderer.endColor = endColor;
    }


    Color GetInterpolatedColor(float angle)
    {
        float minAngle = gameStats._precisionMin;
        float maxAngle = gameStats._precisionMax; ;

        float normalizedValue = Mathf.InverseLerp(minAngle, maxAngle, angle);

        // Define darker, more muted colors
        Color deepPurple = new(0.4f, 0f, 0.4f);   // Dark Purple
        Color darkRed = new(0.5f, 0f, 0f);        // Dark Red
        Color burntOrange = new(0.6f, 0.25f, 0f); // Burnt Orange
        Color mutedYellow = new(0.5f, 0.4f, 0f);  // Muted Yellow
        Color darkGreen = new(0f, 0.3f, 0f);      // Dark Green

        // Interpolate colors
        if (normalizedValue > 0.8f)
            return Color.Lerp(darkRed, deepPurple, Mathf.InverseLerp(0.8f, 1f, normalizedValue));
        else if (normalizedValue > 0.6f)
            return Color.Lerp(burntOrange, darkRed, Mathf.InverseLerp(0.6f, 0.8f, normalizedValue));
        else if (normalizedValue > 0.4f)
            return Color.Lerp(mutedYellow, burntOrange, Mathf.InverseLerp(0.4f, 0.6f, normalizedValue));
        else if (normalizedValue > 0.2f)
            return Color.Lerp(darkGreen, mutedYellow, Mathf.InverseLerp(0.2f, 0.4f, normalizedValue));
        else
            return darkGreen;
    }


    void ApplyVisuals(LineRenderer lineRenderer, MeshRenderer meshRenderer, float angle)
    {
        Color newColor = GetInterpolatedColor(angle);

        // Darken LineRenderer color without transparency (make it a little darker)
        Color darkerColor = newColor * 0.8f; // Darken the color by 20%

        // Apply to LineRenderer
        Gradient gradient = new();
        gradient.SetKeys(
            new GradientColorKey[] { new(darkerColor, 0f), new(darkerColor, 1f) },
            new GradientAlphaKey[] { new(0.9f, 0f), new(0.9f, 1f) } // Increased alpha to make it more solid
        );
        lineRenderer.colorGradient = gradient;

        // Apply to MeshRenderer with transparency for glow effect (not applied to LineRenderer)
        if (meshRenderer != null && meshRenderer.material != null)
        {
            Color transparentColor = newColor;
            transparentColor.a = 0.2f; // 60% opacity

            meshRenderer.material.color = transparentColor;
            meshRenderer.material.SetFloat("_Mode", 3); // Enable transparency
            meshRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            meshRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            meshRenderer.material.SetInt("_ZWrite", 0);
            meshRenderer.material.DisableKeyword("_ALPHATEST_ON");
            meshRenderer.material.EnableKeyword("_ALPHABLEND_ON");
            meshRenderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            meshRenderer.material.renderQueue = 3000; // Transparent queue

            // ✨ Add Glow Effect
            meshRenderer.material.EnableKeyword("_EMISSION");
            meshRenderer.material.SetColor("_EmissionColor", newColor * 2f); // Glow effect
        }
    }





    void ConfigureLineRenderer(LineRenderer lineRenderer)
    {
        // Postavite LineRenderer
        lineRenderer.positionCount = 2; // Dve tačke za liniju
        lineRenderer.startWidth = 0.051f; // Širina linije
        lineRenderer.endWidth = 0.01f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // Shader
        lineRenderer.startColor = Color.green; // Početna boja
        lineRenderer.endColor = Color.green;   // Krajnja boja
    }

    private float GetCurrentAngleBetweenLineRenderersNOTUSED(LineRenderer leftLR, LineRenderer rightLR)
    {
        Vector3 leftPoint = leftLR.GetPosition(1);
        Vector3 rightPoint = rightLR.GetPosition(1);

        Vector3 direction = rightPoint - leftPoint;
        float angle = Vector3.Angle(direction, Vector3.forward);
        Debug.Log("Angle between line renderers: " + angle);
        return angle;
    }

    void GenerateMesh2(float angle)
    {
        int vertexCount = (_resolution + 1) * 2;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[_resolution * 6];

        float angleRad = Mathf.Deg2Rad * angle;
        float halfAngle = angleRad / 2f;

        for (int i = 0; i <= _resolution; i++)
        {
            float t = i / (float)_resolution;
            float currentAngle = -halfAngle + (t * angleRad);

            // Inner point is always at origin — for sharp base
            float xInner = 0f;
            float zInner = 0f;

            // Pointiness bias toward the center
            float normalizedCenterBias = Mathf.Cos(currentAngle / halfAngle * Mathf.PI / 2f);
            float dynamicOuterRadius = _outerRadius + _pointyTipFactor * normalizedCenterBias;

            float xOuter = Mathf.Sin(currentAngle) * dynamicOuterRadius;
            float zOuter = Mathf.Cos(currentAngle) * dynamicOuterRadius;

            vertices[i * 2] = new Vector3(xInner, 0f, zInner); // Inner (origin)
            vertices[i * 2 + 1] = new Vector3(xOuter, 0f, zOuter); // Outer (pointy edge)
        }

        int index = 0;
        for (int i = 0; i < _resolution; i++)
        {
            int innerStart = i * 2;
            int outerStart = i * 2 + 1;
            int innerNext = (i + 1) * 2;
            int outerNext = (i + 1) * 2 + 1;

            triangles[index++] = innerStart;
            triangles[index++] = outerStart;
            triangles[index++] = innerNext;

            triangles[index++] = outerStart;
            triangles[index++] = outerNext;
            triangles[index++] = innerNext;
        }

        Color[] colors = GenerateDirectionalSweepColors(vertexCount, _resolution, angle);

        _meshAimingCircle.Clear();
        _meshAimingCircle.vertices = vertices;
        _meshAimingCircle.triangles = triangles;
        _meshAimingCircle.RecalculateNormals();
        _meshAimingCircle.colors = colors;

        _meshColliderAimingCircle.sharedMesh = null;
        _meshColliderAimingCircle.sharedMesh = _meshAimingCircle;
    }
    private Color[] GenerateDirectionalSweepColors(int vertexCount, int resolution, float angle)
    {
        Color[] colors = new Color[vertexCount];
        float currentTime = Time.time;
        float halfAngle = angle / 2f;

        // Compute sweep position
        float sweepT = Mathf.PingPong(currentTime / (_sweepDuration / 2f), 1f);
        float previousSweepT = Mathf.PingPong((currentTime - Time.deltaTime) / (_sweepDuration / 2f), 1f);
        float sweepDir = sweepT - previousSweepT;
        bool isSweepingRight = sweepDir > 0;

        // Detect if we're at the edge and should pause
        bool hitRightEdge = sweepT >= 1f;
        bool hitLeftEdge = sweepT <= 0f;

        if ((hitRightEdge || hitLeftEdge) && !_isPausedAtEdge)
        {
            _isPausedAtEdge = true;
            _lastSweepEndTime = currentTime;
            _pauseOnRightEdge = hitRightEdge;
        }

        // Apply pause
        if (_isPausedAtEdge)
        {
            float pauseTimeElapsed = currentTime - _lastSweepEndTime;
            if (pauseTimeElapsed < _sweepPauseDuration)
            {
                sweepT = _pauseOnRightEdge ? 1f : 0f;
            }
            else
            {
                _isPausedAtEdge = false;
            }
        }

        // Calculate actual beam position
        float sweepAngle = Mathf.Lerp(-halfAngle, halfAngle, sweepT);
        float lineHalfWidthDeg = angle * _sweepLineWidthPct / 2f;
        float trailMaxLengthDeg = angle * _sweepTrailWidthPct;

        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            float vertexAngle = Mathf.Lerp(-halfAngle, halfAngle, t);
            float angleOffset = vertexAngle - sweepAngle;

            float alpha;
            bool isBehind = isSweepingRight ? (vertexAngle < sweepAngle) : (vertexAngle > sweepAngle);

            if (Mathf.Abs(angleOffset) <= lineHalfWidthDeg)
            {
                // Beam line
                alpha = _sweepLineAlpha;
            }
            else if (isBehind && Mathf.Abs(angleOffset) <= trailMaxLengthDeg)
            {
                // Fading trail behind beam
                float fadeT = Mathf.InverseLerp(lineHalfWidthDeg, trailMaxLengthDeg, Mathf.Abs(angleOffset));
                alpha = Mathf.Lerp(_sweepLineAlpha, _sweepTrailAlpha, fadeT);
            }
            else
            {
                alpha = 0f; // ahead of beam or outside trail
            }

            Color finalColor = new Color(_sweepColor.r, _sweepColor.g, _sweepColor.b, alpha);
            colors[i * 2] = finalColor;
            colors[i * 2 + 1] = finalColor;
        }

        return colors;
    }





    private IEnumerator ApplyMovementPenalty(float percentageIncrease, float interval)
    {
        while (true)
        {
            float penaltyAmount = _currentAngleLineRenderers * (percentageIncrease / 100f);

            // Ensure penalty is applied even at min precision
            if (_currentAngleLineRenderers <= gameStats._precisionMin)
            {
                penaltyAmount = gameStats._precisionMin * (percentageIncrease / 100f);
            }

            _currentAngleLineRenderers += penaltyAmount;
            _currentAngleLineRenderers = Mathf.Min(_currentAngleLineRenderers, gameStats._precisionMax); // Clamp max value

            //Debug.Log($"Movement penalty applied: +{penaltyAmount}°, New angle: {_currentAngleLineRenderers}°");

            yield return new WaitForSeconds(interval);
        }
    }


    void UpdateMovementPenalty(bool isWalking, bool isRunning, bool isStrafing)
    {
        // Stop any existing penalty coroutine
        if (_movementPenaltyCoroutine != null)
        {
            StopCoroutine(_movementPenaltyCoroutine);
            _movementPenaltyCoroutine = null;
        }

        if (isRunning)
        {
            _movementPenaltyCoroutine = StartCoroutine(ApplyMovementPenalty(15f, 0.5f)); // 15% every 0.5s
        }
        else if (isWalking || isStrafing)
        {
            _movementPenaltyCoroutine = StartCoroutine(ApplyMovementPenalty(5f, 0.8f)); // 5% every 0.8s
        }
    }

    private IEnumerator DashCoroutine()
    {
        isDashing = true;
        lastDashTime = Time.time;

        Vector3 dashDirection = _direction.normalized; // Get current movement direction
        float dashSpeed = dashDistance / dashDuration; // Calculate instant speed needed

        StartCoroutine(SpawnGhosts());

        float elapsedTime = 0f;

        while (elapsedTime < dashDuration)
        {
            //_characterController.Move(dashDirection * dashSpeed * Time.deltaTime);
            _characterController.Move(dashDirection * (Time.deltaTime * dashSpeed));

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
    }
    IEnumerator SpawnGhosts()
    {
        if (motionBlur != null)
        {
            motionBlur.active = true;
            motionBlur.intensity.Override(0.8f); // Enable strong blur
        }

        int totalGhosts = 8;
        for (int i = 0; i < totalGhosts; i++)
        {
            CreateGhost(i, totalGhosts);
            yield return new WaitForSeconds(0.02f); // Delay for smooth trail effect
        }

        StartCoroutine(FadeOutMotionBlur(0.3f)); // After ghosts disappear, fade out motion blur
    }
    void CreateGhost(int ghostIndex, int totalGhosts)
    {
        SkinnedMeshRenderer[] skinnedMeshes = GetComponentsInChildren<SkinnedMeshRenderer>();
        if (skinnedMeshes.Length == 0) return;

        Material ghostMaterial = Resources.Load<Material>("Material/GhostMaterial");
        if (ghostMaterial == null)
        {
            Debug.LogError("❌ GhostMaterial not found in Resources/Material!");
            return;
        }

        float scaleFactor = Mathf.Lerp(0.6f, 1.0f, (float)ghostIndex / (totalGhosts - 1));
        float alpha = Mathf.Lerp(0.3f, 0.8f, (float)ghostIndex / (totalGhosts - 1)); // Fade over time

        GameObject ghostParent = new("Ghost");
        ghostParent.transform.SetPositionAndRotation(transform.position, transform.rotation);
        ghostParent.transform.localScale = Vector3.one * scaleFactor;


        foreach (SkinnedMeshRenderer skinnedMesh in skinnedMeshes)
        {
            Mesh bakedMesh = new();
            skinnedMesh.BakeMesh(bakedMesh);

            GameObject ghost = new("GhostMesh");
            ghost.transform.SetParent(ghostParent.transform);
            ghost.transform.position = skinnedMesh.transform.position;
            ghost.transform.rotation = skinnedMesh.transform.rotation;
            ghost.transform.localScale = Vector3.one * scaleFactor;

            MeshFilter ghostMeshFilter = ghost.AddComponent<MeshFilter>();
            ghostMeshFilter.mesh = bakedMesh;

            MeshRenderer ghostRenderer = ghost.AddComponent<MeshRenderer>();
            Material newMaterial = new(ghostMaterial) { color = new Color(0.2f, 0.2f, 0.2f, alpha) };
            ghostRenderer.material = newMaterial;
        }

        Destroy(ghostParent, 0.3f); // Destroy after delay
    }

    IEnumerator FadeOutMotionBlur(float duration)
    {
        float startIntensity = motionBlur.intensity.value;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            motionBlur.intensity.Override(Mathf.Lerp(startIntensity, 0f, elapsed / duration));
            yield return null;
        }

        motionBlur.intensity.Override(0f); // Ensure it's fully off
    }

    private GameObject GetClosestZombie(float aimAssistRange = 2000f, float maxVisibilityAngle = 120f)
    {
        try
        {
            GameObject closestZombie = null;
            float closestDistance = Mathf.Infinity;
            foreach (GameObject zombie in onScreenZombies)
            {
                bool inRange = false;
                bool inAngle = false;
                bool inSight = false;
                inRange = ZombieInRange(zombie.transform.position, aimAssistRange);
                if (inRange)
                    inAngle = ZombieInAimingAngle(zombie.transform.position, maxVisibilityAngle);
                if (inRange && inAngle)
                    inSight = ZombieInClearLineOfSight(zombie.transform.position, zombie);
                if (inRange && inAngle && inSight)
                {
                    float distance = (zombie.transform.position - transform.position).magnitude;
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestZombie = zombie;
                    }
                }
            }
            if (closestZombie != null)
            {
                _currentTarget = closestZombie;
                return closestZombie;
            }
            else
                return null;
        }
        catch (Exception e)
        {
            Debug.LogError("Error in GetClosestZombie: " + e.Message);
            return null;
        }

        bool ZombieInAimingAngle(Vector3 zombiePosition, float maxAngle)
        {
            Vector3 direction = zombiePosition - transform.position;
            float angle = Vector3.Angle(transform.forward, direction);
            if (angle <= maxAngle)
            {
                //Debug.Log("Zombie in aiming angle: " + angle);
                return true;
            }
            else
            {
                Debug.Log("Zombie out of aiming angle: " + angle);
                return false;
            }
        }

        bool ZombieInRange(Vector3 zombiePosition, float range)
        {
            Vector3 direction = zombiePosition - transform.position;
            //Debug.Log("Distance to zombie: " + direction.magnitude);
            if (direction.magnitude <= range)
            {
                return true;
            }
            else
            {
                Debug.Log("Zombie out of range: " + direction.magnitude);
                return false;
            }
        }

        void PrintAllBones(Transform obj, string objectName, string prefix = "")
        {
            foreach (Transform child in obj)
            {
                if (child.name.Contains("Head"))
                {
                    Debug.Log(prefix + child.name + " at position: " + child.position);
                }
                PrintAllBones(child, objectName, prefix + "--"); // Recursively print with indentation
            }
        }
    }


    /// <summary>
    /// Search for hear bone
    /// </summary>
    /// <param name="targetPosition"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    bool ZombieInClearLineOfSight(Vector3 targetPosition, GameObject target)
    {
        _closestZombie = target;
        //Transform playerHead = GetBonePostitionFromObject("Head", gameObject);
        Transform playerHead = GetWeaponPostitionFromPlayer();
        Transform zombieHead = GetBonePostitionFromObject("mixamorig:Head", target);
        Vector3 directionToZombie = zombieHead.position - playerHead.position;
        Vector3 rayStart = playerHead.position + directionToZombie * 0.2f;
        float rayDistance = Vector3.Distance(rayStart, zombieHead.position);

        //PrintAllBones(target.transform, closestZombie.name);

        //Debug.DrawRay(playerHead.position, directionToZombie, Color.red, 0.3f);
        Debug.DrawRay(rayStart, directionToZombie * rayDistance, Color.yellow, 0.3f);
        //if (Physics.Raycast(playerHead.position, directionToZombie, out RaycastHit hit, directionToZombie.magnitude * 2))
        if (Physics.Raycast(rayStart, directionToZombie, out RaycastHit hit, rayDistance))
        {
            if (hit.collider.gameObject.CompareTag("Zombie"))
            {
                //Debug.Log("Zombie in clear line of sight: " + hit.collider.gameObject.name);
                return true;
            }
            else
            {
                Debug.Log("Zombie not in clear line of sight: " + hit.collider.gameObject.name);
                return false;
            }
        }
        Debug.Log("No zombie in clear line of sight - 2");
        return false;
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Transform playerHead = GetBonePostitionFromObject("Head", gameObject);
            Transform zombieHead = _closestZombie != null ? GetBonePostitionFromObject("mixamorig:Head", _closestZombie) : null;

            if (playerHead != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(playerHead.position, 0.2f); // Player head
            }

            if (zombieHead != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(zombieHead.position, 0.2f); // Zombie head
            }
        }
    }
    void DebugEverySecond(string message)
    {
        if (Time.time - _lastLogTime >= 1f)
        {
            Debug.Log(message);
            _lastLogTime = Time.time;
        }
    }
    void SetupLaser(LineRenderer lr)
    {
        lr.positionCount = 2;
        lr.widthMultiplier = baseWidth;
        lr.textureMode = LineTextureMode.Tile;
        lr.numCapVertices = 4;      // za zaobljene krajeve
        lr.numCornerVertices = 2;
        lr.alignment = LineAlignment.View;

        // Gradijent (duž linije): toplo belo ka crvenom
        var g = new Gradient();
        g.SetKeys(
            new[] {
            new GradientColorKey(Color.white, 0f),
            new GradientColorKey(laserColor, 0.35f),
            new GradientColorKey(laserColor, 0.65f),
            new GradientColorKey(Color.white, 1f),
            },
            new[] {
            new GradientAlphaKey(0.0f, 0f),
            new GradientAlphaKey(1.0f, 0.08f),
            new GradientAlphaKey(1.0f, 0.92f),
            new GradientAlphaKey(0.0f, 1f),
            }
        );
        lr.colorGradient = g;

        lr.material = laserMat;
    }

    void AnimateLaser(LineRenderer lr, Material mat)
    {
        // Scroll UV (strujanje)
        float t = Time.time;
        Vector2 o = mat.mainTextureOffset;
        o.x = -t * scrollSpeed;
        mat.mainTextureOffset = o;

        // Puls širine + emisija
        float pulse = Mathf.Lerp(pulseMin, pulseMax, 0.5f + 0.5f * Mathf.Sin(t * pulseSpeed));
        lr.widthMultiplier = baseWidth * pulse;

        float e = emissionBase + Mathf.Sin(t * (pulseSpeed * 1.3f)) * emissionPulse;
        mat.SetColor("_EmissionColor", laserColor * e);
    }

    // 1D “laser core” tekstura (horizontalni gradijent, centar vreo, ivice providne)
    Texture2D MakeLaserTexture(int width, float sharpness)
    {
        var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        for (int x = 0; x < width; x++)
        {
            float u = (x + 0.5f) / width;         // 0..1
            float d = Mathf.Abs(u - 0.5f) * 2f;   // 0 u centru, 1 na ivicama
            float a = Mathf.Exp(-Mathf.Pow(d * sharpness, 2f)); // gauss “hot core”
                                                                // boja: skoro bela u sredini, crvenije ka ivici
            Color c = Color.Lerp(laserColor, Color.white, Mathf.Pow(1f - d, 3f));
            c.a = a; // alpha kontrola žara
            tex.SetPixel(x, 0, c);
        }
        tex.Apply(false, false);
        return tex;
    }
    void SetAimLinesActive(bool on)
    {
        if (lineRendererLeft != null) lineRendererLeft.enabled = on;
        if (lineRendererRight != null) lineRendererRight.enabled = on;

        // (opciono) ugasi emisiju kad nije aktivno, da ne “sija”
        if (laserMat != null)
        {
            var e = on ? (emissionBase) : 0f;
            laserMat.SetColor("_EmissionColor", laserColor * e);
        }
    }

    void UpdateAimingVisuals()
    {
        if (_aim)
        {
            // Uključi linije samo dok se nišani
            SetAimLinesActive(true);

            // Ažuriraj ugao i pozicije
            _currentAngleLineRenderers = Mathf.MoveTowards(
                _currentAngleLineRenderers,
                gameStats._precisionMin,
                gameStats._aimingSpeed * Time.deltaTime
            );

            if (_recoil > 0) { _currentAngleLineRenderers = _recoil; _recoil = 0; }

            UpdateLinePoints(_currentAngleLineRenderers);
            ApplyVisuals(lineRendererLeft, _meshRendererAimingCircle, _currentAngleLineRenderers);
            ApplyVisuals(lineRendererRight, _meshRendererAimingCircle, _currentAngleLineRenderers);

            // Laser animacija samo kad je upaljeno nišanjenje
            AnimateLaser(lineRendererLeft, laserMat);
            AnimateLaser(lineRendererRight, laserMat);
        }
        else
        {
            // Skloni linije kad bežiš / ne nišaniš
            SetAimLinesActive(false);
            _currentAngleLineRenderers = gameStats._precisionStartingAim;
            if (_laserSmokeLeft.isPlaying) _laserSmokeLeft.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (_laserSmokeRight.isPlaying) _laserSmokeRight.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        }
    }

    void EnsureLineRenderersOnTop()
    {
        // Ako želiš bezbedno rešenje bez menjanja materijala:
        lineRendererLeft.sortingOrder = 5000;
        lineRendererRight.sortingOrder = 5000;

        // Ako želiš da zaista ignoriše Z-test (dok ne nišaniš su anyway disabled):
        if (laserMat != null)
        {
            laserMat.SetInt("_ZWrite", 0); // Transparent
            laserMat.renderQueue = 3000;   // Transparent queue
            var zTestAlways = (int)UnityEngine.Rendering.CompareFunction.Always;
            laserMat.SetInt("_ZTest", zTestAlways); // CRTAJ UVEK IZNAD
        }
    }
    // Call this once per frame AFTER UpdateLinePoints(...)
    void RefreshAimLineWidth()
    {
        if (!lineRendererLeft || !lineRendererRight || _mainCamera == null) return;

        // pick a point near the player/base of the V to measure distance
        Vector3 basePoint = lineRendererLeft.positionCount > 0
            ? lineRendererLeft.GetPosition(0)
            : transform.position;

        float dist = Vector3.Distance(_mainCamera.transform.position, basePoint);
        float worldWidth = GetWorldWidthForPixels(_mainCamera, dist, aimLinePixels);

        worldWidth = Mathf.Clamp(worldWidth, minWorldWidth, maxWorldWidth);

        lineRendererLeft.startWidth = lineRendererLeft.endWidth = worldWidth;
        lineRendererRight.startWidth = lineRendererRight.endWidth = worldWidth;
    }

    // Convert desired pixel width to world-space width at a given distance
    float GetWorldWidthForPixels(Camera cam, float distance, int pixels)
    {
        if (cam.orthographic)
        {
            // world height on screen = 2 * orthographicSize
            float worldPerPixel = (2f * cam.orthographicSize) / Screen.height;
            return pixels * worldPerPixel;
        }
        else
        {
            // world height at distance = 2 * d * tan(fov/2)
            float worldHeight = 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float worldPerPixel = worldHeight / Screen.height;
            return pixels * worldPerPixel;
        }
    }
    void AlignEmitterToLine(ParticleSystem ps, LineRenderer lr, float thickness)
    {
        if (lr.positionCount < 2) return;

        Vector3 a = lr.GetPosition(0);
        Vector3 b = lr.GetPosition(1);
        Vector3 dir = b - a;
        float len = dir.magnitude;
        if (len < 0.01f) return;

        // Position in the middle of the line
        var t = ps.transform;
        t.position = a + dir * 0.5f;
        t.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

        // Stretch the emitter shape along the line
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(thickness, thickness, Mathf.Max(0.1f, len));

        // Emission density proportional to line length
        var em = ps.emission;
        em.rateOverTime = 20f * len; // adjust 20f to your taste
    }
    void InitLaserSmoke()
    {
        var prefab = Resources.Load<GameObject>(laserSmokePrefabPath);
        if (!prefab)
        {
            Debug.LogError("Smoke prefab not found at Resources/" + laserSmokePrefabPath);
            return;
        }

        // Create instances as children of this object (player/controller)
        var leftGO = Instantiate(prefab, transform);
        leftGO.name = "LaserSmoke_Left";
        var rightGO = Instantiate(prefab, transform);
        rightGO.name = "LaserSmoke_Right";

        _laserSmokeLeft = leftGO.GetComponent<ParticleSystem>();
        _laserSmokeRight = rightGO.GetComponent<ParticleSystem>();

        // Stop initially — will only play while aiming
        _laserSmokeLeft.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _laserSmokeRight.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}

[Serializable]
public struct Movement
{
    [HideInInspector] public bool isSprinting;
    public float speed;
    public float multiplier;
    public float acceleration;
    [HideInInspector] public float currentSpeed;
}
public enum AimingType
{
    Mouse,
    ControllerRightStick,
    ControllerTrigger
}
