using System.Collections.Generic;
using Assets.Scripts.Game;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZombieGame
{
    /// <summary>
    /// Aiming reticle: a cone whose width = current aim spread, two laser edges, range rings at the
    /// weapon's point-blank / optimal / max bands, and a submarine-sonar sweep that bounces across
    /// the cone (blipping zombies it passes). Color follows hit chance (orange spread -> green lock);
    /// a full lock fires a one-shot pulse and solidifies the edges.
    /// </summary>
    public class PlayerAimVisuals : MonoBehaviour
    {
        // ── Cone / radar fill ───────────────────────────────────────────────────
        [Header("Cone Fill")]
        [SerializeField] private int _resolution = 30;
        [SerializeField] private float _pointyTipFactor = 0.25f;
        [SerializeField] private int _radarLineCount = 6;
        [SerializeField] private float _radarLineSharpness = 24f;
        [SerializeField] private float _scanlineAlpha = 0.18f;   // thinner scanline than before

        // ── Laser edges ─────────────────────────────────────────────────────────
        [Header("Laser Edges")]
        [SerializeField] private float scrollSpeed = 1.6f;
        [SerializeField] private float pulseSpeed = 2.2f;
        [SerializeField] private float emissionBase = 1.4f;
        [SerializeField] private float emissionPulse = 1.0f;
        [SerializeField] private int aimLinePixels = 3;
        [SerializeField] private float minWorldWidth = 0.02f;
        [SerializeField] private float maxWorldWidth = 0.25f;
        [SerializeField] private float baseWidth = 0.06f;

        // ── Range rings ─────────────────────────────────────────────────────────
        [Header("Range Rings (point-blank / optimal / max)")]
        [SerializeField] private bool _showRings = true;
        [SerializeField] private float _ringWidth = 0.05f;
        [SerializeField] private float _ringAlpha = 0.5f;
        [SerializeField] private int _ringSegments = 20;
        [SerializeField] private Color _ringPointBlank = Color.white;
        [SerializeField] private Color _ringOptimal = Color.green;
        [SerializeField] private Color _ringMax = new Color(1f, 0.25f, 0.1f);

        // ── Sonar sweep ─────────────────────────────────────────────────────────
        [Header("Sonar Sweep")]
        [SerializeField] private bool _showSweep = true;
        [SerializeField] private float _sweepCyclesPerSecond = 0.5f; // full left->right->left per 2s
        [SerializeField] private float _sweepWidth = 0.07f;
        [SerializeField] private int _sweepTrailCount = 5;           // faded trailing lines
        [SerializeField] private float _sweepTrailAngleStep = 2.5f;  // deg between trail copies
        [SerializeField] private float _blipAngleTolerance = 2.5f;   // deg window for a pass-blip

        // ── Lock ────────────────────────────────────────────────────────────────
        [Header("Lock")]
        [SerializeField] private float _lockThreshold = 0.97f;       // focus >= this == fully aimed
        [SerializeField] private float _lockPulseDuration = 0.25f;
        [SerializeField] private float _lockPulseStrength = 2.5f;
        [Tooltip("Locked-only travelling energy pulse along the edges: fraction of the arm per second.")]
        [SerializeField] private float _edgePulseTravelSpeed = 1.6f;
        [Tooltip("Half-width of the travelling pulse (fraction of the arm).")]
        [SerializeField] private float _edgePulseWidth = 0.08f;

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
        private Material _laserMat;
        private Texture2D _laserTex;

        private GameObject _gameObjectAimingCircle;
        private Mesh _meshAimingCircle;
        private MeshFilter _meshFilterAimingCircle;
        private MeshRenderer _meshRendererAimingCircle;
        private MeshCollider _meshColliderAimingCircle;
        private AimingCircleTrigger _aimingCircleTrigger;
        private Material _radarMat;
        private Texture2D _radarTex;
        private readonly float _aimingCircleHeight = 0.11f;

        private LineRenderer[] _rings;          // [0]=PB [1]=optimal [2]=max
        private Material _ringMat;

        private LineRenderer[] _sweepLines;     // [0]=head, rest=trail
        private Material _sweepMat;
        private float _sweepPhase;              // 0..1 ping-pong driver
        private float _prevSweepAngleDeg;
        private float _radarTimeTranslation;

        private bool _isLocked;
        private float _lockPulseT;              // 1 -> 0 after a lock
        private float _edgePulsePos;            // 0..1 travelling pulse along the edge while locked

        private Camera _mainCamera;
        private CharacterController _characterController;
        private GameStats _gameStats;
        private float _turnedThisFrame;

        // ─────────────────────────────────────────────────────────────────────────
        internal void Initialize(GameStats gameStats)
        {
            _gameStats = gameStats;
            _mainCamera = Camera.main;
            _characterController = GetComponent<CharacterController>();

            BuildLaserMaterial();
            CreateEdges();
            CreateAimingCircle();
            CreateRings();
            CreateSweep();

            CurrentAngle = gameStats._precisionMax;
        }

        // Radar texture bakes Radar Line Count / Sharpness / Scanline Alpha — rebuild it live when
        // any of them is tweaked in the Inspector (only meaningful once Initialize built the material).
        private void OnValidate()
        {
            if (_radarMat != null) RebuildRadarTexture();
        }

        private void RebuildRadarTexture()
        {
            if (_radarTex != null) Destroy(_radarTex);
            _radarTex = MakeRadarTexture(256, 256, _radarLineCount, _radarLineSharpness);
            _radarMat.mainTexture = _radarTex;
        }

        // ── Public API ───────────────────────────────────────────────────────────

        public void UpdateVisuals(bool isAiming, Vector2 movementInput)
        {
            if (isAiming)
            {
                SetEdgesActive(true);

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

                UpdateLockState();
                UpdateLinePoints(CurrentAngle);
                AnimateEdge(LineRendererLeft);
                AnimateEdge(LineRendererRight);
            }
            else
            {
                SetEdgesActive(false);
                CurrentAngle = _gameStats._precisionStartingAim;
                _isLocked = false;
                _lockPulseT = 0f;
            }

            RefreshEdgeWidth();
        }

        public void UpdateAimingCircle(bool isAiming, Quaternion playerRotation)
        {
            MoveAimingCircle();
            _gameObjectAimingCircle.transform.rotation = playerRotation;

            if (isAiming)
            {
                if (!_meshRendererAimingCircle.enabled) _meshRendererAimingCircle.enabled = true;
                GenerateMesh(CurrentAngle);
                UpdateRings();
                UpdateSweep();
            }
            else
            {
                if (_meshRendererAimingCircle.enabled) _meshRendererAimingCircle.enabled = false;
                SetRingsActive(false);
                SetSweepActive(false);
            }
        }

        public void ClearAimingCircleOutlines() => _aimingCircleTrigger?.ClearAllOutlinedZombies();

        public void DisableMeshRenderer()
        {
            if (_meshRendererAimingCircle != null && _meshRendererAimingCircle.enabled)
                _meshRendererAimingCircle.enabled = false;
            SetEdgesActive(false);
            SetRingsActive(false);
            SetSweepActive(false);
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

        // ── Theme / lock ──────────────────────────────────────────────────────────

        private float FocusProgress()
            => Mathf.InverseLerp(_gameStats._precisionMax, _gameStats._precisionMin, CurrentAngle);

        private Color ThemeColor() => AimPrecisionColors.GetOutlineColor(CurrentHitChance, IsPointBlank);

        private void UpdateLockState()
        {
            bool lockedNow = FocusProgress() >= _lockThreshold;
            if (lockedNow && !_isLocked) { _lockPulseT = 1f; _edgePulsePos = 0f; } // entered lock
            _isLocked = lockedNow;
            if (_lockPulseT > 0f)
                _lockPulseT = Mathf.Max(0f, _lockPulseT - Time.deltaTime / _lockPulseDuration);

            // Travelling edge pulse advances only while locked; loops player -> tip.
            if (_isLocked)
            {
                _edgePulsePos += Time.deltaTime * _edgePulseTravelSpeed;
                if (_edgePulsePos > 1f) _edgePulsePos -= 1f;
            }
            else _edgePulsePos = 0f;
        }

        // ── Edges ──────────────────────────────────────────────────────────────────

        private void CreateEdges()
        {
            LineRendererLeft = NewLine("EdgeLeft", _laserMat, 5000);
            LineRendererRight = NewLine("EdgeRight", _laserMat, 5000);
            SetEdgesActive(false);
        }

        private void SetEdgesActive(bool on)
        {
            if (LineRendererLeft != null) LineRendererLeft.enabled = on;
            if (LineRendererRight != null) LineRendererRight.enabled = on;
        }

        private void UpdateLinePoints(float angle)
        {
            float radius = ComputeVLength();
            float halfAngle = angle / 2f;
            float yOffset = 0.03f;

            Vector3 p = transform.position;
            Quaternion rot = transform.rotation;
            Vector3 baseP = new Vector3(p.x, _gameObjectAimingCircle.transform.position.y + yOffset, p.z);

            Vector3 left = baseP + rot * Quaternion.Euler(0, -halfAngle, 0) * Vector3.forward * radius;
            Vector3 right = baseP + rot * Quaternion.Euler(0, halfAngle, 0) * Vector3.forward * radius;

            LineRendererLeft.positionCount = 2;
            LineRendererLeft.SetPositions(new[] { baseP, left });
            LineRendererRight.positionCount = 2;
            LineRendererRight.SetPositions(new[] { baseP, right });
        }

        private void AnimateEdge(LineRenderer lr)
        {
            if (lr.positionCount < 2) return;
            float t = Time.time;
            float focus = FocusProgress();
            Color theme = ThemeColor();

            lr.colorGradient = BuildEdgeGradient(theme);

            // Scroll ramps with focus, but snaps to ZERO at full lock (beam goes still; the
            // travelling pulse in the gradient takes over as the "fully aimed" signal).
            float lineLen = Vector3.Distance(lr.GetPosition(0), lr.GetPosition(1));
            float dashTiles = Mathf.Lerp(lineLen * 3f, lineLen * 0.6f, focus); // many tiles=dashy -> few=solid
            float scroll = _isLocked ? 0f : Mathf.Lerp(scrollSpeed, scrollSpeed * 5f, focus);
            _laserMat.mainTextureScale = new Vector2(_isLocked ? lineLen * 0.5f : dashTiles, 1f);
            if (!_isLocked) _laserMat.mainTextureOffset = new Vector2(-t * scroll, 0f);

            // width: jittery wide when loose, tight when focused
            float widthMul = Mathf.Lerp(1.4f, 0.6f, focus);
            float pulse = focus < 0.2f
                ? Mathf.Lerp(0.6f, 1.4f, 0.5f + 0.5f * Mathf.Sin(t * 45f)) * (0.9f + Random.value * 0.2f)
                : 1f + Mathf.Sin(t * pulseSpeed) * 0.05f;
            lr.widthMultiplier = widthMul * pulse;

            // emission: theme glow + lock pulse pop
            float e = emissionBase * (0.85f + 0.15f * Mathf.Sin(t * pulseSpeed * 1.5f) * emissionPulse);
            e *= 1f + _lockPulseT * _lockPulseStrength;
            if (_isLocked) e *= 1.4f;
            _laserMat.SetColor("_EmissionColor", theme * e);
            _laserMat.SetColor("_BaseColor", theme);
            _laserMat.SetColor("_Color", theme);
        }

        // Not locked: apex faint -> tip. Locked: solid beam with a bright pulse band travelling along
        // it (player -> tip), sampled into 8 alpha keys so the band reads as a moving wave.
        private Gradient BuildEdgeGradient(Color theme)
        {
            var g = new Gradient();
            var ck = new[] { new GradientColorKey(theme, 0f), new GradientColorKey(theme, 1f) };

            GradientAlphaKey[] ak;
            if (!_isLocked)
            {
                ak = new[] {
                    new GradientAlphaKey(0.05f, 0f), new GradientAlphaKey(1f, 0.1f),
                    new GradientAlphaKey(1f, 0.95f), new GradientAlphaKey(0f, 1f) };
            }
            else
            {
                ak = new GradientAlphaKey[8];
                for (int i = 0; i < 8; i++)
                {
                    float tt = i / 7f;
                    float baseA = (tt < 0.05f || tt > 0.97f) ? 0f : 0.85f;
                    float d = (tt - _edgePulsePos) / Mathf.Max(0.01f, _edgePulseWidth);
                    float bump = Mathf.Exp(-d * d);                 // travelling bright band
                    ak[i] = new GradientAlphaKey(Mathf.Clamp01(Mathf.Max(baseA, bump)), tt);
                }
            }
            g.SetKeys(ck, ak);
            return g;
        }

        private void RefreshEdgeWidth()
        {
            if (!LineRendererLeft || !LineRendererRight || _mainCamera == null) return;
            Vector3 basePoint = LineRendererLeft.positionCount > 0 ? LineRendererLeft.GetPosition(0) : transform.position;
            float dist = Vector3.Distance(_mainCamera.transform.position, basePoint);
            float worldWidth = aimLinePixels > 0 ? WorldWidthForPixels(_mainCamera, dist, aimLinePixels) : baseWidth;
            worldWidth = Mathf.Clamp(worldWidth, minWorldWidth, maxWorldWidth);
            LineRendererLeft.startWidth = LineRendererLeft.endWidth = worldWidth;
            LineRendererRight.startWidth = LineRendererRight.endWidth = worldWidth;
        }

        private float WorldWidthForPixels(Camera cam, float distance, int pixels)
        {
            if (cam.orthographic)
                return pixels * (2f * cam.orthographicSize) / Screen.height;
            float worldHeight = 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            return pixels * (worldHeight / Screen.height);
        }

        // ── Range rings ───────────────────────────────────────────────────────────

        private void CreateRings()
        {
            _ringMat = AdditiveMat();
            _rings = new LineRenderer[3];
            for (int i = 0; i < 3; i++)
            {
                _rings[i] = NewLine("Ring" + i, _ringMat, 4990);
                _rings[i].widthMultiplier = _ringWidth;
                _rings[i].numCornerVertices = 2;
                _rings[i].enabled = false;
            }
        }

        private void SetRingsActive(bool on)
        {
            if (_rings == null) return;
            foreach (var r in _rings) if (r != null) r.enabled = on && _showRings;
        }

        private void UpdateRings()
        {
            if (!_showRings || _rings == null) { SetRingsActive(false); return; }

            var cfg = WeaponConfigSingleton.Instance?.WeaponConfig;
            float pb = cfg != null ? cfg.PointBlankRange : 2f;
            float opt = cfg != null ? cfg.OptimalRange : 6f;
            float max = cfg != null ? cfg.MaxEffectiveRange : ComputeVLength();
            float vLen = ComputeVLength();

            BuildRingArc(_rings[0], pb, vLen, _ringPointBlank);
            BuildRingArc(_rings[1], opt, vLen, _ringOptimal);
            BuildRingArc(_rings[2], max, vLen, _ringMax);
        }

        private void BuildRingArc(LineRenderer ring, float radius, float vLen, Color color)
        {
            if (radius <= 0.01f || radius > vLen + 0.01f) { ring.enabled = false; return; }
            ring.enabled = true;

            float half = CurrentAngle / 2f;
            Quaternion rot = transform.rotation;
            Vector3 baseP = new Vector3(transform.position.x,
                _gameObjectAimingCircle.transform.position.y + 0.02f, transform.position.z);

            ring.positionCount = _ringSegments + 1;
            for (int i = 0; i <= _ringSegments; i++)
            {
                float a = Mathf.Lerp(-half, half, i / (float)_ringSegments);
                ring.SetPosition(i, baseP + rot * Quaternion.Euler(0, a, 0) * Vector3.forward * radius);
            }
            Color c = color; c.a = _ringAlpha;
            ring.startColor = ring.endColor = c;
            ring.widthMultiplier = _ringWidth;
        }

        // ── Sonar sweep + blip ──────────────────────────────────────────────────────

        private void CreateSweep()
        {
            _sweepMat = AdditiveMat();
            int n = Mathf.Max(1, _sweepTrailCount + 1);
            _sweepLines = new LineRenderer[n];
            for (int i = 0; i < n; i++)
            {
                _sweepLines[i] = NewLine("Sweep" + i, _sweepMat, 4995);
                _sweepLines[i].widthMultiplier = _sweepWidth;
                _sweepLines[i].enabled = false;
            }
            _prevSweepAngleDeg = 0f;
        }

        private void SetSweepActive(bool on)
        {
            if (_sweepLines == null) return;
            foreach (var s in _sweepLines) if (s != null) s.enabled = on && _showSweep;
        }

        private void RebuildSweep()
        {
            if (_sweepLines != null)
                foreach (var s in _sweepLines)
                    if (s != null) Destroy(s.gameObject);
            CreateSweep();
        }

        private void UpdateSweep()
        {
            if (!_showSweep) { SetSweepActive(false); return; }

            // Self-heal when Sweep Trail Count is changed in the Inspector at runtime.
            int want = Mathf.Max(1, _sweepTrailCount + 1);
            if (_sweepLines == null || _sweepLines.Length != want) RebuildSweep();

            float half = CurrentAngle / 2f;
            _sweepPhase += Time.deltaTime * _sweepCyclesPerSecond * 2f; // 0..1..0 ping-pong
            float tri = Mathf.PingPong(_sweepPhase, 1f);
            float headAngle = Mathf.Lerp(-half, half, tri);
            int dir = (Mathf.Repeat(_sweepPhase, 2f) < 1f) ? 1 : -1; // sweeping toward +half or -half

            Color theme = ThemeColor();
            float vLen = ComputeVLength();
            Quaternion rot = transform.rotation;
            Vector3 baseP = new Vector3(transform.position.x,
                _gameObjectAimingCircle.transform.position.y + 0.025f, transform.position.z);

            for (int i = 0; i < _sweepLines.Length; i++)
            {
                var s = _sweepLines[i];
                s.enabled = true;
                float a = headAngle - dir * i * _sweepTrailAngleStep;
                a = Mathf.Clamp(a, -half, half);
                Vector3 tip = baseP + rot * Quaternion.Euler(0, a, 0) * Vector3.forward * vLen;
                s.positionCount = 2;
                s.SetPositions(new[] { baseP, tip });
                Color c = theme;
                c.a = (i == 0) ? 1f : Mathf.Lerp(0.6f, 0f, i / (float)_sweepLines.Length);
                s.startColor = s.endColor = c;
                s.widthMultiplier = _sweepWidth * (i == 0 ? 1f : 0.7f);
            }

            BlipPassedZombies(_prevSweepAngleDeg, headAngle, half);
            _prevSweepAngleDeg = headAngle;
        }

        // Flash any in-cone zombie the sweep head crossed between last frame and this one.
        private void BlipPassedZombies(float prevAngle, float nowAngle, float halfAngle)
        {
            var inside = _aimingCircleTrigger?.GetZombiesInside();
            if (inside == null || inside.Count == 0) return;

            float lo = Mathf.Min(prevAngle, nowAngle) - _blipAngleTolerance;
            float hi = Mathf.Max(prevAngle, nowAngle) + _blipAngleTolerance;
            Vector3 fwd = transform.forward; fwd.y = 0f;

            foreach (var col in inside)
            {
                if (col == null) continue;
                Vector3 to = col.bounds.center - transform.position; to.y = 0f;
                if (to.sqrMagnitude < 1e-4f) continue;
                float bearing = Vector3.SignedAngle(fwd, to, Vector3.up);
                if (bearing < lo || bearing > hi) continue;
                col.GetComponent<Enemy>()?.BlipOutline();
            }
        }

        // ── Cone fill mesh (kept) ────────────────────────────────────────────────────

        private void CreateAimingCircle()
        {
            _gameObjectAimingCircle = new GameObject("AnnularSector");
            _meshFilterAimingCircle = _gameObjectAimingCircle.AddComponent<MeshFilter>();
            _meshRendererAimingCircle = _gameObjectAimingCircle.AddComponent<MeshRenderer>();
            _meshColliderAimingCircle = _gameObjectAimingCircle.AddComponent<MeshCollider>();
            _meshColliderAimingCircle.convex = true;
            _meshColliderAimingCircle.isTrigger = true;

            _radarMat = AdditiveMat();
            _radarTex = MakeRadarTexture(256, 256, _radarLineCount, _radarLineSharpness);
            _radarMat.mainTexture = _radarTex;
            _meshRendererAimingCircle.material = _radarMat;

            _meshAimingCircle = new Mesh { name = "AnnularSector" };
            _meshFilterAimingCircle.mesh = _meshAimingCircle;
            _meshColliderAimingCircle.sharedMesh = _meshAimingCircle;

            Rigidbody rb = _gameObjectAimingCircle.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            _meshRendererAimingCircle.enabled = true;
            _aimingCircleTrigger = _gameObjectAimingCircle.AddComponent<AimingCircleTrigger>();
        }

        private void MoveAimingCircle()
        {
            _gameObjectAimingCircle.transform.position = new Vector3(
                _characterController.transform.position.x,
                _characterController.transform.position.y - _characterController.height / 2f + _aimingCircleHeight,
                _characterController.transform.position.z);
        }

        private float ComputeVLength()
            => WeaponConfigSingleton.Instance?.WeaponConfig?.MaxEffectiveRange ?? _defaultVLength;

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

                vertices[i * 2] = Vector3.zero;
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

            Color theme = ThemeColor();
            Color[] colors = SweepFillColors(vertexCount, _resolution, theme);
            _meshAimingCircle.Clear();
            _meshAimingCircle.vertices = vertices;
            _meshAimingCircle.uv = uvs;
            _meshAimingCircle.triangles = triangles;
            _meshAimingCircle.RecalculateNormals();
            _meshAimingCircle.colors = colors;
            _meshColliderAimingCircle.sharedMesh = null;
            _meshColliderAimingCircle.sharedMesh = _meshAimingCircle;

            _aimingCircleTrigger?.SetConeShape(angle, vLength + _pointyTipFactor);

            // scroll the scanline; brighten the whole fill on a fresh lock
            _radarTimeTranslation -= Time.deltaTime * 0.3f;
            _radarTimeTranslation %= 1f;
            float fillBoost = 1f + _lockPulseT * 1.5f;
            Color baseCol = theme * fillBoost; baseCol.a = Mathf.Lerp(0.45f, 0.7f, FocusProgress());
            _radarMat.color = baseCol;
            if (_radarMat.HasProperty("_BaseColor")) _radarMat.SetColor("_BaseColor", baseCol);
        }

        private Color[] SweepFillColors(int vertexCount, int resolution, Color theme)
        {
            Color[] colors = new Color[vertexCount];
            for (int i = 0; i <= resolution; i++)
            {
                float horizontalT = (float)i / resolution;
                float edge = Mathf.Pow(Mathf.Abs(horizontalT - 0.5f) * 2f, 4f);
                float baseAlpha = Mathf.Lerp(0.02f, 0.75f, edge);
                Color c = theme;
                c.a = baseAlpha * 0.1f; colors[i * 2] = c;
                c.a = baseAlpha;        colors[i * 2 + 1] = c;
            }
            return colors;
        }

        private Texture2D MakeRadarTexture(int width, int height, int lineCount, float sharpness)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };

            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;
                float verticalGrid = Mathf.Pow(Mathf.Sin(u * Mathf.PI * 6f), 40f);
                for (int y = 0; y < height; y++)
                {
                    float v = (float)y / height;
                    float fract = (v * lineCount) % 1.0f;
                    float bands = Mathf.Pow(fract, sharpness);
                    float combined = Mathf.Max(bands, verticalGrid * 0.4f) * _scanlineAlpha + bands * (1f - _scanlineAlpha);
                    Color c = Color.white; c.a = combined;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        // ── Shared builders ──────────────────────────────────────────────────────────

        private void BuildLaserMaterial()
        {
            _laserTex = MakeLaserTexture(128, 32, 1.8f);
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Additive");
            _laserMat = new Material(sh) { mainTexture = _laserTex };
            _laserMat.EnableKeyword("_EMISSION");
            _laserMat.SetInt("_ZWrite", 0);
            _laserMat.SetInt("_ZTest", (int)CompareFunction.Always);
            _laserMat.renderQueue = 3000;
        }

        private Material AdditiveMat()
        {
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Particles/Additive") ?? Shader.Find("Sprites/Default");
            var m = new Material(sh);
            if (sh.name.Contains("Universal Render Pipeline"))
            {
                m.SetFloat("_Surface", 1);
                m.SetFloat("_Blend", 1);
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.One);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.EnableKeyword("_ADDITIVE_BLENDING");
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            return m;
        }

        private LineRenderer NewLine(string name, Material mat, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.parent = transform;
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 2;
            lr.alignment = LineAlignment.View;
            lr.textureMode = LineTextureMode.Tile;
            lr.material = mat;
            lr.sortingOrder = sortingOrder;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        private Texture2D MakeLaserTexture(int width, int height, float sharpness)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                float d = Mathf.Abs(v - 0.5f) * 2f;
                float core = Mathf.Exp(-Mathf.Pow(d * sharpness * 3.2f, 2f));
                float glow = Mathf.Exp(-Mathf.Pow(d * sharpness * 1.0f, 2f)) * 0.55f;
                float alpha = Mathf.Clamp01(core + glow);
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    // dash pattern along length (gaps), so high tiling reads as dashes, low as solid
                    float dash = Mathf.SmoothStep(0.35f, 0.5f, Mathf.Abs(((u * 2f) % 1f) - 0.5f));
                    Color c = Color.white; c.a = alpha * Mathf.Lerp(0.85f, 1f, dash);
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply(false, false);
            return tex;
        }
    }
}
