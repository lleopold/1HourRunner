using System.Collections.Generic;
using Assets.Scripts.Game;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZombieGame
{
    /// <summary>
    /// Jagged Alliance style aiming reticle. Thin cone edges run from the player's feet to the tip and
    /// are coloured by the weapon's range bands (white point-blank -> green optimal -> yellow -> red past
    /// max effective); a single bright-white pulse loops up each edge to signal the gizmo is active.
    /// A feet ellipse, a bowed cap arc, and a target grid box (all neutral blue) complete it. The grid box
    /// travels player->tip as aim tightens (a reveal window over a conceptually cone-wide grid, fading at
    /// its edges). At full aim, procedural smoke rises off the edges. The cone collider mesh + SetConeShape
    /// is still generated each frame for zombie outlining; its MeshRenderer stays hidden.
    /// </summary>
    public class PlayerAimVisuals : MonoBehaviour
    {
        // ── Lines / thickness ──────────────────────────────────────────────────────
        [Header("Line Thickness")]
        [SerializeField] private int aimLinePixels = 2;       // constant on-screen thickness
        [SerializeField] private float minWorldWidth = 0.015f;
        [SerializeField] private float maxWorldWidth = 0.25f;
        [SerializeField] private float baseWidth = 0.05f;

        [Tooltip("Neutral colour for feet ring, cap arc and grid.")]
        [SerializeField] private Color _gizmoColor = new Color(0.42f, 0.72f, 0.92f, 1f);

        // ── Edge range-band colours ─────────────────────────────────────────────────
        [Header("Edge Range Colours")]
        [SerializeField] private Color _rangePointBlank = Color.white;
        [SerializeField] private Color _rangeOptimal = new Color(0.30f, 1f, 0.40f);
        [SerializeField] private Color _rangeYellow = new Color(1f, 0.9f, 0.2f);
        [SerializeField] private Color _rangeRed = new Color(1f, 0.3f, 0.15f);
        [Tooltip("Fraction of the edge that fades in at the player / fades out at the tip (soft ends).")]
        [SerializeField] private float _edgeFadeIn = 0.07f;
        [SerializeField] private float _edgeFadeOut = 0.12f;

        // ── Travelling pulse ────────────────────────────────────────────────────────
        [Header("Edge Pulse")]
        [SerializeField] private Color _pulseColor = Color.white;
        [Tooltip("Fraction of the edge travelled per second.")]
        [SerializeField] private float _pulseSpeed = 0.8f;
        [Tooltip("Half length of the bright band, as a fraction of the edge.")]
        [SerializeField] private float _pulseHalfLen = 0.11f;
        [SerializeField] private float _pulseWidthMul = 2.0f;

        // ── Target grid reveal window ────────────────────────────────────────────────
        [Header("Target Grid")]
        [SerializeField] private bool _showGrid = true;
        [Tooltip("Grid colour — set here, NOT on the child GridV/GridH LineRenderers (code rewrites those each frame). Alpha scales overall grid opacity.")]
        [SerializeField] private Color _gridColor = new Color(0.42f, 0.72f, 0.92f, 1f);
        [SerializeField] private int _gridCols = 4;
        [SerializeField] private int _gridRows = 3;
        [SerializeField] private float _gridDepthFraction = 0.22f;   // window depth / cone length
        [SerializeField] private float _gridParkFraction = 0.85f;    // parked centre along cone (no target)
        [Tooltip("Grid width as a fraction of the cone width at each depth (<1 = inset inside the V).")]
        [SerializeField] private float _gridInset = 0.78f;
        [SerializeField] private float _gridWidthMul = 1.8f;
        [SerializeField] private float _gridAlpha = 0.85f;
        [SerializeField] private float _gridEdgeFalloff = 2f;        // higher = sharper centre focus
        [Tooltip("Dimmest an outer grid line gets (0 = invisible edges, 1 = no edge fade).")]
        [SerializeField] private float _gridEdgeMin = 0.55f;
        [Tooltip("How opaque each line's ends stay relative to its middle (0 = fade to nothing).")]
        [SerializeField] private float _gridEndSoftness = 0.6f;
        [Tooltip("Grid opacity floor at zero aim focus (0 = fully transparent until you aim).")]
        [SerializeField] private float _gridMinAppear = 0f;
        [Tooltip("Bright flash on reaching full aim: strength and how long it decays.")]
        [SerializeField] private float _gridFlashStrength = 3f;
        [SerializeField] private float _gridFlashDuration = 0.3f;

        // ── Feet ring / cap ─────────────────────────────────────────────────────────
        [Header("Feet Ring / Cap")]
        [SerializeField] private bool _showFeetRing = true;
        [SerializeField] private float _feetRadius = 0.6f;
        [SerializeField] private int _feetSegments = 48;

        // ── Full-aim smoke ──────────────────────────────────────────────────────────
        [Header("Full-Aim Smoke")]
        [SerializeField] private bool _showSmoke = true;
        [SerializeField] private float _lockThreshold = 0.97f;       // focus >= this == fully aimed
        [SerializeField] private Color _smokeColor = new Color(0.72f, 0.76f, 0.82f, 0.5f);
        [SerializeField] private float _smokeRate = 45f;             // particles/sec while locked
        [SerializeField] private float _smokeRiseSpeed = 1.2f;
        [SerializeField] private float _smokeLifetime = 1.3f;
        [SerializeField] private float _smokeStartSize = 0.28f;

        // ── Cone collider (hidden renderer, kept for trigger) ────────────────────────
        [Header("Cone Collider")]
        [SerializeField] private int _resolution = 30;
        [SerializeField] private float _pointyTipFactor = 0.25f;
        [SerializeField] private float _defaultVLength = 10f;

        // ── Public API (driven by PlayerControllerInput) ────────────────────────
        public float CurrentAngle { get; set; }
        public float Recoil { get; set; }
        public bool IsPrecisionShot { get; private set; }
        public float CurrentHitChance { get; set; } = 1f;
        public float CurrentDistanceMultiplier { get; set; } = 1f;
        public bool IsPointBlank { get; set; }
        public float CurrentTargetDistance { get; set; } = -1f;

        public LineRenderer LineRendererLeft { get; private set; }
        public LineRenderer LineRendererRight { get; private set; }

        public AimingCircleTrigger AimingCircleTrigger => _aimingCircleTrigger;

        // ── Internals ───────────────────────────────────────────────────────────
        private Material _lineMat;
        private Material _gridMat;      // additive glow for the target grid
        private Texture2D _lineTex;

        private LineRenderer _pulseLeft, _pulseRight;
        private LineRenderer _capLine;
        private LineRenderer _feetRing;
        private LineRenderer[] _gridV;   // vertical (along depth) lines
        private LineRenderer[] _gridH;   // horizontal (across width) lines
        private readonly List<LineRenderer> _allLines = new List<LineRenderer>();
        private readonly List<LineRenderer> _gridLines = new List<LineRenderer>();

        private ParticleSystem _smoke;
        private float _smokeAccum;

        private GameObject _gameObjectAimingCircle;
        private Mesh _meshAimingCircle;
        private MeshFilter _meshFilterAimingCircle;
        private MeshRenderer _meshRendererAimingCircle;
        private MeshCollider _meshColliderAimingCircle;
        private AimingCircleTrigger _aimingCircleTrigger;
        private readonly float _aimingCircleHeight = 0.11f;

        private Camera _mainCamera;
        private CharacterController _characterController;
        private GameStats _gameStats;
        private float _turnedThisFrame;

        private float _pulsePos;               // 0..1 travelling band centre
        private bool _gridWasLocked;           // lock edge detect for the flash
        private float _gridFlashT;             // 1 -> 0 after a lock flash
        private Vector3 _baseP, _leftTip, _rightTip;   // cached each frame from UpdateConeLines

        // ─────────────────────────────────────────────────────────────────────────
        internal void Initialize(GameStats gameStats)
        {
            _gameStats = gameStats;
            _mainCamera = Camera.main;
            _characterController = GetComponent<CharacterController>();

            BuildLineMaterial();
            CreateLines();
            CreateAimingCircle();
            CreateSmoke();

            CurrentAngle = gameStats._precisionMax;
        }

        private float FocusProgress()
            => Mathf.Clamp01(Mathf.InverseLerp(_gameStats._precisionMax, _gameStats._precisionMin, CurrentAngle));

        // ── Public API ───────────────────────────────────────────────────────────

        public void UpdateVisuals(bool isAiming, Vector2 movementInput)
        {
            if (isAiming)
            {
                SetLinesActive(true);

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

                UpdateConeLines(CurrentAngle);
                UpdatePulse();
                UpdateFeetRing();
                UpdateGrid();
                UpdateSmoke(true);
            }
            else
            {
                SetLinesActive(false);
                UpdateSmoke(false);
                _gridWasLocked = false;
                _gridFlashT = 0f;
                CurrentAngle = _gameStats._precisionStartingAim;
            }

            RefreshLineWidth();
        }

        public void UpdateAimingCircle(bool isAiming, Quaternion playerRotation)
        {
            MoveAimingCircle();
            _gameObjectAimingCircle.transform.rotation = playerRotation;

            if (_meshRendererAimingCircle.enabled) _meshRendererAimingCircle.enabled = false;
            if (isAiming) GenerateColliderMesh(CurrentAngle);
        }

        public void ClearAimingCircleOutlines() => _aimingCircleTrigger?.ClearAllOutlinedZombies();

        public void DisableMeshRenderer()
        {
            if (_meshRendererAimingCircle != null && _meshRendererAimingCircle.enabled)
                _meshRendererAimingCircle.enabled = false;
            SetLinesActive(false);
            UpdateSmoke(false);
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

        public void ApplyTurnPenalty(float degreesTurned, float penaltyBase)
        {
            if (_gameStats == null || degreesTurned <= 0f) return;
            _turnedThisFrame = degreesTurned;
            float spread = degreesTurned * (penaltyBase / 100f);
            CurrentAngle = Mathf.Min(CurrentAngle + spread, _gameStats._precisionMax);
        }

        public bool IsRadarInPrecisionZone() => false;

        // ── Geometry helpers ───────────────────────────────────────────────────────

        private float ComputeVLength()
            => WeaponConfigSingleton.Instance?.WeaponConfig?.MaxEffectiveRange ?? _defaultVLength;

        private Vector3 GroundBase(float yLift)
        {
            Vector3 p = transform.position;
            return new Vector3(p.x, _gameObjectAimingCircle.transform.position.y + yLift, p.z);
        }

        // ── Cone edges (range gradient) + far cap ────────────────────────────────────

        private void UpdateConeLines(float angle)
        {
            float radius = ComputeVLength();
            float half = angle / 2f;
            Quaternion rot = transform.rotation;
            _baseP = GroundBase(0.03f);

            _leftTip = _baseP + rot * Quaternion.Euler(0, -half, 0) * Vector3.forward * radius;
            _rightTip = _baseP + rot * Quaternion.Euler(0, half, 0) * Vector3.forward * radius;

            Gradient g = BuildRangeGradient(radius);
            LineRendererLeft.colorGradient = g;
            LineRendererRight.colorGradient = g;

            // Subdivide so the gradient (soft fade-in/out + range colour bands) renders smoothly —
            // a 2-point line only samples the gradient at its endpoints.
            SetLinePoints(LineRendererLeft, _baseP, _leftTip, 24);
            SetLinePoints(LineRendererRight, _baseP, _rightTip, 24);

            const int capSeg = 10;
            _capLine.positionCount = capSeg + 1;
            for (int i = 0; i <= capSeg; i++)
            {
                float a = Mathf.Lerp(-half, half, i / (float)capSeg);
                _capLine.SetPosition(i, _baseP + rot * Quaternion.Euler(0, a, 0) * Vector3.forward * radius);
            }
        }

        private static void SetLinePoints(LineRenderer lr, Vector3 a, Vector3 b, int segments)
        {
            lr.positionCount = segments + 1;
            for (int i = 0; i <= segments; i++)
                lr.SetPosition(i, Vector3.Lerp(a, b, i / (float)segments));
        }

        // White (point-blank) -> green (optimal) -> yellow -> red (past max effective), along the beam.
        private Gradient BuildRangeGradient(float length)
        {
            var cfg = WeaponConfigSingleton.Instance?.WeaponConfig;
            float pb = cfg != null ? cfg.PointBlankRange : 2f;
            float opt = cfg != null ? cfg.OptimalRange : 6f;

            float tpb = Mathf.Clamp(pb / length, 0.02f, 0.9f);
            float topt = Mathf.Clamp(opt / length, tpb + 0.02f, 0.95f);
            float tyel = Mathf.Clamp((topt + 1f) * 0.5f, topt + 0.02f, 0.98f);

            float aIn = Mathf.Clamp(_edgeFadeIn, 0.001f, 0.45f);
            float aOut = Mathf.Clamp(1f - _edgeFadeOut, aIn + 0.01f, 0.999f);

            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(_rangePointBlank, 0f),
                    new GradientColorKey(_rangePointBlank, tpb),
                    new GradientColorKey(_rangeOptimal, topt),
                    new GradientColorKey(_rangeYellow, tyel),
                    new GradientColorKey(_rangeRed, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, aIn),
                    new GradientAlphaKey(1f, aOut),
                    new GradientAlphaKey(0f, 1f),
                });
            return g;
        }

        // ── Travelling pulse (one white band per edge, looping) ──────────────────────

        private void UpdatePulse()
        {
            _pulsePos += _pulseSpeed * Time.deltaTime;
            if (_pulsePos - _pulseHalfLen > 1f) _pulsePos = 0f;   // gone off the tip -> restart at feet

            // Rebuilt each frame so Pulse Color edits apply live.
            var pg = WindowGradient(_pulseColor, 1f, 0f);
            _pulseLeft.colorGradient = pg;
            _pulseRight.colorGradient = pg;

            PlacePulse(_pulseLeft, _baseP, _leftTip);
            PlacePulse(_pulseRight, _baseP, _rightTip);
        }

        private void PlacePulse(LineRenderer lr, Vector3 a, Vector3 b)
        {
            const int seg = 8;
            float t0 = Mathf.Clamp01(_pulsePos - _pulseHalfLen);
            float t2 = Mathf.Clamp01(_pulsePos + _pulseHalfLen);
            lr.positionCount = seg + 1;
            for (int i = 0; i <= seg; i++)
                lr.SetPosition(i, Vector3.Lerp(a, b, Mathf.Lerp(t0, t2, i / (float)seg)));
        }

        // ── Feet ellipse ─────────────────────────────────────────────────────────────

        private void UpdateFeetRing()
        {
            if (!_showFeetRing) { _feetRing.enabled = false; return; }
            _feetRing.enabled = true;

            Vector3 c = GroundBase(0.015f);
            _feetRing.positionCount = _feetSegments + 1;
            for (int i = 0; i <= _feetSegments; i++)
            {
                float a = (i / (float)_feetSegments) * Mathf.PI * 2f;
                _feetRing.SetPosition(i, c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * _feetRadius);
            }
        }

        // ── Target grid — reveal window travelling player -> tip with aim focus ──────

        private void UpdateGrid()
        {
            if (!_showGrid) { foreach (var lr in _gridLines) lr.enabled = false; return; }

            float vLen = ComputeVLength();
            float half = CurrentAngle / 2f;
            float halfRad = half * Mathf.Deg2Rad;
            Quaternion rot = transform.rotation;
            Vector3 baseP = GroundBase(0.02f);

            float focus = FocusProgress();

            // Fully transparent when not aimed -> opaque as aim tightens.
            float appear = Mathf.Lerp(_gridMinAppear, 1f, focus);

            // Single bright flash the moment full aim (lock) is reached.
            bool locked = focus >= _lockThreshold;
            if (locked && !_gridWasLocked) _gridFlashT = 1f;
            _gridWasLocked = locked;
            if (_gridFlashT > 0f) _gridFlashT = Mathf.Max(0f, _gridFlashT - Time.deltaTime / _gridFlashDuration);
            float flash = 1f + _gridFlashT * _gridFlashStrength;

            // Trapezoid that follows the cone (inset by _gridInset so it sits inside the V), parked near
            // the tip or on the in-cone target, and never past the tip.
            float tan = Mathf.Tan(halfRad);
            float center = CurrentTargetDistance > 0f
                ? Mathf.Clamp(CurrentTargetDistance, vLen * 0.3f, vLen * 0.9f)
                : vLen * _gridParkFraction;
            float depth = vLen * _gridDepthFraction;
            float far = Mathf.Min(center + depth * 0.5f, vLen * 0.97f);
            float near = Mathf.Max(0.1f, far - depth);

            float wNear = near * tan * _gridInset;
            float wFar = far * tan * _gridInset;

            Vector3 L(float x, float z) => baseP + rot * new Vector3(x, 0f, z);
            float LineAlpha(float centreFade) =>
                Mathf.Clamp01(Mathf.Lerp(1f, _gridEdgeMin, centreFade) * _gridAlpha * appear * _gridColor.a * flash);

            // Vertical lines fan out with the cone (near x -> far x); depth reveal-window fade along length.
            for (int j = 0; j < _gridV.Length; j++)
            {
                float xf = _gridV.Length == 1 ? 0.5f : j / (float)(_gridV.Length - 1);
                float s = xf * 2f - 1f;
                float peak = LineAlpha(Mathf.Pow(Mathf.Abs(s), _gridEdgeFalloff));

                var lr = _gridV[j];
                lr.enabled = true;
                lr.positionCount = 2;
                lr.SetPositions(new[] { L(s * wNear, near), L(s * wFar, far) });
                lr.colorGradient = WindowGradient(_gridColor, peak, peak * _gridEndSoftness);
            }

            // Horizontal lines span the cone width at their depth; edge fade across width.
            for (int i = 0; i < _gridH.Length; i++)
            {
                float df = _gridH.Length == 1 ? 0.5f : i / (float)(_gridH.Length - 1);
                float z = Mathf.Lerp(near, far, df);
                float w = z * tan * _gridInset;
                float peak = LineAlpha(Mathf.Pow(Mathf.Abs(df - 0.5f) * 2f, _gridEdgeFalloff));

                var lr = _gridH[i];
                lr.enabled = true;
                lr.positionCount = 2;
                lr.SetPositions(new[] { L(-w, z), L(w, z) });
                lr.colorGradient = WindowGradient(_gridColor, peak, peak * _gridEndSoftness);
            }
        }

        // `endAlpha` at both ends, `peak` in the middle — a soft-edged visible slice.
        private Gradient WindowGradient(Color color, float peak, float endAlpha)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[]
                {
                    new GradientAlphaKey(endAlpha, 0f),
                    new GradientAlphaKey(peak, 0.5f),
                    new GradientAlphaKey(endAlpha, 1f),
                });
            return g;
        }

        // ── Full-aim smoke ───────────────────────────────────────────────────────────

        private void UpdateSmoke(bool isAiming)
        {
            if (_smoke == null || !_showSmoke) return;

            bool locked = isAiming && FocusProgress() >= _lockThreshold;
            if (!locked) { _smokeAccum = 0f; return; }

            _smokeAccum += _smokeRate * Time.deltaTime;
            int n = Mathf.FloorToInt(_smokeAccum);
            _smokeAccum -= n;

            for (int i = 0; i < n; i++)
            {
                bool leftEdge = Random.value < 0.5f;
                float t = Random.value;
                Vector3 pos = Vector3.Lerp(_baseP, leftEdge ? _leftTip : _rightTip, t) + Vector3.up * 0.05f;
                var ep = new ParticleSystem.EmitParams
                {
                    position = pos,
                    velocity = Vector3.up * _smokeRiseSpeed
                               + new Vector3(Random.Range(-0.2f, 0.2f), 0f, Random.Range(-0.2f, 0.2f)),
                    startColor = _smokeColor,
                    startSize = _smokeStartSize * Random.Range(0.7f, 1.3f),
                    startLifetime = _smokeLifetime * Random.Range(0.8f, 1.2f),
                };
                _smoke.Emit(ep, 1);
            }
        }

        // ── Cone collider mesh (hidden; feeds AimingCircleTrigger) ───────────────────

        private void GenerateColliderMesh(float angle)
        {
            int vertexCount = (_resolution + 1) * 2;
            Vector3[] vertices = new Vector3[vertexCount];
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

                vertices[i * 2] = Vector3.zero;
                vertices[i * 2 + 1] = new Vector3(Mathf.Sin(currentAngle) * dynamicOuterRadius, 0f, Mathf.Cos(currentAngle) * dynamicOuterRadius);
            }

            int index = 0;
            for (int i = 0; i < _resolution; i++)
            {
                triangles[index++] = i * 2;
                triangles[index++] = i * 2 + 1;
                triangles[index++] = (i + 1) * 2 + 1;
            }

            _meshAimingCircle.Clear();
            _meshAimingCircle.vertices = vertices;
            _meshAimingCircle.triangles = triangles;
            _meshColliderAimingCircle.sharedMesh = null;
            _meshColliderAimingCircle.sharedMesh = _meshAimingCircle;

            _aimingCircleTrigger?.SetConeShape(angle, vLength + _pointyTipFactor);
        }

        private void CreateAimingCircle()
        {
            _gameObjectAimingCircle = new GameObject("AnnularSector");
            _meshFilterAimingCircle = _gameObjectAimingCircle.AddComponent<MeshFilter>();
            _meshRendererAimingCircle = _gameObjectAimingCircle.AddComponent<MeshRenderer>();
            _meshColliderAimingCircle = _gameObjectAimingCircle.AddComponent<MeshCollider>();
            _meshColliderAimingCircle.convex = true;
            _meshColliderAimingCircle.isTrigger = true;

            _meshAimingCircle = new Mesh { name = "AnnularSector" };
            _meshFilterAimingCircle.mesh = _meshAimingCircle;
            _meshColliderAimingCircle.sharedMesh = _meshAimingCircle;

            Rigidbody rb = _gameObjectAimingCircle.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            _meshRendererAimingCircle.enabled = false;   // JA look: collider only, no fill
            _aimingCircleTrigger = _gameObjectAimingCircle.AddComponent<AimingCircleTrigger>();
        }

        private void MoveAimingCircle()
        {
            _gameObjectAimingCircle.transform.position = new Vector3(
                _characterController.transform.position.x,
                _characterController.transform.position.y - _characterController.height / 2f + _aimingCircleHeight,
                _characterController.transform.position.z);
        }

        // ── Line + smoke setup ─────────────────────────────────────────────────────────

        private void CreateLines()
        {
            LineRendererLeft = NewLine("EdgeLeft");
            LineRendererRight = NewLine("EdgeRight");
            _pulseLeft = NewLine("PulseLeft");
            _pulseRight = NewLine("PulseRight");
            _capLine = NewLine("ConeCap");
            _feetRing = NewLine("FeetRing");

            // Pulses: fixed white band, alpha 0->1->0 across the segment (set once).
            var pulseGrad = WindowGradient(_pulseColor, 1f, 0f);
            _pulseLeft.colorGradient = pulseGrad;
            _pulseRight.colorGradient = pulseGrad;
            _pulseLeft.sortingOrder = _pulseRight.sortingOrder = 5010;

            _gridV = new LineRenderer[Mathf.Max(2, _gridCols + 1)];
            _gridH = new LineRenderer[Mathf.Max(2, _gridRows + 1)];
            for (int i = 0; i < _gridV.Length; i++) { _gridV[i] = NewLine("GridV" + i); _gridV[i].material = _gridMat; _gridLines.Add(_gridV[i]); }
            for (int i = 0; i < _gridH.Length; i++) { _gridH[i] = NewLine("GridH" + i); _gridH[i].material = _gridMat; _gridLines.Add(_gridH[i]); }

            SetLinesActive(false);
        }

        private void SetLinesActive(bool on)
        {
            foreach (var lr in _allLines) if (lr != null) lr.enabled = on;
            if (!on) return;
            if (!_showFeetRing) _feetRing.enabled = false;
            if (!_showGrid) foreach (var lr in _gridLines) lr.enabled = false;
        }

        private void RefreshLineWidth()
        {
            if (_mainCamera == null || _allLines.Count == 0) return;
            float dist = Vector3.Distance(_mainCamera.transform.position, transform.position);
            float w = aimLinePixels > 0 ? WorldWidthForPixels(_mainCamera, dist, aimLinePixels) : baseWidth;
            w = Mathf.Clamp(w, minWorldWidth, maxWorldWidth);

            foreach (var lr in _allLines)
                if (lr != null) lr.startWidth = lr.endWidth = w;

            float pw = w * _pulseWidthMul;
            _pulseLeft.startWidth = _pulseLeft.endWidth = pw;
            _pulseRight.startWidth = _pulseRight.endWidth = pw;

            float gw = w * _gridWidthMul;
            foreach (var lr in _gridLines)
                if (lr != null) lr.startWidth = lr.endWidth = gw;
        }

        private float WorldWidthForPixels(Camera cam, float distance, int pixels)
        {
            if (cam.orthographic)
                return pixels * (2f * cam.orthographicSize) / Screen.height;
            float worldHeight = 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            return pixels * (worldHeight / Screen.height);
        }

        private void BuildLineMaterial()
        {
            _lineTex = MakeLineTexture(8, 32, 1.8f);
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Additive");
            _lineMat = new Material(sh) { mainTexture = _lineTex };
            _lineMat.SetColor("_BaseColor", Color.white);
            _lineMat.SetColor("_Color", Color.white);
            _lineMat.SetInt("_ZWrite", 0);
            _lineMat.SetInt("_ZTest", (int)CompareFunction.Always);
            _lineMat.renderQueue = 3000;

            // Grid uses alpha blending (additive washed out over bright ground) with a soft-edged line
            // texture so it stays visibly blue while still looking like glowing light, not a solid decal.
            _gridMat = new Material(sh) { mainTexture = _lineTex };
            _gridMat.SetColor("_BaseColor", Color.white);
            _gridMat.SetColor("_Color", Color.white);
            if (sh.name.Contains("Universal Render Pipeline"))
            {
                _gridMat.SetFloat("_Surface", 1);
                _gridMat.SetFloat("_Blend", 0);
            }
            _gridMat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            _gridMat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            _gridMat.SetInt("_ZWrite", 0);
            _gridMat.SetInt("_ZTest", (int)CompareFunction.Always);
            _gridMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _gridMat.renderQueue = 3000;
        }

        private LineRenderer NewLine(string name)
        {
            var go = new GameObject(name);
            go.transform.parent = transform;
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.numCapVertices = 2;
            lr.numCornerVertices = 2;
            lr.alignment = LineAlignment.View;
            lr.textureMode = LineTextureMode.Stretch;
            lr.material = _lineMat;
            lr.startColor = lr.endColor = _gizmoColor;
            lr.sortingOrder = 5000;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.enabled = false;
            _allLines.Add(lr);
            return lr;
        }

        private void CreateSmoke()
        {
            var go = new GameObject("AimSmoke");
            go.transform.parent = transform;
            go.transform.localPosition = Vector3.zero;

            _smoke = go.AddComponent<ParticleSystem>();
            _smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _smoke.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;                    // velocity supplied per-particle
            main.startSize = _smokeStartSize;
            main.startLifetime = _smokeLifetime;
            main.gravityModifier = -0.03f;           // gentle rise
            main.maxParticles = 400;
            main.playOnAwake = false;

            var emission = _smoke.emission; emission.enabled = false;   // manual Emit only
            var shape = _smoke.shape; shape.enabled = false;

            var col = _smoke.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            var sol = _smoke.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.6f), new Keyframe(1f, 1.6f)));

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.material = BuildSmokeMaterial();
            psr.sortingOrder = 5020;
            psr.shadowCastingMode = ShadowCastingMode.Off;
            psr.receiveShadows = false;

            _smoke.Play();
        }

        private Material BuildSmokeMaterial()
        {
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit")
                     ?? Shader.Find("Sprites/Default");
            var m = new Material(sh) { mainTexture = MakeBlobTexture(64) };
            if (sh.name.Contains("Universal Render Pipeline"))
            {
                m.SetFloat("_Surface", 1);            // transparent
                m.SetFloat("_Blend", 0);              // alpha blend
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            m.SetColor("_BaseColor", Color.white);
            m.SetColor("_Color", Color.white);
            return m;
        }

        // Soft cross-section line: bright core + faint glow, white so vertex colour tints it.
        private Texture2D MakeLineTexture(int width, int height, float sharpness)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                float d = Mathf.Abs(v - 0.5f) * 2f;
                float core = Mathf.Exp(-Mathf.Pow(d * sharpness * 3.2f, 2f));
                float glow = Mathf.Exp(-Mathf.Pow(d * sharpness * 1.0f, 2f)) * 0.5f;
                float alpha = Mathf.Clamp01(core + glow);
                for (int x = 0; x < width; x++)
                {
                    Color c = Color.white; c.a = alpha;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply(false, false);
            return tex;
        }

        // Round soft blob for smoke particles.
        private Texture2D MakeBlobTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - r) / r, dy = (y - r) / r;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - dist);
                    a = a * a;
                    Color c = Color.white; c.a = a;
                    tex.SetPixel(x, y, c);
                }
            tex.Apply(false, false);
            return tex;
        }
    }
}
