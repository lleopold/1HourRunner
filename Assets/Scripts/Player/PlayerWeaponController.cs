using System;
using System.Text.RegularExpressions;
using Assets.Scripts.Game;
using DamageNumbersPro;
using UnityEngine;

namespace ZombieGame
{
    /// <summary>
    /// Handles shooting, reload, recoil, muzzle flash, and bullet spawning.
    /// </summary>
    public class PlayerWeaponController : MonoBehaviour
    {
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

        // ── Dependencies (injected via Initialize) ────────────────────────────
        private UIT_GameScreen _uiGameScreen;
        private PlayerAimVisuals _aimVisuals;
        private GameStats _gameStats;

        // ─────────────────────────────────────────────────────────────────────
        internal void Initialize(UIT_GameScreen uiGameScreen, PlayerAimVisuals aimVisuals, GameStats gameStats)
        {
            _uiGameScreen = uiGameScreen;
            _aimVisuals = aimVisuals;
            _gameStats = gameStats;
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
                    _uiGameScreen._bullets.text = _bulletsInClip.ToString();
                }

                if (_reloadingInProgress)
                {
                    ReloadingProgress();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error in PlayerWeaponController.Tick: " + e.Message);
            }
        }

        // ── Fire ──────────────────────────────────────────────────────────────

        private void FireOnce()
        {
            _bulletsInClip--;
            Transform gunBarrel = GetWeaponPosition();
            _nextFireTime = Time.time + 1f / WeaponConfigSingleton.Instance.WeaponConfig.FireRate;

            MuzzleFlash();

            bool isPrecisionShot = _aimVisuals != null && _aimVisuals.IsRadarInPrecisionZone();
            _aimVisuals?.GetType(); // keep reference alive

            Vector3 forceDirection;
            float randomAngle;

            if (isPrecisionShot)
            {
                randomAngle = 0f;
                forceDirection = transform.forward;
                Debug.Log("PRECISION SHOT!");
                SoundFXManager.Instance?.PlaySoundFXClip(WeaponConfigSingleton.Instance.WeaponConfig.shootingClip, transform, 1.2f);
            }
            else
            {
                float currentAngle = _aimVisuals != null ? _aimVisuals.CurrentAngle : 0f;
                randomAngle = UnityEngine.Random.Range(-currentAngle, currentAngle);
                forceDirection = Quaternion.Euler(0f, randomAngle, 0f) * transform.forward;
            }

            SpawnBulletAndShoot(gunBarrel, forceDirection);

            if (!isPrecisionShot)
            {
                ApplyRecoil(_aimVisuals != null ? _aimVisuals.CurrentAngle : 0f);
            }

            SoundFXManager.Instance.PlaySoundFXClip(WeaponConfigSingleton.Instance.WeaponConfig.shootingClip, transform, 1f);

            if (CameraShakeManager.Instance != null)
            {
                float weaponRecoil = WeaponConfigSingleton.Instance.WeaponConfig.Recoil;
                float playerStrength = PlayerConfigSingleton.Instance.PlayerConfig.strength;
                float shakeMultiplier = isPrecisionShot ? 0.3f : 1f;
                CameraShakeManager.Instance.ShakeOnFire(weaponRecoil * shakeMultiplier, playerStrength);
            }
        }

        private void SpawnBulletAndShoot(Transform gunBarrel, Vector3 forceDirection)
        {
            forceDirection = forceDirection.normalized;
            forceDirection.y = 0;
            Quaternion bulletRotation = Quaternion.LookRotation(forceDirection) * Quaternion.Euler(-90, 0, 0);
            GameObject bullet = Instantiate(_bulletPrefab, gunBarrel.position, bulletRotation);
            BulletBehaviour bulletBehaviour = bullet.GetComponent<BulletBehaviour>();
            bulletBehaviour?.Initialize(forceDirection);
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

        // ── Weapon transform helpers ──────────────────────────────────────────

        public Transform GetWeaponPosition()
        {
            Transform weaponTransform = FindRecursive(transform, "Weapon")
                                     ?? FindRecursive(transform, "Pistol(Clone)");
            return AddOnZAxis(weaponTransform, 1f);
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
