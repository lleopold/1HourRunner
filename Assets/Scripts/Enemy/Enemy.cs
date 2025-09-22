using Assets.Scripts.HealthSystem;
using DamageNumbersPro;
using LUZEMRIK.BloodDecals;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
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


    private void Awake()
    {
        Debug.Log("Enemy Awake");

        CacheRagdollParts();
        DisableRagdoll();

        Target = _player;
        zombieNavMeshAgent = GetComponent<NavMeshAgent>();
        zombieNavMeshAgent.updateRotation = true;
        _animator = GetComponent<Animator>();
        int zombieLayer = LayerMask.NameToLayer("Zombies");
        Debug.Log("Zombie Layer: " + zombieLayer);


        AttackFinished = false;
        _monoBehaviour = this;
        _stateMachine = new StateMachine();

        Transform healthBarCanvasTransform = transform.Find("HealthBarCanvas_2");
        GameObject healthBarCanvas = healthBarCanvasTransform.gameObject;
        healthBarUI = healthBarCanvas.transform.Find("HealthBarUI");
        healthBarScroll = healthBarUI.GetComponent<HealthBarScroll>();

        var cg = healthBarUI.GetComponent<CanvasGroup>();
        if (cg) cg.alpha = 1f;

        _healthBarVisible = false;

        string prefabName = gameObject.name;
        _player = GameObject.Find("Player");
        PlayerConfigManager playerConfigManager = Resources.Load<PlayerConfigManager>("Config/Player/PlayerConfigManager"); // Load the config manager
        playerConfig = playerConfigManager.GetConfig(DataHolder.ChosenPlayer.ToString());

        EnemyConfigManager configManager = Resources.Load<EnemyConfigManager>("Config/Enemy/EnemyConfigManager"); // Load the config manager
        _enemyConfig = configManager.GetConfig(prefabName.Replace("(Clone)", ""));//MRS

        _health = _enemyConfig.health;
        _amountOfRuns = _enemyConfig.maxAmountOfRuns; //how many times the zombie can run

        _gathering = GameObject.Find("GatheringPoint");

        var searchForVictim = new SearchForVictim(this, _player);
        var walkToSelected = new WalkToSelected(this, zombieNavMeshAgent, _animator, _enemyConfig, _player.transform);
        var attackFreely = new AttackFreely(this, _player, _animator, _enemyConfig, _monoBehaviour);
        var stop = new Stop(this, zombieNavMeshAgent);
        var fullStop = new FullStop(this, zombieNavMeshAgent);
        var searchForGatheringSpot = new SearchForGathering(this, _gathering);
        var walkToGathering = new WalkToGathering(this, zombieNavMeshAgent, _animator, _enemyConfig, _gathering.transform);
        var roam = new Roam(this, _player.transform, zombieNavMeshAgent, _animator, _enemyConfig, _monoBehaviour);

        //_stateMachine.AddTransition(searchForVictim, walkToSelected, HasTarget);
        //At(searchForVictim, walkToSelected, FarToPlayer);
        //At(searchForGatheringSpot, walkToGathering, FarToGathering);
        //At(walkToGathering, stop, CloseToGather);    //TODO
        At(stop, searchForVictim, HasTarget);
        At(searchForVictim, walkToSelected, FarToPlayer);
        //At(searchForVictim, walkToSelected, HasTarget);

        //At(walkToSelected, fullStop, CloseToPlayer);

        //At(walkToSelected, stop, CloseToPlayer);
        //At(attackFreely, stop, FinishedAttack);
        //At(roam, searchForVictim, StopRoaming);



        At(walkToSelected, attackFreely, CloseToPlayer);
        At(attackFreely, walkToSelected, FinishedAttack);

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
        _outline.OutlineWidth = 5f;
        _outline.enabled = false;


        if (BloodDecalManager.Instance == null)
        {
            GameObject bloodDecalManagerGO = new GameObject("BloodDecalManager");
            bloodDecalManagerGO.AddComponent<BloodDecalManager>();
        }
        CreateDamageNumber();
    }

    public void SetOutline(bool newSetting)
    {
        _outline.enabled = newSetting;
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
        var retval = Vector3.Distance(transform.position, _player.transform.position) > _enemyConfig.meleeRadius;
        if (retval)
        {
            Debug.Log("FarToPlayer:" + retval);
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
        if (_player == null)
        {
            Debug.LogError("Player reference is not assigned to the ZombieMovement script.");
        }

        // Hide health bar until first damage
        if (healthBarUI != null)
        {
            healthBarUI.gameObject.SetActive(false);
            _healthBarVisible = false;
        }
    }

    void Update()
    {
        _stateMachine.Tick();
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


    public void DamageReceived(float damage, Vector3 hitDirection)
    {
        if (_health <= 0) // already dead or dying
            return;

        // First time damage: apply damage BEFORE showing the UI so it appears already reduced
        bool firstReveal = !_healthBarVisible;

        // Apply damage to internal values
        _health -= damage;
        healthBarScroll.healthSystemArmour.Damage(damage);

        // Clamp (just in case overkill damage was large)
        if (_health < 0) _health = 0;

        if (firstReveal && healthBarUI != null)
        {
            healthBarUI.gameObject.SetActive(true);
            _healthBarVisible = true;
        }

        // Visual feedback / animation after health state is correct
        Hit();

        // Damage number (uses already reduced value contextually)
        Vector3 damagePosition = transform.position + Vector3.up * 1.5f;
        _damageNumberPrefab.Spawn(damagePosition, damage);

        if (_health <= 0 && !_spawnedCoin)
        {
            _spawnedCoin = true;
            SpawnCoin(30);
            var zombieCollider = GetComponent<Collider>();
            if (zombieCollider) zombieCollider.enabled = false;
            Die();
        }
    }

    public void Hit()
    {
        Animator zombieAnimator = GetComponent<Animator>();
        int hitLayer = zombieAnimator.GetLayerIndex("Hit");
        zombieAnimator.SetLayerWeight(hitLayer, 1f);
        zombieAnimator.SetTrigger("HitLayer");
        //zombieAnimator.SetTrigger("HitReceived");
        _hit = 1;
        KnockBack(gameObject, transform.forward);
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
    public void KnockBack(GameObject gameObject, Vector3 hitDirection)
    {
        zombieNavMeshAgent.speed = 0;
        zombieNavMeshAgent.velocity = -hitDirection * 2;
        zombieNavMeshAgent.speed = _enemyConfig.speed;


        //AfterHit();
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

        var hitEffectPrefab = Resources.Load<GameObject>("Weapons/Hit_02");
        if (hitEffectPrefab) Instantiate(hitEffectPrefab, _player.transform.position, Quaternion.identity);

        if (strIn == "end")
        {
            AttackFinished = true;
            zombieNavMeshAgent.isStopped = false; // let chase resume immediately
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
        // 1) Turn off the main root Rigidbody entirely — if you just make it kinematic,
        //    it can still “hold” all children in place.
        var mainRb = GetComponent<Rigidbody>();
        if (mainRb != null) Destroy(mainRb);

        // 2) Activate each bone’s physics
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
            // skip the root object’s collider if it’s in your list
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

        //// You no longer need this Rigidbody — removing it avoids
        //// the “floating” effect of a kinematic parent
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
    }

    private bool PlayerIsInMeleeRange()
    {
        return Vector3.Distance(transform.position, _player.transform.position) <= _enemyConfig.meleeRadius;
    }

    private bool IsFacingPlayer()
    {
        Vector3 directionToPlayer = (_player.transform.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        // You can adjust the angle threshold as needed (e.g., 45 degrees)
        return angle < 45f;
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
