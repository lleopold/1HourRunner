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

        // ── Laser ──────────────────────────────────────────────────────────────
        [SerializeField] private float baseWidth = 0.045f;
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
        private readonly float _aimingCircleHeight = 0.11f;

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

            CurrentAngle = gameStats._precisionMax;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        public void UpdateVisuals(bool isAiming, Vector2 movementInput)
        {
            if (isAiming)
            {
                SetAimLinesActive(true);

                bool isMoving = movementInput.magnitude > 0f;
                if (!isMoving)
                {
                    float recoverPerSecond = _gameStats._aimingSpeed;
                    CurrentAngle = Mathf.Max(_gameStats._precisionMin, CurrentAngle - recoverPerSecond * Time.deltaTime);
                }

                if (Recoil > 0f)
                {
                    CurrentAngle = Mathf.Max(CurrentAngle, Recoil);
                    Recoil = 0f;
                }

                CurrentAngle = Mathf.Clamp(CurrentAngle, _gameStats._precisionMin, _gameStats._precisionMax);

                UpdateLinePoints(CurrentAngle);
                ApplyVisuals(LineRendererLeft, _meshRendererAimingCircle, CurrentAngle);
                ApplyVisuals(LineRendererRight, _meshRendererAimingCircle, CurrentAngle);
                // TODO: precision shooting disabled — re-enable when returning to the feature
                // ApplyPrecisionVisuals(_lineRendererPrecisionLeft);
                // ApplyPrecisionVisuals(_lineRendererPrecisionRight);

                AnimateLaser(LineRendererLeft, _laserMat);
                AnimateLaser(LineRendererRight, _laserMat);
                // AnimatePrecisionZone(_lineRendererPrecisionLeft, _precisionLaserMat);
                // AnimatePrecisionZone(_lineRendererPrecisionRight, _precisionLaserMat);

                // if (!_lineRendererPrecisionLeft.enabled) _lineRendererPrecisionLeft.enabled = true;
                // if (!_lineRendererPrecisionRight.enabled) _lineRendererPrecisionRight.enabled = true;
            }
            else
            {
                SetAimLinesActive(false);
                CurrentAngle = _gameStats._precisionStartingAim;

                // if (_lineRendererPrecisionLeft != null && _lineRendererPrecisionLeft.enabled) _lineRendererPrecisionLeft.enabled = false;
                // if (_lineRendererPrecisionRight != null && _lineRendererPrecisionRight.enabled) _lineRendererPrecisionRight.enabled = false;

                if (_laserSmokeLeft.isPlaying) _laserSmokeLeft.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (_laserSmokeRight.isPlaying) _laserSmokeRight.Stop(true, ParticleSystemStopBehavior.StopEmitting);
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

        // TODO: precision shooting disabled — re-enable when returning to the feature
        public bool IsRadarInPrecisionZone()
        {
            // float sweepT = Mathf.PingPong(Time.time / (_sweepDuration / 2f), 1f);
            // if (_isPausedAtEdge) return false;
            // float distanceFromCenter = Mathf.Abs(sweepT - 0.5f);
            // float halfAngle = CurrentAngle / 2f;
            // float tolerance = _precisionZoneToleranceDegrees / halfAngle;
            // return distanceFromCenter <= tolerance;
            return false;
        }

        // ── Setup helpers ──────────────────────────────────────────────────────

        private void CreateLineRenderers()
        {
            _laserTex = MakeLaserTexture(64, 1.8f);

            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Particles/Additive");

            _laserMat = new Material(sh);
            _laserMat.mainTexture = _laserTex;
            _laserMat.SetColor("_BaseColor", laserColor);
            _laserMat.SetColor("_Color", laserColor);
            _laserMat.EnableKeyword("_EMISSION");
            _laserMat.SetColor("_EmissionColor", laserColor * emissionBase);

            // TODO: precision shooting disabled — re-enable when returning to the feature
            // _precisionLaserMat = new Material(sh);
            // _precisionLaserMat.mainTexture = _laserTex;
            // _precisionLaserMat.SetColor("_BaseColor", _precisionVColor);
            // _precisionLaserMat.SetColor("_Color", _precisionVColor);
            // _precisionLaserMat.EnableKeyword("_EMISSION");
            // _precisionLaserMat.SetColor("_EmissionColor", _precisionVColor * emissionBase);

            LineRendererLeft = CreateLineRendererChild("LeftLine");
            LineRendererRight = CreateLineRendererChild("RightLine");
            SetupLaser(LineRendererLeft);
            SetupLaser(LineRendererRight);
            EnsureLineRenderersOnTop();
            SetLineAlpha(LineRendererLeft, 0);
            SetLineAlpha(LineRendererRight, 0);

            // _lineRendererPrecisionLeft = CreateLineRendererChild("PrecisionLeftLine");
            // _lineRendererPrecisionRight = CreateLineRendererChild("PrecisionRightLine");
            // SetupPrecisionLaser(_lineRendererPrecisionLeft);
            // SetupPrecisionLaser(_lineRendererPrecisionRight);
            // _lineRendererPrecisionLeft.enabled = false;
            // _lineRendererPrecisionRight.enabled = false;
        }

        private LineRenderer CreateLineRendererChild(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.parent = transform;
            LineRenderer lr = go.AddComponent<LineRenderer>();
            ConfigureLineRenderer(lr);
            return lr;
        }

        private void CreateAimingCircle()
        {
            _gameObjectAimingCircle = new GameObject("AnnularSector");
            _meshFilterAimingCircle = _gameObjectAimingCircle.AddComponent<MeshFilter>();
            _meshRendererAimingCircle = _gameObjectAimingCircle.AddComponent<MeshRenderer>();
            _meshColliderAimingCircle = _gameObjectAimingCircle.AddComponent<MeshCollider>();
            _meshColliderAimingCircle.convex = true;
            _meshColliderAimingCircle.isTrigger = true;

            _meshRendererAimingCircle.material = new Material(Shader.Find("Sprites/Default"))
            {
                color = new Color(0, 1, 0, 0.5f)
            };

            _meshAimingCircle = new Mesh { name = "AnnularSector" };
            _meshFilterAimingCircle.mesh = _meshAimingCircle;
            _meshColliderAimingCircle.sharedMesh = _meshAimingCircle;

            _gameObjectAimingCircle.AddComponent<AimingCircleTrigger>();

            Rigidbody rb = _gameObjectAimingCircle.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            _meshRendererAimingCircle.enabled = true;
            _aimingCircleTrigger = _gameObjectAimingCircle.AddComponent<AimingCircleTrigger>();
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

        private void UpdateLinePoints(float angle)
        {
            float baseRadiusLocal = 2f;
            float lengthMultiplier = 5f;
            float currentRadius = baseRadiusLocal * lengthMultiplier;
            float halfAngle = angle / 2f;
            // float halfPrecisionAngle = _precisionVAngle / 2f;  // TODO: precision shooting disabled
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

            // TODO: precision shooting disabled — re-enable when returning to the feature
            // Vector3 precisionLeft = triangleBase + playerRotation * Quaternion.Euler(0, -halfPrecisionAngle, 0) * Vector3.forward * currentRadius;
            // Vector3 precisionRight = triangleBase + playerRotation * Quaternion.Euler(0, halfPrecisionAngle, 0) * Vector3.forward * currentRadius;
            // if (_lineRendererPrecisionLeft != null && _lineRendererPrecisionRight != null)
            // {
            //     _lineRendererPrecisionLeft.positionCount = 2;
            //     _lineRendererPrecisionLeft.SetPositions(new[] { triangleBase, precisionLeft });
            //     _lineRendererPrecisionRight.positionCount = 2;
            //     _lineRendererPrecisionRight.SetPositions(new[] { triangleBase, precisionRight });
            // }
        }

        private void ApplyVisuals(LineRenderer lineRenderer, MeshRenderer meshRenderer, float angle)
        {
            Color vColor = AimPrecisionColors.GetAnimatedColor(angle);
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(vColor, 0.35f),
                    new GradientColorKey(vColor, 0.65f),
                    new GradientColorKey(Color.white, 1f),
                },
                new[] {
                    new GradientAlphaKey(0.0f, 0f),
                    new GradientAlphaKey(1.0f, 0.08f),
                    new GradientAlphaKey(1.0f, 0.92f),
                    new GradientAlphaKey(0.0f, 1f),
                });
            lineRenderer.colorGradient = gradient;

            if (meshRenderer != null && meshRenderer.material != null)
            {
                Color newColor = GetInterpolatedColor(angle);
                Color transparentColor = newColor;
                transparentColor.a = 0.2f;
                meshRenderer.material.color = transparentColor;
                meshRenderer.material.SetFloat("_Mode", 3);
                meshRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                meshRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                meshRenderer.material.SetInt("_ZWrite", 0);
                meshRenderer.material.DisableKeyword("_ALPHATEST_ON");
                meshRenderer.material.EnableKeyword("_ALPHABLEND_ON");
                meshRenderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                meshRenderer.material.renderQueue = 3000;
                meshRenderer.material.EnableKeyword("_EMISSION");
                meshRenderer.material.SetColor("_EmissionColor", newColor * 2f);
            }
        }

        private void ApplyPrecisionVisuals(LineRenderer lineRenderer)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(_precisionVColor, 0.35f),
                    new GradientColorKey(_precisionVColor, 0.65f),
                    new GradientColorKey(Color.white, 1f),
                },
                new[] {
                    new GradientAlphaKey(0.0f, 0f),
                    new GradientAlphaKey(_precisionVAlpha, 0.08f),
                    new GradientAlphaKey(_precisionVAlpha, 0.92f),
                    new GradientAlphaKey(0.0f, 1f),
                });
            lineRenderer.colorGradient = gradient;
        }

        private void AnimateLaser(LineRenderer lr, Material mat)
        {
            float t = Time.time;
            Vector2 o = mat.mainTextureOffset;
            o.x = -t * scrollSpeed;
            mat.mainTextureOffset = o;

            float pulse = Mathf.Lerp(pulseMin, pulseMax, 0.5f + 0.5f * Mathf.Sin(t * pulseSpeed));
            lr.widthMultiplier = baseWidth * pulse;

            Color vColor = AimPrecisionColors.GetAnimatedColor(CurrentAngle);
            float e = emissionBase + Mathf.Sin(t * (pulseSpeed * 1.3f)) * emissionPulse;
            mat.SetColor("_BaseColor", vColor);
            mat.SetColor("_Color", vColor);
            mat.SetColor("_EmissionColor", vColor * e);
        }

        private void AnimatePrecisionZone(LineRenderer lr, Material mat)
        {
            float t = Time.time;
            Vector2 o = mat.mainTextureOffset;
            o.x = -t * scrollSpeed;
            mat.mainTextureOffset = o;

            float pulse = Mathf.Lerp(pulseMin, pulseMax, 0.5f + 0.5f * Mathf.Sin(t * pulseSpeed * 1.2f));
            lr.widthMultiplier = baseWidth * pulse * 0.7f;

            float e = emissionBase + Mathf.Sin(t * (pulseSpeed * 1.3f)) * emissionPulse;
            mat.SetColor("_EmissionColor", _precisionVColor * e);
        }

        private void RefreshAimLineWidth()
        {
            if (!LineRendererLeft || !LineRendererRight || _mainCamera == null) return;

            Vector3 basePoint = LineRendererLeft.positionCount > 0
                ? LineRendererLeft.GetPosition(0)
                : transform.position;

            float dist = Vector3.Distance(_mainCamera.transform.position, basePoint);
            float worldWidth = GetWorldWidthForPixels(_mainCamera, dist, aimLinePixels);
            worldWidth = Mathf.Clamp(worldWidth, minWorldWidth, maxWorldWidth);

            LineRendererLeft.startWidth = LineRendererLeft.endWidth = worldWidth;
            LineRendererRight.startWidth = LineRendererRight.endWidth = worldWidth;

            float precisionWidth = worldWidth * 0.7f;
            if (_lineRendererPrecisionLeft) _lineRendererPrecisionLeft.startWidth = _lineRendererPrecisionLeft.endWidth = precisionWidth;
            if (_lineRendererPrecisionRight) _lineRendererPrecisionRight.startWidth = _lineRendererPrecisionRight.endWidth = precisionWidth;
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
            int[] triangles = new int[_resolution * 6];
            float angleRad = Mathf.Deg2Rad * angle;
            float halfAngle = angleRad / 2f;

            for (int i = 0; i <= _resolution; i++)
            {
                float t = i / (float)_resolution;
                float currentAngle = -halfAngle + (t * angleRad);
                float normalizedCenterBias = Mathf.Cos(currentAngle / halfAngle * Mathf.PI / 2f);
                float dynamicOuterRadius = _outerRadius + _pointyTipFactor * normalizedCenterBias;
                vertices[i * 2] = new Vector3(0f, 0f, 0f);
                vertices[i * 2 + 1] = new Vector3(Mathf.Sin(currentAngle) * dynamicOuterRadius, 0f, Mathf.Cos(currentAngle) * dynamicOuterRadius);
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

            float sweepT = Mathf.PingPong(currentTime / (_sweepDuration / 2f), 1f);
            float previousSweepT = Mathf.PingPong((currentTime - Time.deltaTime) / (_sweepDuration / 2f), 1f);
            float sweepDir = sweepT - previousSweepT;
            bool isSweepingRight = sweepDir > 0;

            bool hitRightEdge = sweepT >= 1f;
            bool hitLeftEdge = sweepT <= 0f;

            if ((hitRightEdge || hitLeftEdge) && !_isPausedAtEdge)
            {
                _isPausedAtEdge = true;
                _lastSweepEndTime = currentTime;
                _pauseOnRightEdge = hitRightEdge;
            }

            if (_isPausedAtEdge)
            {
                float pauseTimeElapsed = currentTime - _lastSweepEndTime;
                if (pauseTimeElapsed < _sweepPauseDuration)
                    sweepT = _pauseOnRightEdge ? 1f : 0f;
                else
                    _isPausedAtEdge = false;
            }

            float sweepAngle = Mathf.Lerp(-halfAngle, halfAngle, sweepT);
            float lineHalfWidthDeg = angle * _sweepLineWidthPct / 2f;
            float trailMaxLengthDeg = angle * _sweepTrailWidthPct;

            for (int i = 0; i <= resolution; i++)
            {
                float t = i / (float)resolution;
                float vertexAngle = Mathf.Lerp(-halfAngle, halfAngle, t);
                float angleOffset = vertexAngle - sweepAngle;
                bool isBehind = isSweepingRight ? (vertexAngle < sweepAngle) : (vertexAngle > sweepAngle);

                float alpha;
                if (Mathf.Abs(angleOffset) <= lineHalfWidthDeg)
                    alpha = _sweepLineAlpha;
                else if (isBehind && Mathf.Abs(angleOffset) <= trailMaxLengthDeg)
                    alpha = Mathf.Lerp(_sweepLineAlpha, _sweepTrailAlpha, Mathf.InverseLerp(lineHalfWidthDeg, trailMaxLengthDeg, Mathf.Abs(angleOffset)));
                else
                    alpha = 0f;

                Color finalColor = new Color(_sweepColor.r, _sweepColor.g, _sweepColor.b, alpha);
                colors[i * 2] = finalColor;
                colors[i * 2 + 1] = finalColor;
            }
            return colors;
        }

        private Color GetInterpolatedColor(float angle)
        {
            float minAngle = _gameStats._precisionMin;
            float maxAngle = _gameStats._precisionMax;
            float normalizedValue = Mathf.InverseLerp(minAngle, maxAngle, angle);

            Color deepPurple = new Color(0.4f, 0f, 0.4f);
            Color darkRed = new Color(0.5f, 0f, 0f);
            Color burntOrange = new Color(0.6f, 0.25f, 0f);
            Color mutedYellow = new Color(0.5f, 0.4f, 0f);
            Color darkGreen = new Color(0f, 0.3f, 0f);

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
                    new GradientAlphaKey(0.0f, 0f),
                    new GradientAlphaKey(1.0f, 0.08f),
                    new GradientAlphaKey(1.0f, 0.92f),
                    new GradientAlphaKey(0.0f, 1f),
                });
            lr.colorGradient = g;
            lr.material = _laserMat;
        }

        private void SetupPrecisionLaser(LineRenderer lr)
        {
            lr.positionCount = 2;
            lr.widthMultiplier = baseWidth;
            lr.textureMode = LineTextureMode.Tile;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 2;
            lr.alignment = LineAlignment.View;
            var g = new Gradient();
            g.SetKeys(
                new[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(_precisionVColor, 0.35f),
                    new GradientColorKey(_precisionVColor, 0.65f),
                    new GradientColorKey(Color.white, 1f),
                },
                new[] {
                    new GradientAlphaKey(0.0f, 0f),
                    new GradientAlphaKey(_precisionVAlpha, 0.08f),
                    new GradientAlphaKey(_precisionVAlpha, 0.92f),
                    new GradientAlphaKey(0.0f, 1f),
                });
            lr.colorGradient = g;
            lr.material = _precisionLaserMat;
            lr.sortingOrder = 5001;
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

        private Texture2D MakeLaserTexture(int width, float sharpness)
        {
            var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            for (int x = 0; x < width; x++)
            {
                float u = (x + 0.5f) / width;
                float d = Mathf.Abs(u - 0.5f) * 2f;
                float a = Mathf.Exp(-Mathf.Pow(d * sharpness, 2f));
                Color c = Color.Lerp(laserColor, Color.white, Mathf.Pow(1f - d, 3f));
                c.a = a;
                tex.SetPixel(x, 0, c);
            }
            tex.Apply(false, false);
            return tex;
        }
    }
}
