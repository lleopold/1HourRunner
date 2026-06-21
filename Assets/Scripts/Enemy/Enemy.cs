using Assets.Scripts.HealthSystem;
using DamageNumbersPro;
using LUZEMRIK.BloodDecals;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using ZombieGame;
using Random = UnityEngine.Random;


public class Enemy : MonoBehaviour, IGetHealthSystemArmour
{
    private NavMeshAgent zombieNavMeshAgent;
    public bool dieCondition = false;
    [SerializeField] float _health = 100;
    [SerializeField] Quaternion rotationModification = Quaternion.Euler(270f, 0f, 0f);
    //private HealthSystemArmour _healthSystem;

    EnemyConfig _enemyConfig = null;
    public GameObject _player;
    GameObject _gathering;
    private PlayerConfig playerConfig;
    private int _amountOfRuns;
    private bool isAttacking = false;
    public GameObject Target { get; set; }

    //State machine
    private StateMachine _stateMachine;
    private MonoBehaviour _monoBehaviour;
    private bool _isRoaming;
    public bool AttackFinished { get; set; }
    private Animator _animator;
    private bool _spawnedCoin = false;
    private int _hit; //-1 return from hit, 0 do nothing, 1 hit
    private Outline _outline;
    public HealthBarScroll healthBarScroll;
    private Transform healthBarUI;
    private bool _healthBarVisible = false; // Added: track reveal state


    public BloodDecalAsset _puddles;


    private Rigidbody[] _ragdollBodies;
    private Collider[] _ragdollColliders;
    //[SerializeField] private Animator _animator;
    private static DamageNumber _damageNumberPrefab;
    private static DamageNumber _damageNumberPrefab_crit;
    private static AudioClip _hitClip;
    public EnemyAlertIndicator AlertIndicator { get; private set; }
    private EnemyAnimator _enemyAnimator;
    private static Enemy _closestEnemy;
    private static int _lastClosestFrame = -1;

    // Stagger debug — last values applied, shown in OnGUI
    private static Enemy _lastHitEnemy;
    private float _dbgWeaponStagger = 0f;
    private float _dbgEffStagger = 0f;
    private float _dbgStaggerSpeed = -1f;
    private float _dbgLastStaggerTime = -999f;
    private float _dbgMinVelSinceHit = 999f;

    // Stagger VFX — electric crackle above head, lives while the zombie is recovering its speed
    [SerializeField] private float _staggerVfxHeight = 2.0f;
    private static GameObject _staggerVfxPrefab;
    private GameObject _staggerVfxInstance;
    private bool _staggerActive;

    // Knockdown — a crit floors the zombie (Fall→GettingUp anim). The next hit while it's down
    // is a guaranteed point-blank crit. Re-knockdown is blocked until it's up + a short cooldown,
    // so fast weapons can't chain-stunlock or farm crits.
    [SerializeField] private float _knockdownCooldown = 0.5f;    // grace after standing up before it can be floored again
    [SerializeField] private float _knockdownMaxDuration = 4f;   // safety auto-recover if anim states never report done
    private bool _knockedDown;
    private bool _pendingGuaranteedCrit;
    private bool _seenDownAnim;                                  // confirms the fall/getup actually played before we recover
    private float _knockdownStart;
    private float _knockdownCooldownUntil = -999f;

    public bool IsKnockedDown => _knockedDown;
    public bool HasPendingGuaranteedCrit => _pendingGuaranteedCrit;
    // Consumed once by the shooter — turns the shot into a forced crit, then clears.
    public bool ConsumeGuaranteedCrit()
    {
        if (!_pendingGuaranteedCrit) return false;
        _pendingGuaranteedCrit = false;
        return true;
    }

