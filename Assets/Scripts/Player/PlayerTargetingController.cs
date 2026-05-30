using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static StickDirectionAnalyzer;

namespace ZombieGame
{
    /// <summary>
    /// Manages player rotation (mouse / controller right-stick) and zombie target selection.
    /// Also owns the IsAiming state so it can be shared cleanly with other components.
    /// </summary>
    public class PlayerTargetingController : MonoBehaviour
    {
        // ── Public state ──────────────────────────────────────────────────────
        public bool IsAiming { get; set; }
        public AimingType CurrentAimingType { get; private set; }
        public Vector2 AimControllerInput { get; private set; }
        public Vector3 MouseGroundPoint => _lastRaycastHit.point;
        public GameObject CurrentTarget => _currentTarget;

        // ── Private state ─────────────────────────────────────────────────────
        private Camera _mainCamera;
        private float _rotationSpeed = 500f;
        private RaycastHit _lastRaycastHit;

        private GameObject _currentTarget;
        private GameObject _nextTarget;
        private GameObject[] _onScreenZombies = Array.Empty<GameObject>();

        private bool _aimLeftTrigger;
        private bool _aimRightStick;

        private List<StickInputSample> _stickInputBuffer = new List<StickInputSample>();
        private float _inputBufferDuration = 0.5f;
        private float _lastStickSwitchTime = -Mathf.Infinity;
        private RotationCW _stickDirection;

        public StickDirectionAnalyzer StickAnalyzer { get; private set; }

        // ── Init ──────────────────────────────────────────────────────────────
        public void Initialize()
        {
            _mainCamera = Camera.main;
            StickAnalyzer = new StickDirectionAnalyzer();
        }

        // ── Tick ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Refreshes on-screen zombie list and applies rotation. Call from Update.
        /// </summary>
        public void Tick()
        {
            _onScreenZombies = GameObject.FindGameObjectsWithTag("Zombie");
        }

        /// <summary>
        /// Computes and applies player rotation. Returns the new rotation.
        /// </summary>
        public Quaternion ComputeAndApplyRotation(Vector2 movementInput, Vector3 movementDirection)
        {
            if (!IsAiming)
            {
                if (movementInput.sqrMagnitude == 0) return transform.rotation;
                Vector3 dir = movementDirection;
                dir.y = 0f;
                Quaternion toRot = Quaternion.LookRotation(dir, Vector3.up);
                return Quaternion.RotateTowards(transform.rotation, toRot, _rotationSpeed * Time.deltaTime);
            }

            return ProcessRotation();
        }

        // ── Input callbacks ───────────────────────────────────────────────────

        public void OnAimMouse(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                CurrentAimingType = AimingType.Mouse;
                IsAiming = true;
            }
            else if (context.canceled)
            {
                IsAiming = false;
            }
        }

        public void OnAimControllerRightStick(InputAction.CallbackContext context)
        {
            Vector2 currentInput = context.ReadValue<Vector2>();

            if (currentInput.magnitude > 0.1f)
            {
                IsAiming = true;
                CurrentAimingType = AimingType.ControllerRightStick;
                AimControllerInput = currentInput;
            }

            if (IsAiming)
            {
                _stickInputBuffer.Add(new StickInputSample(currentInput, Time.time));
                _stickInputBuffer.RemoveAll(sample => Time.time - sample.time > _inputBufferDuration);
            }

            if (currentInput.magnitude <= 0.1f && !_aimLeftTrigger)
            {
                IsAiming = false;
                _currentTarget = null;
            }

            if (currentInput.magnitude <= 0.1f)
                _aimRightStick = false;

            if (_currentTarget == null && IsAiming)
                _currentTarget = GetClosestZombie();

            _stickDirection = AnalyzeFlickDirection(transform.forward);
            if (_stickDirection != RotationCW.NONE) SetNextTarget();

            _stickDirection = AnalyzeStickRotation45(transform.forward);
            if (_stickDirection != RotationCW.NONE) SetNextTarget();
        }

        public void OnAimControllerTrigger(bool performed, bool canceled)
        {
            if (performed)
            {
                IsAiming = true;
                _aimLeftTrigger = true;
            }
            if (canceled)
            {
                _aimLeftTrigger = false;
            }
            if (canceled && !_aimRightStick)
            {
                IsAiming = false;
                _currentTarget = null;
            }
        }

        // ── GUI / Gizmo helpers (debug) ───────────────────────────────────────

        public int DrawDebugGUI()
        {
            int y = 10;
            GUI.Label(new Rect(10, y, 300, 20), "_current " + (_currentTarget != null ? _currentTarget.name : "null")); y += 20;
            GUI.Label(new Rect(10, y, 300, 20), "_next " + (_nextTarget != null ? _nextTarget.name : "null")); y += 20;
            GUI.Label(new Rect(10, y, 300, 20), "rotation:" + _stickDirection); y += 20;

            if (_onScreenZombies != null)
            {
                foreach (GameObject zombie in _onScreenZombies)
                {
                    if (zombie == null) continue;
                    string text = $"Zombie: {zombie.name}, Angle: {GetZombieAngle(zombie):F1}°";
                    GUI.Label(new Rect(10, y, 600, 20), text);
                    y += 20;
                }
            }
            GUI.Label(new Rect(10, y, 300, 20), "Raycast hit:" + _lastRaycastHit.ToString());
            y += 20;
            return y;
        }

