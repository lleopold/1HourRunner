using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Assets.Scripts.Game;
using DamageNumbersPro;
using UnityEngine;
using static StickDirectionAnalyzer;

namespace ZombieGame
{
    /// <summary>
    /// Handles shooting, reload, recoil, muzzle flash, and bullet spawning.
    /// Shooting uses a probability roll system — bullets are cosmetic only.
    /// </summary>
    public class PlayerWeaponController : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────
        private const float SecondaryPenaltyMultiplier = 0.7f;
        private const float MissConeHalfAngle = 3f; // degrees

        // ── State ─────────────────────────────────────────────────────────────
        public bool IsShooting { get; set; }

        private float _nextFireTime;
        private int _bulletsInClip;
        private float _reloadTimeLeft;
        private bool _reloadingInProgress;
        private int _reloadingProgress;
        private float _recoilAngle;

        private static DamageNumber _damageNumberPrefab;
        private GameObject _bulletPrefab;
        private RigRecoilController _rigRecoilController;
        private ParticleSystem _muzzleSmoke; // cached — one persistent PS, Emit per shot (no Instantiate)

        // ── Dependencies (injected via Initialize) ────────────────────────────
        private UIT_GameScreen _uiGameScreen;
        private PlayerAimVisuals _aimVisuals;
        private GameStats _gameStats;
        private AimingCircleTrigger _aimingCircleTrigger;
        private PlayerTargetingController _targeting;

        // Reusable buffer for secondary shuffle — avoids per-shot allocation
        private readonly List<GameObject> _secondaryBuffer = new List<GameObject>();

        private float _precisionLogTimer;

        // ─────────────────────────────────────────────────────────────────────
        internal void Initialize(UIT_GameScreen uiGameScreen, PlayerAimVisuals aimVisuals, GameStats gameStats,
                                  AimingCircleTrigger aimingCircleTrigger, PlayerTargetingController targeting)
        {
            _uiGameScreen = uiGameScreen;
            _aimVisuals = aimVisuals;
            _gameStats = gameStats;
            _aimingCircleTrigger = aimingCircleTrigger;
            _targeting = targeting;
            _rigRecoilController = new RigRecoilController(this);
            _bulletPrefab = Resources.Load<GameObject>("Weapons/bullet_1");
            if (_bulletPrefab == null) Debug.LogError("Bullet prefab is null");
            _bulletsInClip = WeaponConfigSingleton.Instance.WeaponConfig.ClipSize;
            LoadDamageNumberPrefab();
        }

        private static void LoadDamageNumberPrefab()
        {
            if (_damageNumberPrefab == null)
            {
                _damageNumberPrefab = Resources.Load<DamageNumber>("DamageNumbers/DamageNumbers_2");
                if (_damageNumberPrefab == null)
                    Debug.LogError("DamageNumber prefab not found in Resources/DamageNumbers!");
            }
        }

        public static void CreateDamageNumber() => LoadDamageNumberPrefab();

        public void SpawnDamageNumber(Vector3 position, float damage)
        {
            _damageNumberPrefab?.Spawn(position, damage);
        }

        // ── Tick ──────────────────────────────────────────────────────────────