    private void Awake()
    {
        Debug.Log("Enemy Awake");

        CacheRagdollParts();
        DisableRagdoll();

        zombieNavMeshAgent = GetComponent<NavMeshAgent>();
        zombieNavMeshAgent.updateRotation = true;
        _animator = GetComponent<Animator>();

        AttackFinished = false;
        _monoBehaviour = this;
        _stateMachine = new StateMachine();

        Transform healthBarCanvasTransform = transform.Find("HealthBarCanvas_2");
        GameObject healthBarCanvas = healthBarCanvasTransform.gameObject;
        healthBarUI = healthBarCanvas.transform.Find("HealthBarUI");
        healthBarScroll = healthBarUI.GetComponent<HealthBarScroll>();

        // Ensure a CanvasGroup exists and start hidden (alpha 0)
        var cg = healthBarUI.GetComponent<CanvasGroup>();
        if (cg == null) cg = healthBarUI.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        _healthBarVisible = false;

        string prefabName = gameObject.name;
        _player = GameObject.FindWithTag("Player");
        if (_player == null) _player = GameObject.Find("Player");
        Target = _player;

        PlayerConfigManager playerConfigManager = Resources.Load<PlayerConfigManager>("Config/Player/PlayerConfigManager"); // Load the config manager
        if (_player != null)
        {
            playerConfig = playerConfigManager.GetConfig(DataHolder.ChosenPlayer.ToString());
        }

        EnemyConfigManager configManager = Resources.Load<EnemyConfigManager>("Config/Enemy/EnemyConfigManager"); // Load the config manager
        _enemyConfig = configManager.GetConfig(prefabName.Replace("(Clone)", ""));//MRS

        _health = _enemyConfig.health;
        _amountOfRuns = _enemyConfig.maxAmountOfRuns; //how many times the zombie can run

        _gathering = GameObject.Find("GatheringPoint");

        _enemyAnimator = new EnemyAnimator(_animator);

        var searchForVictim = new SearchForVictim(this, _player);
        var walkToSelected = new WalkToSelected(this, zombieNavMeshAgent, _animator, _enemyConfig, _player.transform, _enemyAnimator);
        var attackFreely = new AttackFreely(this, _player, _animator, _enemyConfig, _monoBehaviour, _enemyAnimator);
        var turnToPlayer = new TurnToPlayer(this, zombieNavMeshAgent, _animator, _enemyConfig, _enemyAnimator);
        var stop = new Stop(this, zombieNavMeshAgent, _enemyAnimator);
        var idleZombie = new IdleZombie(_enemyAnimator, zombieNavMeshAgent); // New state
        var startMoving = new StartMoving(this, zombieNavMeshAgent, _enemyAnimator);
        var fullStop = new FullStop(this, zombieNavMeshAgent, _enemyAnimator);
        // Gathering states are currently unused (no active transitions reference them).
        // Only build them when a GatheringPoint exists, so a missing marker doesn't crash Awake.
        SearchForGathering searchForGatheringSpot = null;
        WalkToGathering walkToGathering = null;
        if (_gathering != null)
        {
            searchForGatheringSpot = new SearchForGathering(this, _gathering);
            walkToGathering = new WalkToGathering(this, zombieNavMeshAgent, _animator, _enemyConfig, _gathering.transform, _enemyAnimator);
        }
        var roam = new Roam(this, _player.transform, zombieNavMeshAgent, _animator, _enemyConfig, _monoBehaviour, _enemyAnimator);


        // 0. 
        At(stop, walkToSelected, FarToPlayer);
        // 1. When close, start braking
        At(walkToSelected, stop, CloseToPlayer);

        // 2. Once braking is done, go to stable Idle
        At(stop, idleZombie, () => stop.CanExit);

        // 3. From Idle, choose what to do next
        At(idleZombie, turnToPlayer, CloseToPlayer); // turn to face the player BEFORE attacking
        At(idleZombie, startMoving, FarToPlayer);

        // 4. If we lose the player while stopping/idling, go back to searching
        At(idleZombie, searchForVictim, () => !HasTarget());

        At(searchForVictim, startMoving, FarToPlayer);
        At(startMoving, walkToSelected, () => startMoving.CanExit);

        // After attack: always turn to face the player first
        At(attackFreely, turnToPlayer, FinishedAttack);

        // After turning (now facing the player): attack if close, start moving if far
        At(turnToPlayer, attackFreely, () => turnToPlayer.CanExit && CloseToPlayer());
        At(turnToPlayer, startMoving, () => turnToPlayer.CanExit && FarToPlayer());




        //_stateMachine.AddTransition(searchForVictim, walkToSelected, HasTarget);
        //At(searchForVictim, walkToSelected, FarToPlayer);
        //At(searchForGatheringSpot, walkToGathering, FarToGathering);
        //At(walkToGathering, stop, CloseToGather);    //TODO
        //At(searchForVictim, walkToSelected, HasTarget);

        //At(walkToSelected, fullStop, CloseToPlayer);

        //At(walkToSelected, stop, CloseToPlayer);
        //At(attackFreely, stop, FinishedAttack);
        //At(roam, searchForVictim, StopRoaming);



        //At(walkToSelected, attackFreely, CloseToPlayer);

        //At(searchForGatheringSpot, walkToGathering, FarToGathering);
        //At(attackFreely, walkToGathering, CloseToGather);
        //At(walkToGathering, stop, CloseToGather); //TODO
        //At(walkToSelected, stop, CloseToGather);

        //_stateMachine.SetState(searchForGatheringSpot);
        //_stateMachine.SetState(walkToGathering); 
        _stateMachine.SetState(searchForVictim);

        void At(IState from, IState to, Func<bool> condition) => _stateMachine.AddTransition(from, to, condition);

        //Func<bool> HasTarget() => () => Target != null;
        //Func<bool> CloseToPlayer() => () => Vector3.Distance(transform.position, player.position) < _enemyConfig.meleeRadius;
        //At(moveToVictim, , HasTarget());/test

        _outline = gameObject.AddComponent<Outline>();
        _outline.OutlineMode = Outline.Mode.OutlineAll;
        _outline.OutlineColor = Color.red;
        _outline.OutlineWidth = 2.5f;
        _outline.enabled = false;


        if (BloodDecalManager.Instance == null)
        {
            GameObject bloodDecalManagerGO = new GameObject("BloodDecalManager");
            bloodDecalManagerGO.AddComponent<BloodDecalManager>();
        }
        CreateDamageNumber();

        AlertIndicator = gameObject.AddComponent<EnemyAlertIndicator>();
        AlertIndicator.worldOffset = new Vector3(0f, 2.1f, 0f); // tune per model height
        AlertIndicator.prefabPath = "Indicators/Exclamation";
        AlertIndicator.EnsureReady();

    }