        // ── Private rotation logic ────────────────────────────────────────────

        private Quaternion ProcessRotation()
        {
            Quaternion result = transform.rotation;

            if (CurrentAimingType == AimingType.Mouse)
                return MouseRotation();

            if (CurrentAimingType == AimingType.ControllerRightStick)
                return ControllerRotation();

            return result;
        }

        private Quaternion ControllerRotation()
        {
            try
            {
                Vector3 dir = new Vector3(AimControllerInput.x, 0, AimControllerInput.y);
                if (dir.sqrMagnitude < 0.01f) return transform.rotation;
                dir.Normalize();

                if (_currentTarget == null && _nextTarget == null)
                {
                    _nextTarget = GetClosestZombie();
                    return _nextTarget != null
                        ? RotateTowardsGameObject(_nextTarget)
                        : RotateTowardsDirection(dir);
                }

                GameObject target = _nextTarget ?? _currentTarget;
                return RotateTowardsGameObject(target);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error in ControllerRotation: " + ex.Message);
                return Quaternion.identity;
            }
        }

        private Quaternion MouseRotation()
        {
            if (_mainCamera == null) return transform.rotation;

            var ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            int groundMask = LayerMask.GetMask("Ground");

            if (groundMask != 0 && Physics.Raycast(ray, out _lastRaycastHit, 1000f, groundMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 target = _lastRaycastHit.point;
                target.y = transform.position.y;
                Vector3 dir = target - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) return transform.rotation;
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                return Quaternion.RotateTowards(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
            }

            var plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 target = ray.GetPoint(enter);
                Vector3 dir = target - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) return transform.rotation;
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
                return Quaternion.RotateTowards(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
            }

            return transform.rotation;
        }

        private Quaternion RotateTowardsGameObject(GameObject zombie)
        {
            if (zombie == null) return transform.rotation;

            Vector3 dir = zombie.transform.position - transform.position;
            dir.y = 0f;
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            float angle = Quaternion.Angle(transform.rotation, targetRot);

            if (angle < 0.5f && _currentTarget != _nextTarget && _nextTarget != null)
            {
                _currentTarget = _nextTarget;
                _nextTarget = null;
            }

            return Quaternion.Slerp(transform.rotation, targetRot, 7f * Time.deltaTime);
        }