        public void Tick(bool isAiming)
        {
            try
            {
                if (WeaponConfigSingleton.Instance.WeaponConfig == null) return;
                _precisionLogTimer -= Time.deltaTime;
                if (_precisionLogTimer <= 0f)
                {
                    _precisionLogTimer = 0.5f;
                    float angle = _aimVisuals != null ? _aimVisuals.CurrentAngle : -1f;
                    int zombieCount = _aimingCircleTrigger != null ? _aimingCircleTrigger.GetZombiesInside().Count : -1;
                    //Debug.Log($"[AIM] CurrentAngle={angle:F2}° | IsAiming={isAiming} | ZombiesInGizmo={zombieCount}");
                }
                if (IsShooting && _bulletsInClip > 0 && isAiming)
                {
                    if (Time.time >= _nextFireTime)
                    {
                        FireOnce();
                    }
                }

                if (_bulletsInClip == 0 && !_reloadingInProgress)
                {
                    _reloadTimeLeft = GetTotalReloadTime();
                    _nextFireTime = Time.time + _reloadTimeLeft;
                    _reloadingInProgress = true;
                    _uiGameScreen.SetAmmoBar(0);
                }

                if (_bulletsInClip > 0 && !_reloadingInProgress)
                {
                    _uiGameScreen.SetAmmoBar(GetPercentageOfClipLeft());
                    _uiGameScreen.SetBullets(_bulletsInClip);
                }

                if (_reloadingInProgress)
                {
                    ReloadingProgress();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error in PlayerWeaponController.Tick: " + e.Message + "\n" + e.StackTrace);
            }
        }

        // ── Fire ──────────────────────────────────────────────────────────────

        private void FireOnce()
        {
            _bulletsInClip--;
            _nextFireTime = Time.time + 1f / WeaponConfigSingleton.Instance.WeaponConfig.FireRate;

            MuzzleFlash();
            MuzzleSmoke();
            var clip = WeaponConfigSingleton.Instance.WeaponConfig.shootingClip;
            if (clip != null) SoundFXManager.Instance.PlaySoundFXClip(clip, transform, 1f);

            if (CameraShakeManager.Instance != null)
            {
                float weaponRecoil = WeaponConfigSingleton.Instance.WeaponConfig.Recoil;
                float playerStrength = PlayerConfigSingleton.Instance.PlayerConfig.strength;
                CameraShakeManager.Instance.ShakeOnFire(weaponRecoil, playerStrength);
            }

            ApplyRecoil(_aimVisuals != null ? _aimVisuals.CurrentAngle : 0f);

            // ── Probability roll ──────────────────────────────────────────────
            var zombies = _aimingCircleTrigger != null ? _aimingCircleTrigger.GetZombiesInside() : null;
            if (zombies == null || zombies.Count == 0)
            {
                Debug.Log("[SHOT] No zombies in gizmo → MISS");
                SpawnMissBullet();
                return;
            }

            Debug.Log($"[SHOT] Zombies in gizmo: {zombies.Count}");

            GameObject primary = GetPrimaryTarget(zombies);
            Debug.Log($"[SHOT] Primary target: {(primary != null ? primary.name : "none")}");

            // Centralized hit chance — measured to the actual primary target.
            var weaponCfg = WeaponConfigSingleton.Instance.WeaponConfig;
            var playerCfg = PlayerConfigSingleton.Instance.PlayerConfig;
            float angle = _aimVisuals != null ? _aimVisuals.CurrentAngle : 30f;
            Vector3 targetPos = primary != null ? primary.transform.position : transform.position;
            float distance = Vector3.Distance(transform.position, targetPos);

            var hcr = AimPrecisionColors.ComputeHitChance(angle, distance, weaponCfg, playerCfg);
            float hitChance = hcr.Value;

            Debug.Log($"[SHOT] Angle={angle:F2}° | Dist={distance:F1} PB={hcr.IsPointBlank} DistMult={hcr.DistanceMultiplier:F2} | HitChance={hitChance:F3} | AimMode={(_targeting != null ? _targeting.CurrentAimingType.ToString() : "null")}");

            // Downed zombie → next shot is a guaranteed point-blank hit so the bonus crit reliably lands.
            Enemy primaryEnemy = primary != null ? (primary.GetComponent<Enemy>() ?? primary.GetComponentInParent<Enemy>()) : null;
            if (primaryEnemy != null && primaryEnemy.HasPendingGuaranteedCrit)
            {
                hitChance = 1f;
                Debug.Log("[SHOT] Primary is downed → guaranteed point-blank hit + crit");
            }

            GameObject hit = null;

            // Primary roll
            if (primary != null)
            {
                float roll = UnityEngine.Random.value;
                bool primaryHit = roll <= hitChance;
                Debug.Log($"[SHOT] Primary roll={roll:F3} vs hitChance={hitChance:F3} → {(primaryHit ? "HIT" : "miss")}");
                if (primaryHit) hit = primary;
            }

            // Secondary rolls
            if (hit == null)
            {
                _secondaryBuffer.Clear();
                foreach (var c in zombies)
                {
                    if (c != null && c.gameObject != primary)
                        _secondaryBuffer.Add(c.gameObject);
                }
                Shuffle(_secondaryBuffer);
                float secondaryChance = hitChance * SecondaryPenaltyMultiplier;
                Debug.Log($"[SHOT] Rolling {_secondaryBuffer.Count} secondaries, secondaryChance={secondaryChance:F3}");
                for (int i = 0; i < _secondaryBuffer.Count; i++)
                {
                    float roll = UnityEngine.Random.value;
                    bool secHit = roll <= secondaryChance;
                    Debug.Log($"[SHOT]   Secondary[{i}] {_secondaryBuffer[i].name} roll={roll:F3} → {(secHit ? "HIT" : "miss")}");
                    if (secHit) { hit = _secondaryBuffer[i]; break; }
                }
            }

            if (hit != null)
            {
                Debug.Log($"[SHOT] RESULT: HIT {hit.name}");
                SpawnHitBullet(hit);
            }
            else
            {
                Debug.Log("[SHOT] RESULT: MISS — spawning miss bullet");
                SpawnMissBullet();
            }
        }

        // ── Roll helpers ──────────────────────────────────────────────────────

        private GameObject GetPrimaryTarget(HashSet<Collider> zombies)
        {
            // Controller mode: use locked target if it is still in the gizmo
            if (_targeting != null && _targeting.CurrentAimingType == AimingType.ControllerRightStick)
            {
                GameObject locked = _targeting.CurrentTarget;
                if (locked != null)
                {
                    foreach (var c in zombies)
                        if (c != null && c.gameObject == locked) return locked;
                }
            }

            // Mouse mode: zombie in gizmo closest to mouse ground point
            if (_targeting != null && _targeting.CurrentAimingType == AimingType.Mouse)
            {
                Vector3 mousePoint = _targeting.MouseGroundPoint;
                float bestDist = float.MaxValue;
                GameObject best = null;
                foreach (var c in zombies)
                {
                    if (c == null) continue;
                    float d = (c.transform.position - mousePoint).sqrMagnitude;
                    if (d < bestDist) { bestDist = d; best = c.gameObject; }
                }
                return best;
            }

            return null;
        }

        private void SpawnHitBullet(GameObject targetZombie)
        {
            // Collider may be on a child — walk up the hierarchy
            Enemy enemy = targetZombie.GetComponent<Enemy>() ?? targetZombie.GetComponentInParent<Enemy>();

            bool crit = WeaponConfigSingleton.weaponConfig.CritChance > 0 && UnityEngine.Random.value <= WeaponConfigSingleton.weaponConfig.CritChance / 100f;
            // Hitting a downed zombie consumes its pending bonus → forced crit (one per knockdown).
            if (enemy != null && enemy.ConsumeGuaranteedCrit())
            {
                crit = true;
                Debug.Log("[SHOT] Guaranteed crit on downed zombie");
            }

            Transform gunBarrel = GetWeaponPosition();
            Vector3 dir = (targetZombie.transform.position - gunBarrel.position).normalized;
            dir.y = 0;

            // Apply damage immediately via roll system
            float damage = WeaponConfigSingleton.Instance.WeaponConfig.Damage
                           * (1 + UnityEngine.Random.Range(
                               -WeaponConfigSingleton.Instance.WeaponConfig.DamageFluctuation,
                                WeaponConfigSingleton.Instance.WeaponConfig.DamageFluctuation) / 100f);
            if (crit)
            {
                damage *= WeaponConfigSingleton.Instance.WeaponConfig.CritMultiplier;
            }
            if (enemy == null)
                Debug.LogWarning($"[SHOT] SpawnHitBullet: no Enemy component found on {targetZombie.name} or its parents!");
            else
                Debug.Log($"[SHOT] Applying damage={damage:F1} to {enemy.gameObject.name} Crit: {crit.ToString()}");
            enemy?.DamageReceived(damage, dir, crit, WeaponConfigSingleton.Instance.WeaponConfig.Stagger);
            // Note: Enemy.DamageReceived already spawns the damage number internally

            SpawnBullet(gunBarrel, dir, isHitBullet: true, isCrit: crit);
        }

        private void SpawnMissBullet()
        {
            Transform gunBarrel = GetWeaponPosition();
            float randomAngle = UnityEngine.Random.Range(-MissConeHalfAngle, MissConeHalfAngle);
            Vector3 dir = Quaternion.Euler(0f, randomAngle, 0f) * transform.forward;
            SpawnBullet(gunBarrel, dir, isHitBullet: false);
        }

        private void SpawnBullet(Transform gunBarrel, Vector3 dir, bool isHitBullet, bool isCrit = false)
        {
            dir = dir.normalized;
            dir.y = 0;
            Quaternion rot = Quaternion.LookRotation(dir) * Quaternion.Euler(-90, 0, 0);
            GameObject bullet = Instantiate(_bulletPrefab, gunBarrel.position, rot);
            bullet.GetComponent<BulletBehaviour>()?.Initialize(dir, isHitBullet, isCrit);
        }

        private static void Shuffle(List<GameObject> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ── Recoil ────────────────────────────────────────────────────────────

        public void ApplyRecoil(float currentAngle)
        {
            float weaponRecoil = WeaponConfigSingleton.Instance.WeaponConfig.Recoil;
            float playerRecoilReduction = PlayerConfigSingleton.Instance.PlayerConfig.recoilReduction;
            float totalRecoil = Mathf.Max(weaponRecoil - playerRecoilReduction, 0);

            float minAngle = _gameStats._precisionMin;
            float maxAngle = _gameStats._precisionMax;
            float totalAngleRange = maxAngle - minAngle;
            float newAngle = Mathf.Min(currentAngle + (totalAngleRange * (totalRecoil / 100)), maxAngle);

            if (_aimVisuals != null) _aimVisuals.Recoil = newAngle;
            _rigRecoilController?.ApplyRecoil(weaponRecoil, playerRecoilReduction);

            Debug.Log($"Recoil Applied: Weapon={weaponRecoil}, Reduction={playerRecoilReduction}, Total={totalRecoil}, NewAngle={newAngle}");
        }

        // ── Reload ────────────────────────────────────────────────────────────

        private void ReloadingProgress()
        {
            _reloadingProgress = (int)(100 - (100 * (_reloadTimeLeft / GetTotalReloadTime()))) + 1;
            _reloadTimeLeft -= Time.deltaTime;
            _uiGameScreen.SetAmmoBar(_reloadingProgress);
            if (_reloadingProgress >= 100)
            {
                _bulletsInClip = WeaponConfigSingleton.Instance.WeaponConfig.ClipSize;
                _uiGameScreen.SetBullets(_bulletsInClip);
                _reloadingProgress = 100;
                _reloadingInProgress = false;
            }
        }

        private float GetPercentageOfClipLeft()
        {
            return (float)_bulletsInClip / WeaponConfigSingleton.Instance.WeaponConfig.ClipSize * 100f;
        }

        private float GetTotalReloadTime()
        {
            return WeaponConfigSingleton.Instance.WeaponConfig.ReloadTime
                   * (1 + PlayerConfigSingleton.Instance.PlayerConfig.reloadSpeed / 100);
        }

        // ── Muzzle flash ──────────────────────────────────────────────────────

        private void MuzzleFlash()
        {
            try
            {
                Transform weaponTransform = FindRecursive(transform, "Weapon")
                                         ?? FindRecursive(transform, "Pistol(Clone)");
                if (weaponTransform == null) { Debug.LogWarning("No weapon found on player!"); return; }

                Transform muzzleFlashTransform = weaponTransform.Find("MuzzleFlash01");
                if (muzzleFlashTransform == null) { Debug.LogWarning("MuzzleFlash01 not found!"); return; }

                muzzleFlashTransform.GetComponent<ParticleSystem>()?.Play();
            }
            catch (Exception e)
            {
                Debug.LogError($"Muzzle flash error: {e.Message}");
            }
        }

        // ── Muzzle smoke ──────────────────────────────────────────────────────

        private void MuzzleSmoke()
        {
            try
            {
                // Lazy-cache: resolve the persistent smoke PS once, then Emit each shot.
                if (_muzzleSmoke == null)
                {
                    Transform weaponTransform = FindRecursive(transform, "Weapon")
                                             ?? FindRecursive(transform, "Pistol(Clone)");
                    if (weaponTransform == null) return;
                    Transform smoke = weaponTransform.Find("MuzzleSmoke");
                    if (smoke == null) { Debug.LogWarning("MuzzleSmoke not found on weapon!"); return; }
                    _muzzleSmoke = smoke.GetComponent<ParticleSystem>();
                }
                if (_muzzleSmoke != null)
                {
                    if (!_muzzleSmoke.isPlaying) _muzzleSmoke.Play(); // keep system alive so Emit renders
                    _muzzleSmoke.Emit(10);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Muzzle smoke error: {e.Message}");
            }
        }

        // ── Weapon transform helpers ──────────────────────────────────────────

        public Transform GetWeaponPosition()
        {
            Transform weaponTransform = FindRecursive(transform, "Weapon")
                                     ?? FindRecursive(transform, "Pistol(Clone)")
                                     ?? FindRecursive(transform, "Pistol")
                                     ?? FindRecursive(transform, "Gun")
                                     ?? FindRecursive(transform, "weapon");
            if (weaponTransform == null)
            {
                // Log all children to identify the correct weapon name
                var sb = new System.Text.StringBuilder("[WEAPON] Children of player: ");
                LogChildNames(transform, sb, 0);
                Debug.LogWarning(sb.ToString());
                return transform; // fallback to player root so bullets still fire
            }
            return AddOnZAxis(weaponTransform, 1f);
        }

        private static void LogChildNames(Transform t, System.Text.StringBuilder sb, int depth)
        {
            foreach (Transform child in t)
            {
                sb.Append('\n').Append(' ', depth * 2).Append(child.name);
                if (depth < 4) LogChildNames(child, sb, depth + 1);
            }
        }

        public Transform GetBonePosition(string boneName, GameObject searchObject)
        {
            Transform bone = FindRecursive(searchObject.transform, boneName);
            if (bone == null) Debug.LogError($"Bone '{boneName}' not found on '{searchObject.name}'");
            return bone;
        }

        private Transform AddOnZAxis(Transform source, float z)
        {
            GameObject copyGO = new GameObject(source.name);
            Transform copy = copyGO.transform;
            copy.position = source.position;
            copy.rotation = source.rotation;
            copy.localScale = source.localScale;
            copy.forward = source.forward * 1.01f;
            Destroy(copyGO);
            return copy;
        }

        private Transform FindRecursive(Transform parent, string pattern)
        {
            Regex regex = new Regex(pattern);
            if (regex.IsMatch(parent.name)) return parent;
            foreach (Transform child in parent)
            {
                Transform result = FindRecursive(child, pattern);
                if (result != null) return result;
            }
            return null;
        }
    }
}