    public void SetOutline(bool newSetting)
    {
        _outline.enabled = newSetting;
    }
    public void SetOutlineColor(Color c)
    {
        _outline.OutlineColor = c;
    }
    public bool FinishedAttack()
    {
        return AttackFinished;
    }
    public bool IsAttackFinished()
    {
        return AttackFinished;
    }
    public bool CloseToPlayer()
    {
        if (_player == null) return false;
        var retval = Vector3.Distance(transform.position, _player.transform.position) < _enemyConfig.meleeRadius;
        if (retval)
        {
            //Debug.Log("CloseToPlayer:" + retval);
        }
        return retval;
    }
    public bool CloseToGather()
    {
        var retval = Vector3.Distance(transform.position, _gathering.transform.position) < _enemyConfig.meleeRadius;
        if (retval)
        {
            Debug.Log("CloseToGather:" + retval);
        }
        return retval;
    }
    public bool FarToPlayer()
    {
        if (_player == null) return false;
        var retval = Vector3.Distance(transform.position, _player.transform.position) > _enemyConfig.meleeRadius;
        if (retval)
        {
            //Debug.Log("FarToPlayer:" + retval);
        }
        return retval;
    }
    public bool FarToGathering()
    {
        var retval = Vector3.Distance(transform.position, _gathering.transform.position) > _enemyConfig.meleeRadius;
        if (retval)
        {
            Debug.Log("FarToGathering:" + retval);
        }
        return retval;
    }
    public bool StopRoaming()
    {
        return false;
    }

    public bool HasTarget()
    {
        if (Target == null)
        {
            //Debug.Log("HasTarget: false");
        }
        else
        {
            //Debug.Log("HasTarget: true");
        }
        return Target != null;
    }
    public bool Always()
    {
        return true;
    }
    public bool Never()
    {
        return false;
    }


    void Start()
    {
        healthBarScroll.healthSystemArmour.Initialize(_health, 0);
        dieCondition = false;
        zombieNavMeshAgent.speed = _enemyConfig.speed;
        zombieNavMeshAgent.acceleration = MapAcceleration(_enemyConfig.acceleration);
        if (_player == null)
        {
            Debug.LogError("Player reference is not assigned to the ZombieMovement script.");
        }
        // DO NOT deactivate healthBarUI here anymore
    }

    private void UpdateClosestEnemy()
    {
        if (Time.frameCount == _lastClosestFrame) return;
        _lastClosestFrame = Time.frameCount;

        _closestEnemy = null;
        float minDist = float.MaxValue;
        Enemy[] allEnemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        if (_player == null) return;

        foreach (var e in allEnemies)
        {
            if (e._player == null || e._health <= 0) continue;
            float d = Vector3.Distance(e.transform.position, e._player.transform.position);
            if (d < minDist)
            {
                minDist = d;
                _closestEnemy = e;
            }
        }
    }

    private void OnGUI()
    {
        // Prefer the last-hit zombie so the panel follows whatever you just shot; fall back to closest.
        Enemy panelTarget = _lastHitEnemy != null ? _lastHitEnemy : _closestEnemy;
        if (panelTarget != this) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 18;
        style.normal.textColor = Color.yellow;
        style.alignment = TextAnchor.UpperRight;
        style.fontStyle = FontStyle.Bold;

        string info = $"[CLOSEST ZOMBIE DEBUG]\n" +
                      $"Name: {gameObject.name}\n" +
                      $"State: {_stateMachine.CurrentState?.GetType().Name ?? "None"}\n" +
                      $"Health: {_health:F1}\n" +
                      $"Target: {(Target != null ? Target.name : "None")}\n" +
                      $"CloseToPlayer: {CloseToPlayer()}\n" +
                      $"FarToPlayer: {FarToPlayer()}\n" +
                      $"Velocity: {(_enemyAnimator != null ? _enemyAnimator.Velocity.ToString("F2") : "N/A")}\n" +
                      $"--- STAGGER ---\n" +
                      $"Resistance: {_enemyConfig.staggerResistance:F0}  Accel: {zombieNavMeshAgent.acceleration:F1}\n" +
                      $"Weapon Stagger: {_dbgWeaponStagger:F0}\n" +
                      $"Eff Stagger: {_dbgEffStagger:F0}\n" +
                      $"StaggerSpeed: {(_dbgStaggerSpeed >= 0 ? _dbgStaggerSpeed.ToString("F2") : "—")} / base {_enemyConfig.speed:F2}\n" +
                      $"AgentVel: {zombieNavMeshAgent.velocity.magnitude:F2}  (since hit {Time.time - _dbgLastStaggerTime:F1}s)\n" +
                      $"Dip min vel: {(_dbgMinVelSinceHit < 900 ? _dbgMinVelSinceHit.ToString("F2") : "—")}";

        // Position in upper right corner with some padding
        float width = 320;
        float height = 320;
        Rect rect = new Rect(Screen.width - width - 10, 10, width, height);

        GUI.Box(new Rect(Screen.width - width - 20, 5, width + 10, height + 10), ""); // Background box
        GUI.Label(rect, info, style);
    }

