using UnityEngine;

namespace ZombieGame
{
    /// <summary>
    /// Owns the reload lifecycle for the current weapon: timer, ammo-bar progress
    /// and reload SFX. Plays one <see cref="WeaponConfig.loadOneBulletClip"/> per
    /// bullet (revolver/shotgun feel) and one <see cref="WeaponConfig.cockGunClip"/>
    /// at the end. Magazine weapons with large clips get a capped number of load
    /// sounds instead of one-per-bullet so they don't machine-gun.
    /// Plain C# helper driven by <see cref="PlayerWeaponController"/>.
    /// </summary>
    public class Reload
    {
        // ── Tuning ────────────────────────────────────────────────────────────
        /// <summary>Clips this size or smaller play one load sound per bullet.</summary>
        private const int MaxIndividualLoads = 15;
        /// <summary>Load sounds used when clip is larger than <see cref="MaxIndividualLoads"/>.</summary>
        private const int CappedLoadCount = 5;
        private const float LoadVolume = 1f;
        private const float CockVolume = 1f;

        // ── Dependencies ──────────────────────────────────────────────────────
        private readonly Transform _origin;
        private readonly UIT_GameScreen _uiGameScreen;

        // ── State ─────────────────────────────────────────────────────────────
        private float _totalTime;
        private float _timeLeft;
        private float _interval;        // seconds between load sounds
        private int _loadSoundCount;    // total load sounds this reload
        private int _loadSoundsPlayed;
        private bool _cockPlayed;

        public bool InProgress { get; private set; }
        /// <summary>Total duration of the current reload (valid after <see cref="Begin"/>).</summary>
        public float TotalTime => _totalTime;

        public Reload(Transform origin, UIT_GameScreen uiGameScreen)
        {
            _origin = origin;
            _uiGameScreen = uiGameScreen;
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        /// <summary>Starts a reload. Returns the total reload time.</summary>
        public float Begin()
        {
            WeaponConfig weapon = WeaponConfigSingleton.Instance.WeaponConfig;

            _totalTime = GetTotalReloadTime();
            _timeLeft = _totalTime;
            _loadSoundsPlayed = 0;
            _cockPlayed = false;
            InProgress = true;

            _loadSoundCount = weapon.ClipSize <= MaxIndividualLoads ? weapon.ClipSize : CappedLoadCount;
            if (_loadSoundCount < 1) _loadSoundCount = 1;

            // Reserve room at the tail for the cock sound so the last load doesn't overlap it.
            float cockLen = weapon.cockGunClip != null ? weapon.cockGunClip.length : 0f;
            float loadWindow = Mathf.Max(0f, _totalTime - cockLen);
            _interval = loadWindow / _loadSoundCount;

            _uiGameScreen.SetAmmoBar(0);
            return _totalTime;
        }

        /// <summary>
        /// Advances the reload by one frame. Returns true on the frame the reload
        /// completes (caller should refill the clip).
        /// </summary>
        public bool Tick()
        {
            if (!InProgress) return false;

            WeaponConfig weapon = WeaponConfigSingleton.Instance.WeaponConfig;

            _timeLeft -= Time.deltaTime;
            float elapsed = _totalTime - _timeLeft;

            int shouldHavePlayed = _interval > 0f
                ? Mathf.Clamp(Mathf.FloorToInt(elapsed / _interval), 0, _loadSoundCount)
                : _loadSoundCount;

            while (_loadSoundsPlayed < shouldHavePlayed)
            {
                Play(weapon.loadOneBulletClip, LoadVolume);
                _loadSoundsPlayed++;
            }

            int progress = Mathf.Clamp((int)(100f * elapsed / _totalTime) + 1, 0, 100);
            _uiGameScreen.SetAmmoBar(progress);

            if (_timeLeft <= 0f)
            {
                // Flush any load sounds the framerate skipped, then cock.
                while (_loadSoundsPlayed < _loadSoundCount)
                {
                    Play(weapon.loadOneBulletClip, LoadVolume);
                    _loadSoundsPlayed++;
                }
                if (!_cockPlayed)
                {
                    Play(weapon.cockGunClip, CockVolume);
                    _cockPlayed = true;
                }

                _uiGameScreen.SetAmmoBar(100);
                InProgress = false;
                return true;
            }

            return false;
        }

        /// <summary>Aborts an in-progress reload without firing the cock sound.</summary>
        public void Cancel()
        {
            InProgress = false;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private float GetTotalReloadTime()
        {
            return WeaponConfigSingleton.Instance.WeaponConfig.ReloadTime
                   * (1 + PlayerConfigSingleton.Instance.PlayerConfig.reloadSpeed / 100);
        }

        private void Play(AudioClip clip, float volume)
        {
            if (clip == null || SoundFXManager.Instance == null) return;
            SoundFXManager.Instance.PlaySoundFXClip(clip, _origin, volume);
        }
    }
}
