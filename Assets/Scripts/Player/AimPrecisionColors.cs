using UnityEngine;

namespace ZombieGame
{
    /// <summary>
    /// Defines the color thresholds for the aiming V-gizmo based on current aim angle.
    ///
    /// Angle ranges (degrees):
    ///   30 – 20  →  White  (wide / trash)
    ///   20 – 15  →  Green
    ///   15 – 10  →  Blue
    ///   10 –  7  →  Purple
    ///    7 –  3  →  Red
    ///    ≤  3    →  Red pulsing (crit zone)
    ///
    /// On tier change: briefly flashes white then smoothly lerps into the new tier color.
    /// Call <see cref="GetAnimatedColor"/> every frame from PlayerAimVisuals.
    /// </summary>
    public static class AimPrecisionColors
    {
        public const float CritThreshold = 3f;

        // Speed at which the displayed color lerps toward the target tier color (units/sec)
        public const float TransitionSpeed = 6f;

        // How long the white flash lasts on a tier change (seconds)
        public const float FlashDuration = 0.18f;

        // Pulse speed (Hz) used in the crit zone
        public const float CritPulseSpeed = 8f;

        // ── Tier colors ───────────────────────────────────────────────────────
        private static readonly Color Trash = Color.white;
        private static readonly Color Good = Color.green;
        private static readonly Color Great = Color.blue;
        private static readonly Color Elite = new Color(0.6f, 0f, 1f);   // purple
        private static readonly Color Crit = Color.red;

        // ── Transition state ─────────────────────────────────────────────────
        private static Color _currentColor = Color.white;
        private static Color _previousTier = Color.white;
        private static float _flashTimer = 0f;  // counts down from FlashDuration
        private static bool _initialized = false;

        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Returns the hit probability multiplier for <paramref name="angleDegrees"/> based on aim tier.</summary>
        public static float GetHitMultiplier(float angleDegrees)
        {
            if (angleDegrees > 20f) return 0.50f;
            if (angleDegrees > 15f) return 0.75f;
            if (angleDegrees > 10f) return 0.85f;
            if (angleDegrees > 7f) return 0.95f;
            return 0.99f;
        }

        /// <summary>Returns a distance-based hit multiplier: 1.0 within optimalRange, linear falloff to 0.1 at maxEffectiveRange, clamped to 0.1 beyond.</summary>
        public static float GetDistanceMultiplier(float distance, float optimalRange, float maxEffectiveRange)
        {
            if (distance <= optimalRange) return 1.0f;
            if (distance >= maxEffectiveRange) return 0.1f;
            float t = (distance - optimalRange) / (maxEffectiveRange - optimalRange);
            return Mathf.Lerp(1.0f, 0.1f, t);
        }

        // ── Centralized hit chance ───────────────────────────────────────────
        // Fixed hit chance when the zombie is within point-blank range (close enough).
        public const float PointBlankHitChance = 0.98f;

        /// <summary>Single source of truth for hit chance. Used by the GUI, zombie outline coloring, the actual shot roll, and anyone added later.</summary>
        public struct HitChanceResult
        {
            public float Value;              // final hit chance 0..1
            public bool IsPointBlank;        // zombie close enough → fixed white outline + 98%
            public float DistanceMultiplier; // 1.0 inside point-blank
            public float Distance;
        }

        /// <summary>
        /// Computes hit chance for a single target.
        ///   • Point blank (distance ≤ <see cref="WeaponConfig.PointBlankRange"/>) → fixed <see cref="PointBlankHitChance"/> (98%).
        ///   • Beyond point blank → regular calc: weaponAcc · playerAcc · aimMult · distanceMult.
        /// </summary>
        public static HitChanceResult ComputeHitChance(float angleDegrees, float distance, WeaponConfig wCfg, PlayerConfig pCfg)
        {
            var r = new HitChanceResult { Distance = distance, DistanceMultiplier = 1f, Value = 1f };
            if (wCfg == null || pCfg == null) return r;

            // Point blank: zombie close enough → fixed 98%, white outline.
            if (distance > 0f && distance <= wCfg.PointBlankRange)
            {
                r.IsPointBlank = true;
                r.Value = PointBlankHitChance;
                return r;
            }

            // Beyond point blank → regular hit calculation.
            float aimMult = GetHitMultiplier(angleDegrees);
            r.DistanceMultiplier = (distance > 0f && wCfg.MaxEffectiveRange > 0f)
                ? GetDistanceMultiplier(distance, wCfg.OptimalRange, wCfg.MaxEffectiveRange)
                : 1f;
            r.Value = (wCfg.Accuracy / 100f) * (pCfg.Accuracy / 100f) * aimMult * r.DistanceMultiplier;
            return r;
        }

        /// <summary>Outline color straight from a <see cref="HitChanceResult"/> — white only at point blank.</summary>
        public static Color GetOutlineColor(HitChanceResult r) => GetOutlineColor(r.Value, r.IsPointBlank);

        public static Color GetOutlineColor(float value, bool pointBlank)
        {
            if (pointBlank) return Color.white;
            if (value >= 0.75f) return Color.Lerp(Color.yellow, Color.green, (value - 0.75f) / 0.25f);
            if (value >= 0.40f) return Color.Lerp(Color.red, Color.yellow, (value - 0.40f) / 0.35f);
            return Color.red;
        }

        /// <summary>Returns the raw tier color for <paramref name="angleDegrees"/> with no animation.</summary>
        public static Color GetColor(float angleDegrees)
        {
            if (angleDegrees > 20f) return Trash;
            if (angleDegrees > 15f) return Good;
            if (angleDegrees > 10f) return Great;
            if (angleDegrees > 7f) return Elite;
            return Crit;
        }

        /// <summary>Returns true when the angle is in the crit pulsing zone (≤ 10°).</summary>
        public static bool IsCrit(float angleDegrees) => angleDegrees <= CritThreshold;

        /// <summary>
        /// Call this every frame from PlayerAimVisuals.
        /// Returns a smoothly-animated color that:
        ///   • Flashes white for <see cref="FlashDuration"/> seconds when the tier changes.
        ///   • Lerps from white back to the new tier color over that window.
        ///   • Pulses in the crit zone.
        /// </summary>
        public static Color GetAnimatedColor(float angleDegrees)
        {
            Color targetTier = GetColor(angleDegrees);

            // First-frame init
            if (!_initialized)
            {
                _currentColor = targetTier;
                _previousTier = targetTier;
                _initialized = true;
            }

            // Detect tier change → trigger flash
            if (targetTier != _previousTier)
            {
                _flashTimer = FlashDuration;
                _previousTier = targetTier;
            }

            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;

                // t goes 0→1 as flash wears off; lerp white → target
                float t = 1f - Mathf.Clamp01(_flashTimer / FlashDuration);
                _currentColor = Color.Lerp(Color.white, targetTier, t);
            }
            else
            {
                // Smoothly track the target (handles tiny floating-point drift)
                _currentColor = Color.Lerp(_currentColor, targetTier, Time.deltaTime * TransitionSpeed);
            }

            // Crit zone: pulse the settled color between red and orange-red
            if (IsCrit(angleDegrees))
            {
                float p = 0.5f + 0.5f * Mathf.Sin(Time.time * CritPulseSpeed);
                return Color.Lerp(_currentColor, new Color(1f, 0.45f, 0f), p);
            }

            return _currentColor;
        }
    }
}