    void Update()
    {
        UpdateClosestEnemy();

        // While floored the zombie is frozen — skip the state machine so it can't fight the anim.
        if (_knockedDown) { TickKnockdown(); return; }

        _stateMachine.Tick();

        // Track lowest agent velocity since last stagger (so a fast recovery can't hide the dip)
        if (zombieNavMeshAgent != null)
        {
            float v = zombieNavMeshAgent.velocity.magnitude;
            if (v < _dbgMinVelSinceHit) _dbgMinVelSinceHit = v;
        }

        // End the stagger VFX once the agent has climbed back near full speed (or a safety timeout).
        if (_staggerActive && zombieNavMeshAgent != null && _enemyConfig != null)
        {
            float sinceHit = Time.time - _dbgLastStaggerTime;
            bool recovered = zombieNavMeshAgent.velocity.magnitude >= _enemyConfig.speed * 0.9f;
            if ((recovered && sinceHit > 0.15f) || sinceHit > 4f)
                HideStaggerVfx();
        }
        Debug.DrawRay(transform.position, transform.forward * 5, Color.red);  // Expected forward
        Debug.DrawRay(transform.position, zombieNavMeshAgent.velocity.normalized * 5, Color.green); // Movement direction

        Vector3 directionToPlayer = (_player.transform.position - transform.position).normalized;
        Debug.DrawRay(transform.position, directionToPlayer * 5, Color.blue);

        Vector3 forwardDirection = transform.forward;
        Vector3 movementDirection = zombieNavMeshAgent.velocity.normalized;
        float angleToPlayer = Vector3.Angle(forwardDirection, directionToPlayer);

        //Debug.Log("Angle to Player: " + angleToPlayer);


        if (_hit == 1)
        {
            //HitLayerPlus();
        }
        if (_hit == -1)
        {
            //HitLayerMinus();
        }
    }


    public void DamageReceived(float damage, Vector3 hitDirection, bool isCrit = false, float weaponStagger = 0f)
    {
        if (_health <= 0) return;

        bool firstReveal = !_healthBarVisible;

        // Apply damage first
        _health -= damage;
        healthBarScroll.healthSystemArmour.Damage(damage);
        if (_health < 0) _health = 0;

        if (firstReveal && healthBarUI != null)
        {
            var cg = healthBarUI.GetComponent<CanvasGroup>();
            if (cg) cg.alpha = 1f;     // show now, already updated by damage
            _healthBarVisible = true;
        }

        Hit();
        PlayHitSound();
        ApplyStagger(weaponStagger, isCrit);

        // Crit floors the zombie (only if it survived the hit and isn't already down / on cooldown).
        if (isCrit && _health > 0 && CanKnockDown())
            KnockDown();

        Vector3 damagePosition = transform.position + Vector3.up * 1.5f;
        DamageNumber numberPrefab = (isCrit && _damageNumberPrefab_crit != null)
            ? _damageNumberPrefab_crit
            : _damageNumberPrefab;
        numberPrefab.Spawn(damagePosition, damage);

        if (_health <= 0 && !_spawnedCoin)
        {
            _spawnedCoin = true;
            SpawnCoin(30);
            var zombieCollider = GetComponent<Collider>();
            if (zombieCollider) zombieCollider.enabled = false;
            Die();
        }
    }

    private void PlayHitSound()
    {
        if (_hitClip == null)
            _hitClip = Resources.Load<AudioClip>("Audio/SFX/Enemy/zombie_hit_1");

        if (_hitClip == null)
        {
            Debug.LogWarning("zombie_hit_1 not found at Resources/Audio/SFX/Enemy/zombie_hit_1");
            return;
        }

        if (SoundFXManager.Instance != null)
            SoundFXManager.Instance.PlaySoundFXClip(_hitClip, transform, _enemyConfig.hitSoundVolume);
    }

    public void Hit()
    {
        Animator zombieAnimator = GetComponent<Animator>();
        int hitLayer = zombieAnimator.GetLayerIndex("Hit");
        zombieAnimator.SetLayerWeight(hitLayer, 1f);
        zombieAnimator.SetTrigger("HitLayer");
        //zombieAnimator.SetTrigger("HitReceived");
        _hit = 1;
    }

    // Maps a 0-100 config value to a usable NavMeshAgent.acceleration.
    // Low config = sluggish recovery ramp, high = snappy. Tune the range in inspector feel.
    private static float MapAcceleration(float config0to100)
    {
        return Mathf.Lerp(2f, 40f, Mathf.Clamp01(config0to100 / 100f));
    }

