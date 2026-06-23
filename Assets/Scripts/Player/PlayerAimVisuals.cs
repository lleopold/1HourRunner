using System;
using Assets.Scripts.Game;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ZombieGame
{
    /// <summary>
    /// Handles all aiming visuals: laser line renderers, precision V lines,
    /// radar sweep mesh, aiming circle mesh, and laser smoke particles.
    /// </summary>
    public class PlayerAimVisuals : MonoBehaviour
    {
        private float _radarTimeTranslation;
        // ── Sweep / radar ─────────────────────────────────────────────────────
        [SerializeField] private float _sweepDuration = 2f;
        [SerializeField] private float _sweepLineWidthPct = 0.05f;
        [SerializeField] private float _sweepTrailWidthPct = 0.6f;
        [SerializeField] private float _sweepLineAlpha = 3.0f;
        [SerializeField] private float _sweepTrailAlpha = 0.05f;
        [SerializeField] private float _sweepPauseDuration = 1f;
        [SerializeField] private Color _sweepColor = Color.green;
        [SerializeField] private int _resolution = 30;
        [SerializeField] private float _outerRadius = 10f;
        [SerializeField] private float _pointyTipFactor = 0.25f;

        [Header("Radar Visuals")]
        [SerializeField] private float _radarSpeed = 2.5f;
        [SerializeField] private int _radarLineCount = 10;
        [SerializeField] private float _radarLineSharpness = 32f;
        [SerializeField] private float _radarOpacity = 0.6f;
        [SerializeField] private float _radarBoostMin = 0.8f;
        [SerializeField] private float _radarBoostMax = 1.8f;

        // ── Laser ──────────────────────────────────────────────────────────────
        [SerializeField] private float baseWidth = 0f;
        [SerializeField] private float scrollSpeed = 1.6f;
        [SerializeField] private float pulseSpeed = 2.2f;
        [SerializeField] private float pulseMin = 0.85f;
        [SerializeField] private float pulseMax = 1.15f;
        [SerializeField] private Color laserColor = new Color(1f, 0.2f, 0.05f);
        [SerializeField] private float emissionBase = 1.4f;
        [SerializeField] private float emissionPulse = 1.0f;

        [Header("Precision Shot System")]
        [SerializeField] private float _precisionVAngle = 0.1f;
        [SerializeField] private Color _precisionVColor = Color.yellow;
        [SerializeField] private float _precisionVAlpha = 3.0f;
        [SerializeField] private float _precisionZoneToleranceDegrees = 3f;

        // ── Aim line pixel width ───────────────────────────────────────────────
        [SerializeField] private int aimLinePixels = 0;
        [SerializeField] private float minWorldWidth = 0.02f;
        [SerializeField] private float maxWorldWidth = 0.25f;

        // ── Smoke ──────────────────────────────────────────────────────────────
        [SerializeField] private string laserSmokePrefabPath = "VFX/LaserSmoke";
        [SerializeField] private float _defaultVLength = 10f;

        [Header("Plasma Shot V-Arms (replaces laser lines)")]
        [SerializeField] private bool _usePlasmaShot = true;
        [SerializeField] private GameObject _plasmaShotPrefab;
        [Tooltip("Travel speed of each shot (units/sec). Lifetime is auto-set so the shot dies at the arm tip.")]
        [SerializeField] private float _plasmaShotSpeed = 25f;
        [Tooltip("Shots emitted per world unit of arm length. Higher = denser, more line-like.")]
        [SerializeField] private float _plasmaShotsPerUnit = 30f;
        [Tooltip("OFF (default) = auto color from weapon ranges: white(point-blank)→green(optimal)→orange→red(max). ON = use the manual gradient below.")]
        [SerializeField] private bool _useManualPlasmaColor = false;
        [Tooltip("Only used when 'Use Manual Plasma Color' is ON. Left = at player, right = at the tip.")]
        [GradientUsage(true)]
        [SerializeField] private Gradient _plasmaColorOverDistance;
        private GameObject _plasmaLeft;
        private GameObject _plasmaRight;
        private ParticleSystem _plasmaRootLeft;
        private ParticleSystem _plasmaRootRight;
        public float CurrentHitChance { get; set; } = 1f;
        public float CurrentDistanceMultiplier { get; set; } = 1f;
        public bool IsPointBlank { get; set; }
        public float CurrentTargetDistance { get; set; } = -1f;

        // ── Internal references ────────────────────────────────────────────────
        public LineRenderer LineRendererLeft { get; private set; }
        public LineRenderer LineRendererRight { get; private set; }
        private LineRenderer _lineRendererPrecisionLeft;
        private LineRenderer _lineRendererPrecisionRight;

        private Material _laserMat;
        private Texture2D _laserTex;
        private Material _precisionLaserMat;

        private GameObject _gameObjectAimingCircle;
        private Mesh _meshAimingCircle;
        private MeshFilter _meshFilterAimingCircle;
        private MeshRenderer _meshRendererAimingCircle;
        private MeshCollider _meshColliderAimingCircle;
        private AimingCircleTrigger _aimingCircleTrigger;

        /// <summary>The trigger component on the AnnularSector — available after Initialize().</summary>
        public AimingCircleTrigger AimingCircleTrigger => _aimingCircleTrigger;
        private readonly float _aimingCircleHeight = 0.11f;

        private Texture2D _radarTex;
        private Material _instancedRadarMat;
        private float _currentRadarOffset;

        private ParticleSystem _laserSmokeLeft;
        private ParticleSystem _laserSmokeRight;

        private float _lastSweepEndTime = -999f;
        private bool _isPausedAtEdge;
        private bool _pauseOnRightEdge;

        private Camera _mainCamera;
        private CharacterController _characterController;

        // ── State (set by PlayerControllerInput) ──────────────────────────────
        public float CurrentAngle { get; set; }
        public float Recoil { get; set; }
        public bool IsPrecisionShot { get; private set; }

        // ── GameStats reference ────────────────────────────────────────────────
        private GameStats _gameStats;

        // ─────────────────────────────────────────────────────────────────────
        internal void Initialize(GameStats gameStats)
        {
            _gameStats = gameStats;
            _mainCamera = Camera.main;
            _characterController = GetComponent<CharacterController>();

            CreateLineRenderers();
            CreateAimingCircle();
            InitLaserMaterials();
            InitLaserSmoke();
            InitPlasmaShots();

            CurrentAngle = gameStats._precisionMax;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        public void UpdateVisuals(bool isAiming, Vector2 movementInput)
        {
            if (isAiming)
            {
                SetAimLinesActive(true);

                // Radar line sharpness driven by aim: 1 when not aimed, 0 when fully aimed.
                // Quantize to 0.1 steps so the 256x256 texture only rebakes ~10x across the
                // whole aim sweep instead of every frame (that rebake was the 5 FPS killer).
                float radarFocus = Mathf.InverseLerp(_gameStats._precisionMax, _gameStats._precisionMin, CurrentAngle);
                _radarLineSharpness = Mathf.Round(Mathf.Lerp(1f, 0f, radarFocus) * 10f) / 10f;

                if (_radarLineCount != _lastRadarLineCount || !Mathf.Approximately(_radarLineSharpness, _lastRadarLineSharpness))
                {
                    RegenerateRadarTexture();
                }

                bool isMoving = movementInput.magnitude > 0f || _turnedThisFrame > 0.01f;
                if (!isMoving)
                {
                    float recoverPerSecond = _gameStats._aimingSpeed;
                    CurrentAngle = Mathf.Max(_gameStats._precisionMin, CurrentAngle - recoverPerSecond * Time.deltaTime);
                }
                _turnedThisFrame = 0f;

                if (Recoil > 0f)
                {
                    CurrentAngle = Mathf.Max(CurrentAngle, Recoil);
                    Recoil = 0f;
                }

                CurrentAngle = Mathf.Clamp(CurrentAngle, _gameStats._precisionMin, _gameStats._precisionMax);

                UpdateLinePoints(CurrentAngle);
                ApplyVisuals(LineRendererLeft, _meshRendererAimingCircle, CurrentAngle);
                ApplyVisuals(LineRendererRight, _meshRendererAimingCircle, CurrentAngle);

                AnimateLaser(LineRendererLeft, _laserMat);
                AnimateLaser(LineRendererRight, _laserMat);

                UpdatePlasmaShots(true);
            }
            else
            {
                SetAimLinesActive(false);
                CurrentAngle = _gameStats._precisionStartingAim;

                if (_laserSmokeLeft.isPlaying) _laserSmokeLeft.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (_laserSmokeRight.isPlaying) _laserSmokeRight.Stop(true, ParticleSystemStopBehavior.StopEmitting);

                UpdatePlasmaShots(false);
            }

            RefreshAimLineWidth();
        }

        public void UpdateAimingCircle(bool isAiming, Quaternion playerRotation)
        {
            MoveAimingCircle();
            _gameObjectAimingCircle.transform.rotation = playerRotation;

            if (isAiming)
            {
                if (!_meshRendererAimingCircle.enabled) _meshRendererAimingCircle.enabled = true;
                GenerateMesh(CurrentAngle);

                if (!_laserSmokeLeft.isPlaying) _laserSmokeLeft.Play();
                if (!_laserSmokeRight.isPlaying) _laserSmokeRight.Play();

                AlignEmitterToLine(_laserSmokeLeft, LineRendererLeft, 0.02f);
                AlignEmitterToLine(_laserSmokeRight, LineRendererRight, 0.02f);
            }
            else
            {
                if (_meshRendererAimingCircle.enabled) _meshRendererAimingCircle.enabled = false;

                if (_laserSmokeLeft.isPlaying) _laserSmokeLeft.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (_laserSmokeRight.isPlaying) _laserSmokeRight.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        public void ClearAimingCircleOutlines()
        {
            _aimingCircleTrigger?.ClearAllOutlinedZombies();
        }

        public void DisableMeshRenderer()
        {
            if (_meshRendererAimingCircle != null && _meshRendererAimingCircle.enabled)
                _meshRendererAimingCircle.enabled = false;
        }

        public void ApplyMovementPenalty(bool isMoving, bool wasMoving, bool isRunning, float movementPenaltyBase)
        {
            if (_gameStats == null) return;
            float range = _gameStats._precisionMax - _gameStats._precisionMin;

            if (isMoving && !wasMoving)
            {
                float burst = range * (movementPenaltyBase / 100f);
                CurrentAngle = Mathf.Min(CurrentAngle + burst, _gameStats._precisionMax);
            }

            if (isMoving)
            {
                float perSecond = movementPenaltyBase * (isRunning ? 2f : 1f);
                CurrentAngle = Mathf.Min(CurrentAngle + perSecond * Time.deltaTime, _gameStats._precisionMax);
            }
        }

        public bool IsRadarInPrecisionZone()
        {
            return false;
        }

        // Swinging the aim with the mouse spreads the V the same way WASD movement does:
        // each degree the player turns this frame adds (penaltyBase/100) to the angle.
        public void ApplyTurnPenalty(float degreesTurned, float penaltyBase)
        {
            if (_gameStats == null || degreesTurned <= 0f) return;
            // Mark as "moving" so the standing-still recovery pauses this frame —
            // otherwise recovery eats the spread the same frame and the swing looks free.
            _turnedThisFrame = degreesTurned;
            float spread = degreesTurned * (penaltyBase / 100f);
            CurrentAngle = Mathf.Min(CurrentAngle + spread, _gameStats._precisionMax);
        }
        private float _turnedThisFrame;

        // ── Setup helpers ──────────────────────────────────────────────────────

        private void CreateLineRenderers()
        {
            // Modifying texture size to 128x32 to cleanly fit our procedural dash segments
            _laserTex = MakeLaserTexture(128, 32, 1.8f);

            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Particles/Additive");

            _laserMat = new Material(sh);
            _laserMat.mainTexture = _laserTex;
            _laserMat.SetColor("_BaseColor", laserColor);
            _laserMat.SetColor("_Color", laserColor);
            _laserMat.EnableKeyword("_EMISSION");
            _laserMat.SetColor("_EmissionColor", laserColor * emissionBase);

            LineRendererLeft = CreateLineRendererChild("LeftLine");
            LineRendererRight = CreateLineRendererChild("RightLine");
            SetupLaser(LineRendererLeft);
            SetupLaser(LineRendererRight);
            EnsureLineRenderersOnTop();
            SetLineAlpha(LineRendererLeft, 0);
            SetLineAlpha(LineRendererRight, 0);
        }

        private LineRenderer CreateLineRendererChild(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.parent = transform;
            LineRenderer lr = go.AddComponent<LineRenderer>();
            ConfigureLineRenderer(lr);
            return lr;
        }

        private int _lastRadarLineCount;
        private float _lastRadarLineSharpness;

        private void CreateAimingCircle()
        {
            _gameObjectAimingCircle = new GameObject("AnnularSector");
            _meshFilterAimingCircle = _gameObjectAimingCircle.AddComponent<MeshFilter>();
            _meshRendererAimingCircle = _gameObjectAimingCircle.AddComponent<MeshRenderer>();
            _meshColliderAimingCircle = _gameObjectAimingCircle.AddComponent<MeshCollider>();
            _meshColliderAimingCircle.convex = true;
            _meshColliderAimingCircle.isTrigger = true;

            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Particles/Additive")
                  ?? Shader.Find("Sprites/Default");

            _meshRendererAimingCircle.material = new Material(sh);

            _lastRadarLineCount = _radarLineCount;
            _lastRadarLineSharpness = _radarLineSharpness;
            _radarTex = MakeRadarTexture(256, 256, _radarLineCount, _radarLineSharpness);

            _meshRendererAimingCircle.material.mainTexture = _radarTex;
            _instancedRadarMat = _meshRendererAimingCircle.material;

            if (sh.name.Contains("Universal Render Pipeline"))
            {
                _instancedRadarMat.SetFloat("_Surface", 1);
                _instancedRadarMat.SetFloat("_Blend", 1);
                _instancedRadarMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _instancedRadarMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                _instancedRadarMat.SetInt("_ZWrite", 0);
                _instancedRadarMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                _instancedRadarMat.EnableKeyword("_ADDITIVE_BLENDING");
                _instancedRadarMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            _meshAimingCircle = new Mesh { name = "AnnularSector" };
            _meshFilterAimingCircle.mesh = _meshAimingCircle;
            _meshColliderAimingCircle.sharedMesh = _meshAimingCircle;

            Rigidbody rb = _gameObjectAimingCircle.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            _meshRendererAimingCircle.enabled = true;
            _aimingCircleTrigger = _gameObjectAimingCircle.AddComponent<AimingCircleTrigger>();
        }

        private void RegenerateRadarTexture()
        {
            _lastRadarLineCount = _radarLineCount;
            _lastRadarLineSharpness = _radarLineSharpness;

            if (_radarTex != null)
            {
                Destroy(_radarTex);
            }

            _radarTex = MakeRadarTexture(256, 256, _radarLineCount, _radarLineSharpness);

            if (_instancedRadarMat != null)
            {
                _instancedRadarMat.mainTexture = _radarTex;
            }
        }

        private void InitLaserMaterials()
        {
            if (_laserMat != null)
            {
                _laserMat.SetInt("_ZWrite", 0);
                _laserMat.renderQueue = 3000;
                _laserMat.SetInt("_ZTest", (int)CompareFunction.Always);
            }
        }

        private void InitLaserSmoke()
        {
            var prefab = Resources.Load<GameObject>(laserSmokePrefabPath);
            if (!prefab)
            {
                Debug.LogError("Smoke prefab not found at Resources/" + laserSmokePrefabPath);
                return;
            }
            var leftGO = Instantiate(prefab, transform);
            leftGO.name = "LaserSmoke_Left";
            var rightGO = Instantiate(prefab, transform);
            rightGO.name = "LaserSmoke_Right";
            _laserSmokeLeft = leftGO.GetComponent<ParticleSystem>();
            _laserSmokeRight = rightGO.GetComponent<ParticleSystem>();

            var mainLeft = _laserSmokeLeft.main;
            mainLeft.startColor = laserColor;
            var mainRight = _laserSmokeRight.main;
            mainRight.startColor = laserColor;

            _laserSmokeLeft.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _laserSmokeRight.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // ── Plasma shot V-arms ─────────────────────────────────────────────────

        private void InitPlasmaShots()
        {
            if (!_usePlasmaShot || _plasmaShotPrefab == null) return;

            // Remove orphans left over from a recompile/domain-reload during Play —
            // otherwise they keep running the raw prefab defaults (fast/far/white) uncontrolled.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i);
                if (c.name == "PlasmaShot_Left" || c.name == "PlasmaShot_Right")
                    Destroy(c.gameObject);
            }

            _plasmaLeft = Instantiate(_plasmaShotPrefab, transform);
            _plasmaLeft.name = "PlasmaShot_Left";
            _plasmaRight = Instantiate(_plasmaShotPrefab, transform);
            _plasmaRight.name = "PlasmaShot_Right";

            _plasmaRootLeft = _plasmaLeft.GetComponent<ParticleSystem>();
            _plasmaRootRight = _plasmaRight.GetComponent<ParticleSystem>();

            ConfigureStreamShooter(_plasmaLeft);
            ConfigureStreamShooter(_plasmaRight);

            _plasmaLeft.SetActive(false);
            _plasmaRight.SetActive(false);
        }

        // Turns the one-shot impact VFX into a cheap continuous dot-stream (a line).
        private void ConfigureStreamShooter(GameObject root)
        {
            var shooter = root.GetComponent<ParticleSystem>();

            foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (ps == shooter) continue;
                // The 7 children are Collision-triggered sub-emitters (impact burst ~131 particles).
                // For an aiming line we never want impacts — silence them entirely.
                var cem = ps.emission;
                cem.enabled = false;
                var cmain = ps.main;
                cmain.playOnAwake = false;
            }

            if (shooter == null) return;

            var main = shooter.main;
            main.loop = true;
            main.playOnAwake = true;
            main.maxParticles = 600;          // hard cap against runaway
            main.startColor = Color.white;     // gradient defines color, not a base tint

            // No impacts → no sub-emitter explosion.
            var col2 = shooter.collision;
            col2.enabled = false;
            var sub = shooter.subEmitters;
            sub.enabled = false;

            var em = shooter.emission;
            em.enabled = true;
            em.SetBursts(new ParticleSystem.Burst[0]); // drop the one-shot burst → continuous stream

            // Color is applied per-frame in PlacePlasma (depends on live weapon range vs arm length).
            var clo = shooter.colorOverLifetime;
            clo.enabled = true;
        }

        // Color along the shot's travel mapped to WEAPON distance bands:
        //   0..PointBlankRange  → white
        //   ..OptimalRange      → green
        //   ..MaxEffectiveRange → orange → red
        // Particle age == distance fraction (constant speed), so we convert world
        // distances into 0..1 positions along the current arm length.
        private Gradient _distGradient;
        private float _lastGradientArmLen = -1f;

        private Gradient BuildDistanceGradient(float armLen)
        {
            // Manual override only when explicitly toggled on.
            if (_useManualPlasmaColor && _plasmaColorOverDistance != null && _plasmaColorOverDistance.colorKeys.Length > 0)
                return _plasmaColorOverDistance;

            armLen = Mathf.Max(armLen, 0.01f);
            if (_distGradient != null && Mathf.Abs(armLen - _lastGradientArmLen) < 0.1f)
                return _distGradient;
            _lastGradientArmLen = armLen;
            if (_distGradient == null) _distGradient = new Gradient();

            var cfg = WeaponConfigSingleton.Instance?.WeaponConfig;
            float pb = cfg != null ? cfg.PointBlankRange : 2f;
            float opt = cfg != null ? cfg.OptimalRange : 6f;
            float max = cfg != null ? cfg.MaxEffectiveRange : 10f;

            Color white = Color.white;
            Color green = Color.green;
            Color orange = new Color(1f, 0.5f, 0f);
            Color red = Color.red;

            float tpb = pb / armLen;
            float topt = opt / armLen;
            float tmid = (opt + (max - opt) * 0.5f) / armLen;
            float tmax = max / armLen;

            // Force strictly-increasing, in-range key times (Gradient needs ordered keys).
            const float e = 0.001f;
            tmax = Mathf.Clamp(tmax, 4f * e, 1f - e);
            tmid = Mathf.Clamp(tmid, 3f * e, tmax - e);
            topt = Mathf.Clamp(topt, 2f * e, tmid - e);
            tpb = Mathf.Clamp(tpb, 1f * e, topt - e);

            _distGradient.SetKeys(
                new[] {
                    new GradientColorKey(white,  0f),
                    new GradientColorKey(white,  tpb),
                    new GradientColorKey(green,  topt),
                    new GradientColorKey(orange, tmid),
                    new GradientColorKey(red,    tmax),
                    new GradientColorKey(red,    1f),
                },
                new[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.92f),
                    new GradientAlphaKey(0f, 1f),
                });
            return _distGradient;
        }

        private void UpdatePlasmaShots(bool isAiming)
        {
            if (!_usePlasmaShot) return;

            // Self-heal: a script recompile / domain reload during Play wipes these
            // non-serialized refs without re-running Initialize. Rebuild on demand.
            if (_plasmaLeft == null || _plasmaRight == null)
            {
                if (_plasmaShotPrefab == null) return;
                InitPlasmaShots();
                if (_plasmaLeft == null || _plasmaRight == null) return;
            }

            // Plasma replaces the laser lines — hide the renderers while keeping points computed.
            if (LineRendererLeft != null) LineRendererLeft.enabled = false;
            if (LineRendererRight != null) LineRendererRight.enabled = false;

            if (!isAiming)
            {
                if (_plasmaLeft.activeSelf) _plasmaLeft.SetActive(false);
                if (_plasmaRight.activeSelf) _plasmaRight.SetActive(false);
                return;
            }

            if (!_plasmaLeft.activeSelf) _plasmaLeft.SetActive(true);
            if (!_plasmaRight.activeSelf) _plasmaRight.SetActive(true);

            PlacePlasma(_plasmaLeft, _plasmaRootLeft, LineRendererLeft);
            PlacePlasma(_plasmaRight, _plasmaRootRight, LineRendererRight);
        }

        private void PlacePlasma(GameObject plasma, ParticleSystem root, LineRenderer lr)
        {
            if (lr.positionCount < 2) return;
            Vector3 basePos = lr.GetPosition(0);
            Vector3 tip = lr.GetPosition(1);
            Vector3 dir = tip - basePos;
            float len = dir.magnitude;
            if (len < 0.0001f) return;

            // Emit from the base, fire toward the tip — the stream of shots forms the line.
            plasma.transform.position = basePos;
            plasma.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

            if (root != null)
            {
                float speed = Mathf.Max(_plasmaShotSpeed, 0.01f);
                float life = len / speed;                 // shot dies exactly at the tip
                var m = root.main;
                m.startSpeed = speed;
                m.startLifetime = life;

                var em = root.emission;
                em.rateOverTime = _plasmaShotsPerUnit * speed; // dots/sec → ~_plasmaShotsPerUnit per unit

                // Recolor by weapon distance bands (white→green→orange→red along the arm).
                var clo = root.colorOverLifetime;
                clo.enabled = true;
                clo.color = new ParticleSystem.MinMaxGradient(BuildDistanceGradient(len));
            }
        }

        // ── Private visual methods ─────────────────────────────────────────────

        private void MoveAimingCircle()
        {
            _gameObjectAimingCircle.transform.position = new Vector3(
                _characterController.transform.position.x,
                _characterController.transform.position.y - _characterController.height / 2f + _aimingCircleHeight,
                _characterController.transform.position.z);
        }

        private void SetAimLinesActive(bool on)
        {
            if (LineRendererLeft != null) LineRendererLeft.enabled = on;
            if (LineRendererRight != null) LineRendererRight.enabled = on;
            if (_laserMat != null)
            {
                float e = on ? emissionBase : 0f;
                _laserMat.SetColor("_EmissionColor", laserColor * e);
            }
        }

        private float ComputeVLength()
        {
            if (CurrentTargetDistance > 0f)
            {
                float maxRange = WeaponConfigSingleton.Instance?.WeaponConfig?.MaxEffectiveRange ?? _defaultVLength;
                return Mathf.Min(CurrentTargetDistance * 1.15f, maxRange);
            }
            return _defaultVLength;
        }

        private void UpdateLinePoints(float angle)
        {
            float currentRadius = ComputeVLength();
            float halfAngle = angle / 2f;
            float yOffset = 0.03f;

            Vector3 playerPosition = transform.position;
            Quaternion playerRotation = transform.rotation;
            Vector3 triangleBase = new Vector3(playerPosition.x,
                _gameObjectAimingCircle.transform.position.y + yOffset, playerPosition.z);

            Vector3 leftPoint = triangleBase + playerRotation * Quaternion.Euler(0, -halfAngle, 0) * Vector3.forward * currentRadius;
            Vector3 rightPoint = triangleBase + playerRotation * Quaternion.Euler(0, halfAngle, 0) * Vector3.forward * currentRadius;

            LineRendererLeft.positionCount = 2;
            LineRendererLeft.SetPositions(new[] { triangleBase, leftPoint });
            LineRendererRight.positionCount = 2;
            LineRendererRight.SetPositions(new[] { triangleBase, rightPoint });
        }

        private void ApplyVisuals(LineRenderer lineRenderer, MeshRenderer meshRenderer, float angle)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] {
                    new GradientColorKey(Color.white,           0.00f),
                    new GradientColorKey(Color.white,           0.08f),
                    new GradientColorKey(Color.green,           0.50f),
                    new GradientColorKey(Color.red,             0.92f),
                    new GradientColorKey(Color.red,             1.00f),
                },
                new[] {
                    new GradientAlphaKey(0.0f, 0.00f),
                    new GradientAlphaKey(1.0f, 0.05f),
                    new GradientAlphaKey(1.0f, 0.95f),
                    new GradientAlphaKey(0.0f, 1.00f),
                });
            lineRenderer.colorGradient = gradient;

            if (meshRenderer != null && _instancedRadarMat != null)
            {
                Color themeColor = Color.green;
                float focusProgress = Mathf.InverseLerp(_gameStats._precisionMax, _gameStats._precisionMin, angle);

                float boost = Mathf.Lerp(0.8f, 1.8f, focusProgress);
                Color baseCol = themeColor * boost;
                baseCol.a = 0.6f;

                _instancedRadarMat.color = baseCol;
                if (_instancedRadarMat.HasProperty("_BaseColor"))
                    _instancedRadarMat.SetColor("_BaseColor", baseCol);

                _radarTimeTranslation -= Time.deltaTime * _radarSpeed;
                _radarTimeTranslation %= 1f;

                if (_instancedRadarMat.HasProperty("_EmissionColor"))
                    _instancedRadarMat.SetColor("_EmissionColor", Color.black);

                _instancedRadarMat.SetInt("_ZWrite", 0);
                _instancedRadarMat.renderQueue = 3105;
            }
        }

        private void AnimateLaser(LineRenderer lr, Material mat)
        {
            float t = Time.time;
            float focusProgress = Mathf.InverseLerp(_gameStats._precisionMax, _gameStats._precisionMin, CurrentAngle);

            float currentScrollSpeed = Mathf.Lerp(scrollSpeed * 0.8f, scrollSpeed * 6f, focusProgress);
            Vector2 o = mat.mainTextureOffset;
            o.x = -t * currentScrollSpeed;
            mat.mainTextureOffset = o;

            // 2. DYNAMIC TILING FIX: Scale the texture pattern to the line's real-world length
            float lineLength = Vector3.Distance(lr.GetPosition(0), lr.GetPosition(1));
            // Adjust the '2.0f' multiplier to make the dashes tighter or longer
            mat.mainTextureScale = new Vector2(lineLength * 2.0f, 1f);


            float targetWidthMultiplier = Mathf.Lerp(1.4f, 0.6f, focusProgress);
            float stabilityPulse = 1.0f;

            if (focusProgress < 0.2f)
            {
                stabilityPulse = Mathf.Lerp(0.6f, 1.4f, 0.5f + 0.5f * Mathf.Sin(t * 45f));
                stabilityPulse *= (0.9f + UnityEngine.Random.value * 0.2f);
            }
            else if (focusProgress < 0.85f)
            {
                float pSpeed = Mathf.Lerp(pulseSpeed, pulseSpeed * 2f, focusProgress);
                stabilityPulse = Mathf.Lerp(0.9f, 1.1f, 0.5f + 0.5f * Mathf.Sin(t * pSpeed));
            }
            else
            {
                stabilityPulse = 1.0f + Mathf.Sin(t * 120f) * 0.03f;
            }

            lr.widthMultiplier = targetWidthMultiplier * stabilityPulse;

            float boost = Mathf.Lerp(0.8f, 1.8f, focusProgress);
            float e = emissionBase * (0.85f + 0.15f * Mathf.Sin(t * (pulseSpeed * 1.5f)) * emissionPulse) * boost;

            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);
            mat.SetColor("_EmissionColor", Color.white * e);
        }

        private void RefreshAimLineWidth()
        {
            if (!LineRendererLeft || !LineRendererRight || _mainCamera == null) return;

            Vector3 basePoint = LineRendererLeft.positionCount > 0
                ? LineRendererLeft.GetPosition(0)
                : transform.position;

            float dist = Vector3.Distance(_mainCamera.transform.position, basePoint);
            // Pixel-constant width when aimLinePixels>0, otherwise fall back to a real world width
            // (baseWidth). Without this fallback aimLinePixels=0 collapsed the beam to minWorldWidth.
            float worldWidth = aimLinePixels > 0
                ? GetWorldWidthForPixels(_mainCamera, dist, aimLinePixels)
                : baseWidth;
            worldWidth = Mathf.Clamp(worldWidth, minWorldWidth, maxWorldWidth);

            LineRendererLeft.startWidth = LineRendererLeft.endWidth = worldWidth;
            LineRendererRight.startWidth = LineRendererRight.endWidth = worldWidth;
        }

        private float GetWorldWidthForPixels(Camera cam, float distance, int pixels)
        {
            if (cam.orthographic)
            {
                float worldPerPixel = (2f * cam.orthographicSize) / Screen.height;
                return pixels * worldPerPixel;
            }
            else
            {
                float worldHeight = 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float worldPerPixel = worldHeight / Screen.height;
                return pixels * worldPerPixel;
            }
        }

        private void GenerateMesh(float angle)
        {
            int vertexCount = (_resolution + 1) * 2;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            int[] triangles = new int[_resolution * 6];
            float angleRad = Mathf.Deg2Rad * angle;
            float halfAngle = angleRad / 2f;
            float vLength = ComputeVLength();

            for (int i = 0; i <= _resolution; i++)
            {
                float t = i / (float)_resolution;
                float currentAngle = -halfAngle + (t * angleRad);
                float normalizedCenterBias = Mathf.Cos(currentAngle / halfAngle * Mathf.PI / 2f);
                float dynamicOuterRadius = vLength + _pointyTipFactor * normalizedCenterBias;

                vertices[i * 2] = new Vector3(0f, 0f, 0f);
                vertices[i * 2 + 1] = new Vector3(Mathf.Sin(currentAngle) * dynamicOuterRadius, 0f, Mathf.Cos(currentAngle) * dynamicOuterRadius);

                uvs[i * 2] = new Vector2(t, 0f + _radarTimeTranslation);
                uvs[i * 2 + 1] = new Vector2(t, 1f + _radarTimeTranslation);
            }

            int index = 0;
            for (int i = 0; i < _resolution; i++)
            {
                int innerStart = i * 2;
                int outerStart = i * 2 + 1;
                int outerNext = (i + 1) * 2 + 1;

                triangles[index++] = innerStart;
                triangles[index++] = outerStart;
                triangles[index++] = outerNext;
            }

            Color themeColor = AimPrecisionColors.GetOutlineColor(CurrentHitChance, IsPointBlank);
            Color[] colors = GenerateDirectionalSweepColors(vertexCount, _resolution, angle, themeColor);
            _meshAimingCircle.Clear();
            _meshAimingCircle.vertices = vertices;
            _meshAimingCircle.uv = uvs;
            _meshAimingCircle.triangles = triangles;
            _meshAimingCircle.RecalculateNormals();
            _meshAimingCircle.colors = colors;
            _meshColliderAimingCircle.sharedMesh = null;
            _meshColliderAimingCircle.sharedMesh = _meshAimingCircle;

            // Feed the true cone shape to the trigger so it can drop zombies that leave the
            // arc. The convex MeshCollider can't do this reliably (hull + per-frame rebuild).
            _aimingCircleTrigger?.SetConeShape(angle, vLength + _pointyTipFactor);
        }

        // Updated signature: Now generates a true 2D matrix texture with horizontal bands AND vertical grid ticks
        private Texture2D MakeRadarTexture(int width, int height, int lineCount, float sharpness)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;
                // Vertical alignment grids: check spacing across the horizontal axis
                float horizontalGrid = Mathf.Pow(Mathf.Sin(u * Mathf.PI * 6f), 40f);

                for (int y = 0; y < height; y++)
                {
                    float v = (float)y / height;
                    float fract = (v * lineCount) % 1.0f;
                    float horizontalSweepBands = Mathf.Pow(fract, sharpness);

                    // Combine horizontal target rings with sharp vertical grid marks
                    float combinedPattern = Mathf.Max(horizontalSweepBands, horizontalGrid * 0.4f);

                    Color c = Color.white;
                    c.a = combinedPattern;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        // Fixes the faded visual core: Aggressively dropping alpha in the dead center while spiking borders
        private Color[] GenerateDirectionalSweepColors(int vertexCount, int resolution, float angle, Color themeColor)
        {
            Color[] colors = new Color[vertexCount];

            for (int i = 0; i <= resolution; i++)
            {
                float horizontalT = (float)i / resolution;
                float horizontalDistFromCenter = Mathf.Abs(horizontalT - 0.5f) * 2f;

                // Sharp mathematical edge-glow power falloff curve
                float edgeGlow = Mathf.Pow(horizontalDistFromCenter, 4.0f);

                // Dead center drops down to near-invisible baseline (0.02f) to avoid hiding targets
                float baseAlpha = Mathf.Lerp(0.02f, 0.75f, edgeGlow);

                Color c = Color.white;

                c.a = baseAlpha * 0.1f;
                colors[i * 2] = c;

                c.a = baseAlpha;
                colors[i * 2 + 1] = c;
            }
            return colors;
        }

        private Color GetInterpolatedColor(float angle)
        {
            float minAngle = _gameStats._precisionMin;
            float maxAngle = _gameStats._precisionMax;
            float normalizedValue = Mathf.InverseLerp(minAngle, maxAngle, angle);

            Color deepPurple = new Color(0.8f, 0.2f, 1f);
            Color brightRed = new Color(1f, 0.1f, 0.1f);
            Color brightOrange = new Color(1f, 0.5f, 0f);
            Color brightYellow = new Color(1f, 0.9f, 0f);
            Color brightGreen = new Color(0.2f, 1f, 0.2f);

            if (normalizedValue > 0.8f)
                return Color.Lerp(brightRed, deepPurple, Mathf.InverseLerp(0.8f, 1f, normalizedValue));
            else if (normalizedValue > 0.6f)
                return Color.Lerp(brightOrange, brightRed, Mathf.InverseLerp(0.6f, 0.8f, normalizedValue));
            else if (normalizedValue > 0.4f)
                return Color.Lerp(brightYellow, brightOrange, Mathf.InverseLerp(0.4f, 0.6f, normalizedValue));
            else if (normalizedValue > 0.2f)
                return Color.Lerp(brightGreen, brightYellow, Mathf.InverseLerp(0.2f, 0.4f, normalizedValue));
            else
                return brightGreen;
        }

        private void AlignEmitterToLine(ParticleSystem ps, LineRenderer lr, float thickness)
        {
            if (lr.positionCount < 2) return;
            Vector3 a = lr.GetPosition(0);
            Vector3 b = lr.GetPosition(1);
            Vector3 dir = b - a;
            float len = dir.magnitude;
            if (len < 0.01f) return;

            var t = ps.transform;
            t.position = a + dir * 0.5f;
            t.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(thickness, thickness, Mathf.Max(0.1f, len));

            var em = ps.emission;
            em.rateOverTime = 20f * len;
        }

        private void ConfigureLineRenderer(LineRenderer lr)
        {
            lr.positionCount = 2;
            lr.startWidth = 0.051f;
            lr.endWidth = 0.01f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.green;
            lr.endColor = Color.green;
        }

        private void SetupLaser(LineRenderer lr)
        {
            lr.positionCount = 2;
            lr.widthMultiplier = baseWidth;
            lr.textureMode = LineTextureMode.Tile;

            lr.textureMode = LineTextureMode.Tile;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 2;
            lr.alignment = LineAlignment.View;
            var g = new Gradient();
            g.SetKeys(
                new[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(laserColor, 0.35f),
                    new GradientColorKey(laserColor, 0.65f),
                    new GradientColorKey(Color.white, 1f),
                },
                new[] {
                    new GradientAlphaKey(0.0f, 0.02f),
                    new GradientAlphaKey(1.0f, 0.08f),
                    new GradientAlphaKey(1.0f, 0.92f),
                    new GradientAlphaKey(0.0f, 0.98f),
                });
            lr.colorGradient = g;
            lr.material = _laserMat;
            if (_laserMat != null)
            {
                _laserMat.mainTexture = _laserTex;
            }
            lr.material = _laserMat;
        }

        private void EnsureLineRenderersOnTop()
        {
            LineRendererLeft.sortingOrder = 5000;
            LineRendererRight.sortingOrder = 5000;
            if (_laserMat != null)
            {
                _laserMat.SetInt("_ZWrite", 0);
                _laserMat.renderQueue = 3000;
                _laserMat.SetInt("_ZTest", (int)CompareFunction.Always);
            }
        }

        private void SetLineAlpha(LineRenderer lr, float alpha)
        {
            Color s = lr.startColor; s.a = alpha; lr.startColor = s;
            Color e = lr.endColor; e.a = alpha; lr.endColor = e;
        }

        // Two-layer laser cross-section: razor-thin white-hot core + wide soft colored glow halo,
        // continuous beam with soft travelling energy pulses (never breaks into dots).
        private Texture2D MakeLaserTexture(int width, int height, float sharpness)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                float d = Mathf.Abs(v - 0.5f) * 2f; // 0 = center, 1 = edge

                // Razor-thin white-hot core (saturates under additive blending → blooms)
                float core = Mathf.Exp(-Mathf.Pow(d * sharpness * 3.2f, 2f));
                // Wide soft colored glow halo around the core
                float glow = Mathf.Exp(-Mathf.Pow(d * sharpness * 1.0f, 2f)) * 0.55f;
                float alpha = Mathf.Clamp01(core + glow);

                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;

                    // Continuous beam: soft energy pulse travelling along length, never drops to 0
                    float pulse = 0.8f + 0.2f * (0.5f + 0.5f * Mathf.Sin(u * Mathf.PI * 6f));

                    Color c = Color.white;
                    c.a = alpha * pulse;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply(false, false);
            return tex;
        }
    }
}