        private Quaternion RotateTowardsDirection(Vector3 direction)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            return Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }

        // ── Target selection ──────────────────────────────────────────────────

        private GameObject GetClosestZombie(float range = 2000f, float maxAngle = 120f)
        {
            try
            {
                GameObject closest = null;
                float closestDist = Mathf.Infinity;

                foreach (GameObject zombie in _onScreenZombies)
                {
                    if (!ZombieInRange(zombie.transform.position, range)) continue;
                    if (!ZombieInAimingAngle(zombie.transform.position, maxAngle)) continue;
                    if (!ZombieInClearLineOfSight(zombie.transform.position, zombie)) continue;

                    float dist = (zombie.transform.position - transform.position).magnitude;
                    if (dist < closestDist) { closestDist = dist; closest = zombie; }
                }

                if (closest != null) _currentTarget = closest;
                return closest;
            }
            catch (Exception e)
            {
                Debug.LogError("Error in GetClosestZombie: " + e.Message);
                return null;
            }
        }

        private void SetNextTarget()
        {
            if (_stickDirection == RotationCW.NONE) return;

            bool clockwise = _stickDirection == RotationCW.CW;
            GameObject newTarget = GetNextTarget(clockwise);
            if (newTarget != null && newTarget != _currentTarget)
            {
                _nextTarget = newTarget;
                _lastStickSwitchTime = Time.time;
            }
        }

        private GameObject GetNextTarget(bool clockwise)
        {
            if (_onScreenZombies.Length <= 1 || _currentTarget == null) return null;

            List<Transform> others = new List<Transform>();
            foreach (GameObject z in _onScreenZombies)
            {
                if (z != null && z.name != _currentTarget.name)
                    others.Add(z.transform);
            }

            Vector3 aimDir = new Vector3(AimControllerInput.x, 0, AimControllerInput.y).normalized;
            if (aimDir == Vector3.zero) aimDir = transform.forward;

            Transform best = GetNextZombieInDirection(others, aimDir, clockwise ? RotationCW.CW : RotationCW.CCW);
            return best?.gameObject;
        }

        private Transform GetNextZombieInDirection(List<Transform> zombies, Vector3 aimDir, RotationCW dir)
        {
            try
            {
                Transform best = null;
                float bestAngle = dir == RotationCW.CW ? float.MaxValue : float.MinValue;

                foreach (Transform zombie in zombies)
                {
                    if (zombie == null || zombie.gameObject.name == _currentTarget?.name) continue;
                    float angle = GetZombieAngle(zombie.gameObject);

                    if (dir == RotationCW.CW && angle < bestAngle) { bestAngle = angle; best = zombie; }
                    else if (dir == RotationCW.CCW && angle > bestAngle) { bestAngle = angle; best = zombie; }
                }
                return best;
            }
            catch (Exception ex)
            {
                Debug.LogError("Error in GetNextZombieInDirection: " + ex.Message);
                return null;
            }
        }

        // ── Stick analysis ────────────────────────────────────────────────────

        private RotationCW AnalyzeFlickDirection(Vector3 playerForward)
        {
            if (_stickInputBuffer.Count < 2) return RotationCW.NONE;

            StickInputSample? neutralSample = null;
            StickInputSample? strongSample = null;

            foreach (var s in _stickInputBuffer)
            {
                if (neutralSample == null && s.input.magnitude < 0.2f) neutralSample = s;
                if (s.input.magnitude >= 0.7f) strongSample = s;
            }

            if (neutralSample != null && strongSample != null && strongSample.Value.time > neutralSample.Value.time)
            {
                Vector2 inputDir = strongSample.Value.input.normalized;
                Vector3 flickDir = new Vector3(inputDir.x, 0, inputDir.y);
                float angle = GetAngleBetween(playerForward, flickDir);
                _stickInputBuffer.Clear();

                if (angle < 15f || angle > 345f) return RotationCW.NONE;
                return angle <= 180f ? RotationCW.CCW : RotationCW.CW;
            }
            return RotationCW.NONE;
        }

        private RotationCW AnalyzeStickRotation45(Vector3 playerForward)
        {
            if (_stickInputBuffer.Count < 2) return RotationCW.NONE;

            StickInputSample? startSample = null;
            float startAngle = 0f;

            foreach (var sample in _stickInputBuffer)
            {
                if (sample.input.magnitude >= 0.7f)
                {
                    Vector3 dir = new Vector3(sample.input.x, 0f, sample.input.y).normalized;
                    float angle = GetAngleBetween(playerForward, dir);

                    if (startSample == null) { startSample = sample; startAngle = angle; }
                    else
                    {
                        float delta = Mathf.DeltaAngle(startAngle, angle);
                        if (Mathf.Abs(delta) >= 45f)
                        {
                            _stickInputBuffer.Clear();
                            return delta > 0 ? RotationCW.CCW : RotationCW.CW;
                        }
                    }
                }
            }
            return RotationCW.NONE;
        }

        // ── Angle helpers ─────────────────────────────────────────────────────

        public float GetZombieAngle(GameObject zombie)
        {
            Vector3 forward = transform.forward; forward.y = 0; forward.Normalize();
            Vector3 toZombie = zombie.transform.position - transform.position; toZombie.y = 0; toZombie.Normalize();
            float angle = Vector3.Angle(forward, toZombie);
            bool isCW = Vector3.Cross(forward, toZombie).y < 0;
            return isCW ? 360f - angle : angle;
        }

        private float GetAngleBetween(Vector3 forward, Vector3 flickDirection)
        {
            forward.y = 0; forward.Normalize();
            flickDirection.y = 0; flickDirection.Normalize();
            float angle = Vector3.Angle(forward, flickDirection);
            bool isCW = Vector3.Cross(forward, flickDirection).y > 0;
            return isCW ? 360f - angle : angle;
        }

        private bool ZombieInAimingAngle(Vector3 zombiePosition, float maxAngle)
        {
            Vector3 direction = zombiePosition - transform.position;
            return Vector3.Angle(transform.forward, direction) <= maxAngle;
        }

        private bool ZombieInRange(Vector3 zombiePosition, float range)
        {
            return (zombiePosition - transform.position).magnitude <= range;
        }

        private bool ZombieInClearLineOfSight(Vector3 targetPosition, GameObject target)
        {
            Transform playerHead = GetWeaponPositionApprox();
            Transform zombieHead = FindBone(target, "mixamorig:Head");
            if (playerHead == null || zombieHead == null) return false;

            Vector3 dir = zombieHead.position - playerHead.position;
            Vector3 rayStart = playerHead.position + dir * 0.2f;
            float rayDist = Vector3.Distance(rayStart, zombieHead.position);

            Debug.DrawRay(rayStart, dir * rayDist, Color.yellow, 0.3f);
            if (Physics.Raycast(rayStart, dir, out RaycastHit hit, rayDist))
                return hit.collider.gameObject.CompareTag("Zombie");

            return false;
        }

        private Transform FindBone(GameObject obj, string boneName)
        {
            return FindRecursive(obj.transform, boneName);
        }

        private Transform FindRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                Transform result = FindRecursive(child, name);
                if (result != null) return result;
            }
            return null;
        }

        /// <summary>Approximate weapon/head position for line-of-sight checks.</summary>
        private Transform GetWeaponPositionApprox()
        {
            return FindRecursive(transform, "Weapon") ?? FindRecursive(transform, "Pistol(Clone)") ?? transform;
        }
    }
}