    // Stagger: sharp drop to a reduced speed, then smooth climb back via NavMeshAgent.acceleration.
    // effStagger = weaponStagger (x2 on crit) - this enemy's resistance, clamped 0-100.
    private void ApplyStagger(float weaponStagger, bool isCrit)
    {
        if (_knockedDown) return; // knockdown supersedes stagger; the guaranteed-crit hit won't re-stagger

        float raw = weaponStagger * (isCrit ? 2f : 1f);
        float effStagger = Mathf.Clamp(raw - _enemyConfig.staggerResistance, 0f, 100f);

        // Debug tracking (always update so GUI reflects even fully-resisted hits)
        _dbgWeaponStagger = weaponStagger;
        _dbgEffStagger = effStagger;
        _dbgLastStaggerTime = Time.time;
        _dbgMinVelSinceHit = 999f; // reset dip tracker
        _lastHitEnemy = this;

        if (effStagger <= 0f) { _dbgStaggerSpeed = -1f; return; } // immune / fully resisted

        float baseSpeed = _enemyConfig.speed;
        float staggerSpeed = baseSpeed * (1f - effStagger / 100f);

        // Re-hit takes the deeper dip so rapid fire keeps the zombie down (no accumulator).
        staggerSpeed = Mathf.Min(zombieNavMeshAgent.velocity.magnitude, staggerSpeed);
        _dbgStaggerSpeed = staggerSpeed;

        // Fast drop: write velocity directly toward the player (bypasses acceleration = the punch).
        Vector3 toPlayer = _player != null
            ? (_player.transform.position - transform.position).normalized
            : transform.forward;
        zombieNavMeshAgent.velocity = toPlayer * staggerSpeed;

        // Smooth recovery: restore the speed cap; acceleration ramps velocity back up.
        zombieNavMeshAgent.speed = baseSpeed;

        // Electric crackle above the head while staggered (Update ends it on recovery).
        ShowStaggerVfx();
    }

    private void ShowStaggerVfx()
    {
        if (_staggerVfxPrefab == null)
            _staggerVfxPrefab = Resources.Load<GameObject>("VFX/StaggerSpark");
        if (_staggerVfxPrefab == null) return;

        _staggerActive = true;
        if (_staggerVfxInstance != null) return; // already showing, keep it alive

        _staggerVfxInstance = Instantiate(_staggerVfxPrefab, transform);
        _staggerVfxInstance.transform.localPosition = Vector3.up * _staggerVfxHeight;
        _staggerVfxInstance.transform.localRotation = Quaternion.identity;
    }

    private void HideStaggerVfx()
    {
        _staggerActive = false;
        if (_staggerVfxInstance != null)
        {
            Destroy(_staggerVfxInstance);
            _staggerVfxInstance = null;
        }
    }

    // ── Knockdown ─────────────────────────────────────────────────────────
    private bool CanKnockDown() => !_knockedDown && Time.time >= _knockdownCooldownUntil;

    private void KnockDown()
    {
        _knockedDown = true;
        _pendingGuaranteedCrit = true;   // next hit while down = forced point-blank crit
        _seenDownAnim = false;
        _knockdownStart = Time.time;

        if (_animator != null) _animator.SetTrigger("Fall");
        if (zombieNavMeshAgent != null)
        {
            zombieNavMeshAgent.velocity = Vector3.zero;
            zombieNavMeshAgent.isStopped = true;
        }
        HideStaggerVfx(); // no crackle while floored
    }

    // Called from Update while _knockedDown: hold the zombie frozen and detect when it has stood back up.
    private void TickKnockdown()
    {
        if (zombieNavMeshAgent != null) zombieNavMeshAgent.isStopped = true;

        var st = _animator.GetCurrentAnimatorStateInfo(0);
        if (st.IsName("FallingBack") || st.IsName("GettingUp")) _seenDownAnim = true;

        bool animDone = _seenDownAnim && !st.IsName("FallingBack") && !st.IsName("GettingUp");
        if (animDone || Time.time - _knockdownStart > _knockdownMaxDuration)
            RecoverFromKnockdown();
    }

