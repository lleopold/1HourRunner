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
        private static readonly Color Good  = Color.green;
        private static readonly Color Great = Color.blue;
        private static readonly Color Elite = new Color(0.6f, 0f, 1f);   // purple
        private static readonly Color Crit  = Color.red;

        // ── Transition state ─────────────────────────────────────────────────
        private static Color   _currentColor    = Color.white;
        private static Color   _previousTier    = Color.white;
        private static float   _flashTimer      = 0f;  // counts down from FlashDuration
        private static bool    _initialized     = false;

        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Returns the hit probability multiplier for <paramref name="angleDegrees"/> based on aim tier.</summary>
        public static float GetHitMultiplier(float angleDegrees)
        {
            if (angleDegrees > 20f) return 0.50f;
            if (angleDegrees > 15f) return 0.75f;
            if (angleDegrees > 10f) return 0.85f;
            if (angleDegrees >  7f) return 0.95f;
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

        /// <summary>Returns the raw tier color for <paramref name="angleDegrees"/> with no animation.</summary>
        public static Color GetColor(float angleDegrees)
        {
            if (angleDegrees > 20f) return Trash;
            if (angleDegrees > 15f) return Good;
            if (angleDegrees > 10f) return Great;
            if (angleDegrees >  7f) return Elite;
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
                _initialized  = true;
            }

            // Detect tier change → trigger flash
            if (targetTier != _previousTier)
            {
                _flashTimer    = FlashDuration;
                _previousTier  = targetTier;
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
