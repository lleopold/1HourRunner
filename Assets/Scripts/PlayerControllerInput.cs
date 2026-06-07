using Assets.Scripts.Game;
using CodeMonkey.HealthSystemCM;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ZombieGame
{
    /// <summary>
    /// Orchestrates player input, movement, health/stamina, and dash.
    /// Delegates aiming visuals to <see cref="PlayerAimVisuals"/>,
    /// shooting/recoil to <see cref="PlayerWeaponController"/>,
    /// and rotation/targeting to <see cref="PlayerTargetingController"/>.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerAimVisuals))]
    [RequireComponent(typeof(PlayerWeaponController))]
    [RequireComponent(typeof(PlayerTargetingController))]
    public class PlayerControllerInput : MonoBehaviour, IGetHealthSystem
    {
        public enum PowerupType { Health, Reload, Running, ClipSize }
        public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }
        public struct PowerupOffer
        {
            public PowerupType type;
            public Rarity rarity;
            public string title;
            public string description;
            public string iconPath;
        }

        // ── Movement ──────────────────────────────────────────────────────────
        private CharacterController _characterController;
        private Vector3 _direction;
        private float _velocityX;
        private float _velocityZ;
        private readonly float _gravity = -9.81f;
        private float _velocity;
        private int _numberOfJumps;
        private readonly int _maxNumberOfJumps = 1;
        private Movement _movement;

        private bool _isDecelerating;
        private float _decelRate;
        private bool _lastWasRunning;
        private float _rawInputMagnitude;
        private const float WALK_STOP_TIME = 0.2f;
        private const float RUN_STOP_TIME = 0.4f;
        private bool _wasMoving;

        // ── Dash ──────────────────────────────────────────────────────────────
        private readonly float _dashDistance = 4f;
        private readonly float _dashDuration = 0.2f;
        private readonly float _dashCooldown = 1f;
        private bool _isDashing;
        private float _lastDashTime = -Mathf.Infinity;
        private Material _ghostMaterial;

        // ── Post-processing ───────────────────────────────────────────────────
        private MotionBlur _motionBlur;

        // ── Health / stamina ──────────────────────────────────────────────────
        private HealthSystem _healthSystem;
        private float _currentHealth;
        private float _currentStamina;
        private bool _waitingToRestoreStamina;

        // ── UI refs ───────────────────────────────────────────────────────────
        private UIT_GameScreen _uiT_GameScreen;
        private UIT_EndGamePopUp _uiT_EndGamePopUp;
        private UIT_LevelUp _uiT_LevelUp;

        // ── Input ─────────────────────────────────────────────────────────────
        private PlayerInputIS _playerInput;
        private Vector2 _input;
        private bool _isGamePaused;

        // ── Component references ──────────────────────────────────────────────
        private Animator _animator;
        private PlayerAimVisuals _aimVisuals;
        private PlayerWeaponController _weaponController;
        private PlayerTargetingController _targeting;
        private GameStats _gameStats;

        private float MovementPenaltyBase
            => PlayerConfigSingleton.Instance.PlayerConfig.movementAimingPenalty;

        // ── Properties ────────────────────────────────────────────────────────

        public float Health
        {
            get => _currentHealth;
            set
            {
                _currentHealth = value;
                if (_currentHealth <= 0)
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
            get => Mathf.Max(_currentStamina, 0f);
            set
            {
                _currentStamina = Mathf.Max(value, 0f);
                if (_currentStamina <= 0) Debug.Log("Zero stamina");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _animator = GetComponent<Animator>();
            _aimVisuals = GetComponent<PlayerAimVisuals>();
            _weaponController = GetComponent<PlayerWeaponController>();
            _targeting = GetComponent<PlayerTargetingController>();

            _playerInput = PliTuls.InitializeInput(
                Move, Jump, Sprint, OnAim, OnAimControllerRightStick, OnAimControllerTrigger, OnShoot);

            _currentHealth = PlayerConfigSingleton.Instance.PlayerConfig.health;
            _healthSystem = new HealthSystem(PlayerConfigSingleton.Instance.PlayerConfig.health);
            Stamina = PlayerConfigSingleton.Instance.PlayerConfig.stamina;

            _uiT_GameScreen = FindFirstObjectByType<UIT_GameScreen>();
            _uiT_GameScreen.InitializeWeaponUI(WeaponConfigSingleton.Instance.WeaponConfig.ClipSize);
            _uiT_GameScreen.SetHealth(100);
            _uiT_GameScreen.SetStamina(100);

            _uiT_EndGamePopUp = FindObjectOfType<UIT_EndGamePopUp>();
            _uiT_EndGamePopUp._root.visible = false;
            _uiT_EndGamePopUp.enabled = false;

            _gameStats = new GameStats
            {
                _precisionMin = PlayerConfigSingleton.Instance.PlayerConfig.PrecisionAngleMin,
                _precisionMax = PlayerConfigSingleton.Instance.PlayerConfig.PrecisionAngleMax,
                _precisionStartingAim = PlayerConfigSingleton.Instance.PlayerConfig.PrecisionAimingAngleStarting,
                _aimingSpeed = PlayerConfigSingleton.Instance.PlayerConfig.AimingSpeed
            };

            _aimVisuals.Initialize(_gameStats);
            _weaponController.Initialize(_uiT_GameScreen, _aimVisuals, _gameStats,
                                         _aimVisuals.AimingCircleTrigger, _targeting);
            _targeting.Initialize();
        }

        private void Start()
        {
            gameObject.tag = "Player";

            SkinnedMeshRenderer skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinnedMesh == null || skinnedMesh.sharedMesh == null)
            {
                Debug.LogError("No SkinnedMeshRenderer found on Player!");
                return;
            }

            _ghostMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _ghostMaterial.SetColor("_BaseColor", new Color(1, 1, 1, 0.3f));
            _ghostMaterial.SetFloat("_Surface", 1);
            _ghostMaterial.SetFloat("_Blend", 2);
            _ghostMaterial.SetFloat("_Cull", 0);

            Volume postProcessVolume = GameObject.Find("PostProcessing")?.GetComponent<Volume>();
            if (postProcessVolume != null)
                postProcessVolume.profile.TryGet(out _motionBlur);
            else
                Debug.LogError("PostProcessing Volume not found!");

            if (_movement.acceleration <= 0f) _movement.acceleration = 80f;

            PlayerWeaponController.CreateDamageNumber();
        }

        private void OnDestroy()
        {
            PliTuls.UnregisterInputEvents(_playerInput,
                Move, Jump, Sprint, OnAim, OnAimControllerRightStick, OnAimControllerTrigger, OnShoot);
        }

        private void Update()
        {
            if (_animator != null) _animator = GetComponent<Animator>();

            _targeting.Tick();

            Quaternion newRotation = _targeting.ComputeAndApplyRotation(_input, _direction);
            transform.rotation = newRotation;

            ApplyGravity();
            ApplyMovement();
            UpdateAimAnimator();
            _weaponController.Tick(_targeting.IsAiming);
            RestoreStamina();

            _aimVisuals.UpdateAimingCircle(_targeting.IsAiming, transform.rotation);
            _aimVisuals.UpdateVisuals(_targeting.IsAiming, _input);

            if (!_targeting.IsAiming) _aimVisuals.ClearAimingCircleOutlines();

            if (_uiT_EndGamePopUp._root.visible && !_isGamePaused) PauseGame(true);
        }

        void OnGUI()
        {
            int y = _targeting.DrawDebugGUI();
            y += 10;

            // ── Shot chance breakdown ─────────────────────────────────────────
            var wCfg = WeaponConfigSingleton.Instance?.WeaponConfig;
            var pCfg = PlayerConfigSingleton.Instance?.PlayerConfig;
            if (wCfg == null || pCfg == null) return;

            float angle = _aimVisuals != null ? _aimVisuals.CurrentAngle : 30f;
            float weaponAcc = wCfg.Accuracy / 100f;
            float playerAcc = pCfg.Accuracy / 100f;
            float aimMult = AimPrecisionColors.GetHitMultiplier(angle);

            // Find closest zombie inside the gizmo
            var trigger = _aimVisuals?.AimingCircleTrigger;
            var zombiesInGizmo = trigger?.GetZombiesInside();

            float gizmoClosestDist = 0f;
            string gizmoClosestName = "none";
            if (zombiesInGizmo != null && zombiesInGizmo.Count > 0)
            {
                float bestDist = float.MaxValue;
                foreach (var c in zombiesInGizmo)
                {
                    if (c == null) continue;
                    float d = Vector3.Distance(transform.position, c.transform.position);
                    if (d < bestDist) { bestDist = d; gizmoClosestName = c.gameObject.name; }
                }
                gizmoClosestDist = bestDist;
            }

            // Use gizmo-closest distance for multiplier when available, else locked target
            float distance;
            if (gizmoClosestDist > 0f)
                distance = gizmoClosestDist;
            else
            {
                GameObject tgt = _targeting.CurrentTarget;
                distance = tgt != null ? Vector3.Distance(transform.position, tgt.transform.position) : 0f;
            }

            float distMult = (distance > 0f && wCfg.MaxEffectiveRange > 0f)
                ? AimPrecisionColors.GetDistanceMultiplier(distance, wCfg.OptimalRange, wCfg.MaxEffectiveRange)
                : 1f;

            float hitChance = weaponAcc * playerAcc * aimMult * distMult;

            GUI.Label(new Rect(10, y, 400, 20), "── Shot Chance ──────────────────"); y += 20;
            GUI.Label(new Rect(10, y, 400, 20), $"Aim angle:        {angle:F1}°"); y += 20;
            GUI.Label(new Rect(10, y, 400, 20), $"Weapon accuracy:  {weaponAcc:F2}  ({wCfg.Accuracy:F0}%)"); y += 20;
            GUI.Label(new Rect(10, y, 400, 20), $"Player accuracy:  {playerAcc:F2}  ({pCfg.Accuracy:F0}%)"); y += 20;
            GUI.Label(new Rect(10, y, 400, 20), $"Aim multiplier:   {aimMult:F2}"); y += 20;
            GUI.Label(new Rect(10, y, 500, 20), $"Gizmo zombies:    {(zombiesInGizmo != null ? zombiesInGizmo.Count : 0)}  closest: {gizmoClosestName}  ({gizmoClosestDist:F1} u)"); y += 20;
            if (distance > 0f)
            {
                GUI.Label(new Rect(10, y, 500, 20), $"Distance:         {distance:F1} u  (opt {wCfg.OptimalRange:F0} / max {wCfg.MaxEffectiveRange:F0})"); y += 20;
                GUI.Label(new Rect(10, y, 400, 20), $"Distance mult:    {distMult:F2}"); y += 20;
            }
            GUI.Label(new Rect(10, y, 400, 20), $"TOTAL hit chance: {hitChance * 100f:F1}%"); y += 20;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Input callbacks
        // ─────────────────────────────────────────────────────────────────────

        public void Move(InputAction.CallbackContext context)
        {
            _input = context.ReadValue<Vector2>();
            _rawInputMagnitude = Mathf.Clamp01(_input.magnitude);
            _direction = new Vector3(_input.x, 0, _input.y);
            if (_direction.sqrMagnitude > 1f) _direction.Normalize();
            _velocityZ = Vector3.Dot(_direction.normalized, transform.forward);
            _velocityX = Vector3.Dot(_direction.normalized, transform.right);
        }

        public void Jump(InputAction.CallbackContext context)
        {
            if (!context.started) return;
            if (!IsGrounded() && _numberOfJumps >= _maxNumberOfJumps) return;
            if (Stamina <= 0 || Stamina <= 30) return;
            if (_numberOfJumps == 0) StartCoroutine(WaitForLanding());
            _numberOfJumps++;
            _animator.SetTrigger("RollTrigger");
            Stamina -= 30;
            _uiT_GameScreen.SetStamina(Stamina);
            Debug.Log("Jumping");
            if (Time.time >= _lastDashTime + _dashCooldown && !_isDashing && _input.magnitude > 0)
                StartCoroutine(DashCoroutine());
        }

        public void Sprint(InputAction.CallbackContext context)
        {
            _movement.isSprinting = context.started || context.performed;
            if (context.canceled) _movement.isSprinting = false;
        }

        public void OnAim(InputAction.CallbackContext context)
        {
            _targeting.OnAimMouse(context);
        }

        public void OnAimControllerRightStick(InputAction.CallbackContext context)
        {
            _targeting.OnAimControllerRightStick(context);
        }

        public void OnAimControllerTrigger(InputAction.CallbackContext context)
        {
            _targeting.OnAimControllerTrigger(context.performed, context.canceled);
        }

        public void OnShoot(InputAction.CallbackContext context)
        {
            if (context.started && !context.canceled && _targeting.IsAiming)
                _weaponController.IsShooting = true;
            if (context.canceled)
                _weaponController.IsShooting = false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Movement
        // ─────────────────────────────────────────────────────────────────────

        private void ApplyMovement()
        {
            bool hasInput = _rawInputMagnitude > 0.01f;
            if (hasInput && _isDecelerating) _isDecelerating = false;

            if (!_isDashing)
            {
                int sprinting = _movement.isSprinting ? 1 : 0;
                float targetSpeed = hasInput ? GetMoveSpeed(_rawInputMagnitude, sprinting) : 0f;

                if (hasInput)
                {
                    _lastWasRunning = _movement.isSprinting;
                    UpdateCurrentSpeed(targetSpeed);
                }
                else
                {
                    if (!_isDecelerating && _movement.currentSpeed > 0.01f)
                    {
                        float stopTime = Mathf.Max(0.01f, _lastWasRunning ? RUN_STOP_TIME : WALK_STOP_TIME);
                        _decelRate = _movement.currentSpeed / stopTime;
                        _isDecelerating = true;
                    }
                    if (_isDecelerating)
                    {
                        _movement.currentSpeed = Mathf.Max(0f, _movement.currentSpeed - _decelRate * Time.deltaTime);
                        if (_movement.currentSpeed <= 0.0001f)
                        {
                            _movement.currentSpeed = 0f;
                            _isDecelerating = false;
                        }
                    }
                }

                _characterController.Move(_direction * (_movement.currentSpeed * Time.deltaTime));
            }

            _animator.SetFloat("MoveZ", _velocityZ, 0.2f, Time.deltaTime);
            _animator.SetFloat("MoveX", _velocityX, 0.2f, Time.deltaTime);

            float walkBase = PlayerConfigSingleton.Instance.PlayerConfig.speed;
            float runMultiplier = (PlayerConfigSingleton.Instance.PlayerConfig.RunningSpeed_pct / 100f) + 1f;
            float runMax = walkBase * runMultiplier;
            float denom = _movement.isSprinting ? runMax : walkBase;
            float speedRatio = denom > 0f ? Mathf.Clamp01(_movement.currentSpeed / denom) : 0f;
            float moveAmount = _movement.isSprinting ? 0.5f + 0.5f * speedRatio : 0.5f * speedRatio;
            if (!_movement.isSprinting && _rawInputMagnitude > 0f && moveAmount < 0.05f) moveAmount = 0.05f;
            _animator.SetFloat("moveAmount", moveAmount, 0.15f, Time.deltaTime);

            bool isMoving = _rawInputMagnitude > 0;
            bool isRunning = _movement.isSprinting;
            _aimVisuals.ApplyMovementPenalty(isMoving, _wasMoving, isRunning, MovementPenaltyBase);
            _wasMoving = isMoving;
        }

        private void ApplyGravity()
        {
            if (IsGrounded() && _velocity < 0)
                _velocity = -1f;
            else
            {
                _velocity += _gravity * Time.deltaTime;
                _direction.y = _velocity;
            }
        }

        private bool IsGrounded() => _characterController.isGrounded;

        private float GetMoveSpeed(float inputMagnitude, int runningLocal)
        {
            if (inputMagnitude <= 0.001f) return 0f;
            float returnSpeed = PlayerConfigSingleton.Instance.PlayerConfig.speed;
            float tempWalking = runningLocal == 1 ? 0 : 1;
            float fwdBck = Vector3.Dot(transform.forward, _direction);
            if (Math.Sign(fwdBck) == 1)
            {
                float walk = ((PlayerConfigSingleton.Instance.PlayerConfig.speed / 100) + 1) * tempWalking;
                float run = ((PlayerConfigSingleton.Instance.PlayerConfig.RunningSpeed_pct / 100) + 1) * runningLocal;
                returnSpeed *= (walk + run);
            }
            else if (Math.Sign(fwdBck) == -1)
            {
                float walk = ((PlayerConfigSingleton.Instance.PlayerConfig.speed / 100) + 1) * tempWalking;
                float run = ((PlayerConfigSingleton.Instance.PlayerConfig.BackMovementPenalty_pct
                              * PlayerConfigSingleton.Instance.PlayerConfig.speed / 100) + 1) * runningLocal;
                returnSpeed *= (walk + run);
            }
            _movement.acceleration = PlayerConfigSingleton.Instance.PlayerConfig.acceleration;
            return returnSpeed;
        }

        private void UpdateCurrentSpeed(float targetSpeed)
        {
            float accelPct = Mathf.Clamp(_movement.acceleration, 0f, 100f);
            if (accelPct >= 100f) { _movement.currentSpeed = targetSpeed; return; }
            if (accelPct <= 0f) { if (targetSpeed == 0f) _movement.currentSpeed = 0f; return; }
            float factor = accelPct / 100f;
            _movement.currentSpeed += (targetSpeed - _movement.currentSpeed) * factor;
            if (Mathf.Abs(targetSpeed - _movement.currentSpeed) < 0.01f) _movement.currentSpeed = targetSpeed;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Animator / aim
        // ─────────────────────────────────────────────────────────────────────

        private void UpdateAimAnimator()
        {
            if (_targeting.IsAiming)
            {
                _animator.SetFloat("AimH", DataHolder.weaponType == WeaponType.H1 ? 1 : 0);
                _animator.SetFloat("AimHH", DataHolder.weaponType == WeaponType.H2 ? 1 : 0);
            }
            else
            {
                _animator.SetFloat("AimH", 0);
                _animator.SetFloat("AimHH", 0);
                _aimVisuals.DisableMeshRenderer();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Health / stamina
        // ─────────────────────────────────────────────────────────────────────

        public void RestoreStamina()
        {
            if (_waitingToRestoreStamina) return;
            if (Stamina == 0)
            {
                _waitingToRestoreStamina = true;
                Invoke(nameof(StopWaitingForRestoreStamina), 1f);
            }
            if (!_movement.isSprinting && _currentStamina < PlayerConfigSingleton.Instance.PlayerConfig.stamina)
            {
                Stamina += PlayerConfigSingleton.Instance.PlayerConfig.staminaRegenSpeed * Time.deltaTime;
                float pct = (_currentStamina / PlayerConfigSingleton.Instance.PlayerConfig.stamina) * 100;
                _uiT_GameScreen.SetStamina(pct);
            }
        }

        private void StopWaitingForRestoreStamina() => _waitingToRestoreStamina = false;

        public void HitReceived(float damage, string bodyPart)
        {
            Health -= damage;
            _healthSystem.Damage(damage);
            float healthPct = _healthSystem.GetHealth() / _healthSystem.GetHealthMax() * 100;
            _uiT_GameScreen.SetHealth(healthPct);
            Debug.Log($"Player hit in {bodyPart} for {damage} damage. Health: {Health}");
            _animator.Play("HeadHit");
            SoundFXManager.Instance.PlaySoundFXClip(PlayerConfigSingleton.Instance.PlayerConfig.hitReceived, transform, 1f);
            _weaponController.SpawnDamageNumber(transform.position + Vector3.up * 1.5f, damage);
        }

        public HealthSystem GetHealthSystem() => _healthSystem;

        // ─────────────────────────────────────────────────────────────────────
        // Game flow
        // ─────────────────────────────────────────────────────────────────────

        public void PauseGame(bool pause)
        {
            Time.timeScale = pause ? 0f : 1f;
            _isGamePaused = pause;
        }

        internal void AddCoins(int coinValue)
        {
            if (_uiT_GameScreen.AddXP(coinValue))
            {
                GameObject levelUpPrefab = Resources.Load<GameObject>("LevelUp/UILevelUp");
                Instantiate(levelUpPrefab, transform.position + new Vector3(0f, 1f, 0f), Quaternion.identity);
                _uiT_LevelUp = FindFirstObjectByType<UIT_LevelUp>();
                PauseGame(true);
                _uiT_LevelUp.SetOffers(_uiT_LevelUp.GenerateDefaultOffers());
            }
            Debug.Log("Player received " + coinValue + " coins");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Dash & ghosts
        // ─────────────────────────────────────────────────────────────────────

        private IEnumerator DashCoroutine()
        {
            _isDashing = true;
            _lastDashTime = Time.time;
            Vector3 dashDirection = _direction.normalized;
            float dashSpeed = _dashDistance / _dashDuration;
            StartCoroutine(SpawnGhosts());
            float elapsed = 0f;
            while (elapsed < _dashDuration)
            {
                _characterController.Move(dashDirection * (dashSpeed * Time.deltaTime));
                elapsed += Time.deltaTime;
                yield return null;
            }
            _isDashing = false;
        }

        private IEnumerator SpawnGhosts()
        {
            if (_motionBlur != null) { _motionBlur.active = true; _motionBlur.intensity.Override(0.8f); }
            int totalGhosts = 8;
            for (int i = 0; i < totalGhosts; i++)
            {
                CreateGhost(i, totalGhosts);
                yield return new WaitForSeconds(0.02f);
            }
            StartCoroutine(FadeOutMotionBlur(0.3f));
        }

        private void CreateGhost(int ghostIndex, int totalGhosts)
        {
            SkinnedMeshRenderer[] skinnedMeshes = GetComponentsInChildren<SkinnedMeshRenderer>();
            if (skinnedMeshes.Length == 0) return;
            Material mat = Resources.Load<Material>("Material/GhostMaterial");
            if (mat == null) { Debug.LogError("GhostMaterial not found in Resources/Material!"); return; }
            float scaleFactor = Mathf.Lerp(0.6f, 1.0f, (float)ghostIndex / (totalGhosts - 1));
            float alpha = Mathf.Lerp(0.3f, 0.8f, (float)ghostIndex / (totalGhosts - 1));
            GameObject ghostParent = new GameObject("Ghost");
            ghostParent.transform.SetPositionAndRotation(transform.position, transform.rotation);
            ghostParent.transform.localScale = Vector3.one * scaleFactor;
            foreach (SkinnedMeshRenderer sm in skinnedMeshes)
            {
                Mesh bakedMesh = new Mesh();
                sm.BakeMesh(bakedMesh);
                GameObject ghost = new GameObject("GhostMesh");
                ghost.transform.SetParent(ghostParent.transform);
                ghost.transform.position = sm.transform.position;
                ghost.transform.rotation = sm.transform.rotation;
                ghost.transform.localScale = Vector3.one * scaleFactor;
                ghost.AddComponent<MeshFilter>().mesh = bakedMesh;
                ghost.AddComponent<MeshRenderer>().material =
                    new Material(mat) { color = new Color(0.2f, 0.2f, 0.2f, alpha) };
            }
            Destroy(ghostParent, 0.3f);
        }

        private IEnumerator FadeOutMotionBlur(float duration)
        {
            if (_motionBlur == null) yield break;
            float startIntensity = _motionBlur.intensity.value;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _motionBlur.intensity.Override(Mathf.Lerp(startIntensity, 0f, elapsed / duration));
                yield return null;
            }
            _motionBlur.intensity.Override(0f);
        }

        private IEnumerator WaitForLanding()
        {
            yield return new WaitUntil(() => !IsGrounded());
            yield return new WaitUntil(IsGrounded);
            _numberOfJumps = 0;
        }
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