    private void RecoverFromKnockdown()
    {
        _knockedDown = false;
        _pendingGuaranteedCrit = false;  // bonus expires if it wasn't used while down
        _seenDownAnim = false;
        _knockdownCooldownUntil = Time.time + _knockdownCooldown;
        if (zombieNavMeshAgent != null) zombieNavMeshAgent.isStopped = false;
    }
    public void HitLayerPlus()
    {
        Animator zombieAnimator = GetComponent<Animator>();
        int hitLayer = zombieAnimator.GetLayerIndex("Hit");
        float targetWeight = 1f;
        float weightChangeSpeed = 0.5f; // Adjust this value as needed
        float currentWeight = zombieAnimator.GetLayerWeight(hitLayer);
        float newWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * weightChangeSpeed);
        zombieAnimator.SetLayerWeight(hitLayer, newWeight);
    }
    private void AfterHit(string after)
    {
        _hit = -1;
        Animator zombieAnimator = GetComponent<Animator>();
        int hitLayer = zombieAnimator.GetLayerIndex("Hit");
        zombieAnimator.SetLayerWeight(hitLayer, 0f);
    }
    private void HitLayerMinus()
    {
        Animator zombieAnimator = GetComponent<Animator>();
        int hitLayer = zombieAnimator.GetLayerIndex("Hit");
        float targetWeight = 0f;
        float weightChangeSpeed = 0.5f; // Adjust this value as needed
        float currentWeight = zombieAnimator.GetLayerWeight(hitLayer);
        float newWeight = Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * weightChangeSpeed);
        zombieAnimator.SetLayerWeight(hitLayer, newWeight);
        if (currentWeight < 0.1f)
        {
            _hit = 0;
        }
    }


    public HealthSystemArmour GetHealthSystem()
    {
        Debug.LogError("GetHealthSystem called, null returned.");
        return null;
    }
    public void Die()
    {
        HideStaggerVfx();
        _knockedDown = false;
        _pendingGuaranteedCrit = false;
        DisableMainCharacter();
        //EnableRagdoll();
        KickbackRagdoll(5f);



        //SpawnBloodPoolAtSpine(gameObject);
        //gameObject.tag = "Z_Dead";
        Destroy(healthBarUI.gameObject);
        zombieNavMeshAgent.isStopped = true;
        DataHolder.EnemiesKilled++;
        _animator.SetBool("Die", true);
        float clipLength = GetAnimationClip("Death").length;
        StartCoroutine(WaitForAnimationAndSpawnBloodPool(clipLength));

        //var dissolveComponent = gameObject.GetComponent<EnemyDissolve>();
        //dissolveComponent.TriggerDissolveAndDestroy();
        //GetComponent<EnemyDissolve>().TriggerDissolveAndDestroy();// start dissolve effect and destroy object when done TODO igors LATER

        //Destroy(gameObject, clipLength + 50f); //trebace destroy nakon dissolve
    }
    private void KickbackRagdoll(float forceMagnitude)
    {
        GameObject zombie_head_prefab = Resources.Load<GameObject>("Enemies/male_zombie_II_1_head");
        GameObject zombie_body_prefab = Resources.Load<GameObject>("Enemies/male_zombie_II_1_body");
        GameObject zombie_body = Instantiate(zombie_body_prefab, transform.position, Quaternion.identity);
        GameObject zombie_head = Instantiate(zombie_head_prefab, transform.position, Quaternion.identity);

        GameObject player = GameObject.FindWithTag("Player");
        Vector3 playerPosition = player.transform.position;
        Vector3 awayFromPlayerDirection = (transform.position - playerPosition).normalized;
        awayFromPlayerDirection += Random.insideUnitSphere * 0.1f; // add slight randomness
        awayFromPlayerDirection.Normalize();

        // Find all Rigidbodies
        Rigidbody[] headRigidbodies = zombie_head.GetComponentsInChildren<Rigidbody>();
        Rigidbody[] bodyRigidbodies = zombie_body.GetComponentsInChildren<Rigidbody>();

        // Apply forces
        ApplyExplosionForce(headRigidbodies, awayFromPlayerDirection, 5f, 15f, 15f, 0.5f);
        ApplyExplosionForce(bodyRigidbodies, awayFromPlayerDirection, 5f, 15f, 15f, 0.5f);

        // Add dissolve to spawned ragdoll parts (copy settings from this Enemy if available)
        var srcDissolve = GetComponent<EnemyDissolve>();
        if (srcDissolve != null && srcDissolve.dissolveTemplate != null)
        {
            AddDissolveAndTrigger(zombie_head, srcDissolve, delay: 2f);
            AddDissolveAndTrigger(zombie_body, srcDissolve, delay: 2f);
        }
    }

    private static void AddDissolveAndTrigger(GameObject target, EnemyDissolve src, float delay)
    {
        var ed = target.AddComponent<EnemyDissolve>();
        ed.dissolveTemplate = src.dissolveTemplate;
        ed.delayBefore = delay;
        ed.dissolveSeconds = src.dissolveSeconds;
        ed.propDissolve = src.propDissolve;
        ed.propBaseMap = src.propBaseMap;
        ed.propBaseColor = src.propBaseColor;

        // Ensure dissolve starts from zero
        if (ed.dissolveTemplate.HasProperty(ed.propDissolve))
        {
            ed.dissolveTemplate.SetFloat(ed.propDissolve, 0f);
        }

        ed.TriggerDissolveAndDestroy();
    }

    private void ApplyExplosionForce(Rigidbody[] rigidbodies, Vector3 forceDirection, float forceMin, float forceMax, float torqueMagnitude, float upFactor)
    {
        foreach (var rb in rigidbodies)
        {
            if (rb == null) continue;
            rb.isKinematic = false;
            rb.useGravity = true;

            Vector3 finalDirection = (forceDirection + Vector3.up * upFactor).normalized;
            float forceStrength = Random.Range(forceMin, forceMax);

            rb.AddForce(finalDirection * forceStrength, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * torqueMagnitude, ForceMode.Impulse);
        }
    }

    int add(int a, int b)
    {
        return a + b;
    }



    private IEnumerator WaitForAnimationAndSpawnBloodPool(float clipLength)
    {
        // Wait for the duration of the animation to finish
        yield return new WaitForSeconds(clipLength);

        // Find the spine bone (or similar) in the hierarchy
        Transform spineTransform = FindBoneRecursive(transform, "spine");

        if (spineTransform != null)
        {
            // Spawn the blood pool under the spine bone's position
            SpawnBloodPoolAtSpine(spineTransform.position);
        }
        else
        {
            Debug.LogWarning("Spine bone not found in the hierarchy.");
        }
    }


    void SpawnBloodPoolAtSpine(Vector3 position)
    {
        Vector3 spinePosition = position;

        RaycastHit hit;
        if (Physics.Raycast(spinePosition + Vector3.up * 0.1f, Vector3.down, out hit, 2.0f)) // Adjust ray length as needed
        {
            Vector3 hitPoint = hit.point;
            Vector3 surfaceNormal = hit.normal;

            _puddles = Resources.Load<BloodDecalAsset>("DecalAssets/Puddles");

            BloodDecalManager.Instance.AddDecal(_puddles, new Color32(120, 0, 0, 255), hitPoint, surfaceNormal, Vector3.one);
        }
        else
        {
            Debug.LogWarning("No surface found below the zombie's spine to spawn the blood pool.");
        }
    }

    private Transform FindBoneRecursive(Transform parent, string boneName)
    {
        // Check if this bone's name matches (case-insensitive)
        if (parent.name.ToLower().Contains(boneName.ToLower()))
        {
            return parent;
        }

        // Loop through each child and recursively check for the bone
        foreach (Transform child in parent)
        {
            Transform found = FindBoneRecursive(child, boneName);
            if (found != null)
            {
                return found;
            }
        }

        // Return null if the bone was not found
        return null;
    }



    private AnimationClip GetAnimationClip(string name)
    {
        if (!_animator) return null; // no animator
        foreach (AnimationClip clip in _animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == name)
            {
                return clip;
            }
        }
        return null; // no clip by that name
    }
    public void SpawnCoin(int value)
    {
        GameObject coinPrefab = Resources.Load<GameObject>("Enemies/Coins/CopperCoin");
        if (!coinPrefab)
        {
            Debug.LogWarning("CopperCoin prefab not found at Resources/Enemies/Coins/CopperCoin");
            return;
        }

        // Optional: disable own collider to avoid physics pushing the coin upward on spawn
        var enemyCollider = GetComponent<Collider>();
        if (enemyCollider && enemyCollider.enabled)
        {
            enemyCollider.enabled = false;
        }

        // Raycast to ground to place coin on the surface
        Vector3 spawnPos = transform.position;
        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f;

        // Ignore the Zombies layer to avoid hitting ourselves
        int zombiesLayer = LayerMask.NameToLayer("Zombies");
        int mask = ~(1 << zombiesLayer);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 10f, mask, QueryTriggerInteraction.Ignore))
        {
            // Small lift to prevent z-fighting if coin pivot is at center/bottom
            spawnPos = hit.point + Vector3.up * 0.25f;
        }

        GameObject coin = Instantiate(coinPrefab, spawnPos, Quaternion.identity);
        var coinComp = coin.GetComponent<Coin>();
        if (coinComp != null) coinComp.CoinAmount = value;
    }

    //referenced from the animation
    public void AnimationZombieAttack(string strIn)
    {
        // Keep movement halted during all attack events (including "end").
        zombieNavMeshAgent.isStopped = true;

        if (strIn != "end")
        {
            SpawnSwordTrail();
        }

        if (strIn != "end" && PlayerIsInMeleeRange() && IsFacingPlayer())
        {
            _player.GetComponent<PlayerControllerInput>().HitReceived(_enemyConfig.meleeDamage, strIn);
            SpawnImpactEffect(_player.transform.position);
        }

        if (strIn == "end")
        {
            // Signal the state machine to transition; do NOT resume NavMesh here.
            AttackFinished = true;
            // Movement will resume in WalkToSelected.OnEnter to keep animation/motion in sync.
        }
    }

    private void SpawnSwordTrail()
    {
        GameObject trailPrefab = Resources.Load<GameObject>("FX/Sword_Trail_FIRE");
        if (!trailPrefab) return;

        float height = GetApproxHalfHeight() * 0.6f; // chest-ish height
        const float forwardOffset = 0.5f;            // little in front of zombie
        Vector3 pos = transform.position + transform.up * height + transform.forward * forwardOffset;

        // Match zombie rotation exactly, then apply FX authoring adjustment
        Quaternion rot = transform.rotation * rotationModification;

        // Parent to enemy so it follows the swing
        Instantiate(trailPrefab, pos, rot, transform);
    }


    // Approximate half-height of the zombie from collider or renderers
    private float GetApproxHalfHeight()
    {
        var capsule = GetComponent<CapsuleCollider>();
        if (capsule) return Mathf.Max(0.5f, capsule.height * 0.5f);

        if (TryGetCombinedRendererBounds(out Bounds b))
            return Mathf.Max(0.5f, b.size.y * 0.5f);

        return 1.0f; // fallback
    }

    private bool TryGetCombinedRendererBounds(out Bounds combined)
    {
        combined = new Bounds();
        var renderers = GetComponentsInChildren<Renderer>();
        bool hasAny = false;
        foreach (var r in renderers)
        {
            if (!r.enabled) continue;
            if (!hasAny)
            {
                combined = r.bounds;
                hasAny = true;
            }
            else
            {
                combined.Encapsulate(r.bounds);
            }
        }
        return hasAny;
    }

    void SetRagdollState(bool state)
    {
        foreach (var rb in _ragdollBodies)
        {
            rb.isKinematic = !state;
        }

        foreach (var col in _ragdollColliders)
        {
            col.enabled = state;
        }
    }
    private void CacheRagdollParts()
    {
        _ragdollBodies = GetComponentsInChildren<Rigidbody>();
        _ragdollColliders = GetComponentsInChildren<Collider>();
    }

    private void DisableRagdoll()
    {
        foreach (var rb in _ragdollBodies)
            rb.isKinematic = true;

        foreach (var col in _ragdollColliders)
        {
            if (col.gameObject != gameObject)
                col.enabled = false;
        }
    }

    private void EnableRagdoll()
    {
        // 1) Turn off the main root Rigidbody entirely � if you just make it kinematic,
        //    it can still �hold� all children in place.
        var mainRb = GetComponent<Rigidbody>();
        if (mainRb != null) Destroy(mainRb);

        // 2) Activate each bone�s physics
        foreach (var rb in _ragdollBodies)
        {
            rb.isKinematic = false;   // allow physics sim
            rb.useGravity = true;    // <<< this is crucial
            rb.mass = Mathf.Max(0.5f, rb.mass);  // give it a reasonable weight
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }

        // 3) Enable their colliders
        foreach (var col in _ragdollColliders)
        {
            // skip the root object�s collider if it�s in your list
            if (col.gameObject != gameObject)
            {
                col.enabled = true;
                col.isTrigger = false;
            }
        }
    }

    private void DisableMainCharacter()
    {
        gameObject.SetActive(false);

        //// Stop animation immediately
        //if (_animator != null)
        //    _animator.enabled = false;

        //// Disable the navmesh agent / movement etc.
        //if (zombieNavMeshAgent != null)
        //    zombieNavMeshAgent.enabled = false;

        //// Turn off the main capsule collider
        //var mainCol = GetComponent<Collider>();
        //if (mainCol != null)
        //    mainCol.enabled = false;

        //// You no longer need this Rigidbody � removing it avoids
        //// the �floating� effect of a kinematic parent
        //var mainRb = GetComponent<Rigidbody>();
        //if (mainRb != null)
        //    Destroy(mainRb);
    }

    public static void CreateDamageNumber()
    {
        // Load once and reuse
        if (_damageNumberPrefab == null)
        {
            _damageNumberPrefab = Resources.Load<DamageNumber>("DamageNumbers/DamageNumbers_1");
            //_damageNumberPrefab = Instantiate(_damageNumberPrefab, transform.position, Quaternion.identity);
            if (_damageNumberPrefab == null)
            {
                Debug.LogError("DamageNumber prefab not found in Prefabs!");
                return;
            }
        }
        if (_damageNumberPrefab_crit == null)
        {
            _damageNumberPrefab_crit = Resources.Load<DamageNumber>("DamageNumbers/DamageNumbers_3");
            if (_damageNumberPrefab_crit == null)
            {
                Debug.LogError("DamageNumber Crit prefab not found in Prefabs!");
                return;
            }
        }
    }

    private bool PlayerIsInMeleeRange()
    {

        float distance = Vector3.Distance(transform.position, _player.transform.position);
        //Debug.Log("Melee range: " + _enemyConfig.meleeRadius + "Current range: " + distance);
        return Vector3.Distance(transform.position, _player.transform.position) <= _enemyConfig.meleeRadius;
    }

    // Wide enough that an in-range hit lands cleanly (no fake "sure hit" whiffs); a player who
    // gets fully behind the zombie (>90°) still dodges. Mid-swing tracking keeps this small anyway.
    private const float FacingHitAngle = 90f;
    private bool IsFacingPlayer()
    {
        Vector3 directionToPlayer = (_player.transform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        return angle < FacingHitAngle;
    }

    private void SpawnImpactEffect(Vector3 position)
    {
        // You can customize this to use your own impact effect prefab
        GameObject impactEffectPrefab = Resources.Load<GameObject>("FX/ImpactEffect");
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("ImpactEffect prefab not found at Resources/FX/ImpactEffect");
        }
    }

}
