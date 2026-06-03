using UnityEngine;

/// <summary>
/// Owns all velocity writes to the enemy Animator.
/// Enforces smooth transitions — if any caller tries to jump velocity by more
/// than MaxDeltaPerFrame in a single frame, an error is logged and the value
/// is clamped instead of snapped.
/// </summary>
public class EnemyAnimator
{
    private readonly Animator _animator;
    private static readonly int VelocityHash = Animator.StringToHash("velocity");

    private const float DefaultDampTime  = 0.15f;
    private const float MaxDeltaPerFrame = 0.30f; // max allowed jump per frame (0–1 scale)

    private float _current = 0f;

    public float Velocity => _current;

    public EnemyAnimator(Animator animator)
    {
        _animator = animator;
        _current  = animator != null ? animator.GetFloat(VelocityHash) : 0f;
    }

    /// <summary>
    /// Set target velocity with damping. Use this everywhere instead of raw SetFloat.
    /// </summary>
    public void SetVelocity(float target, float dampTime = DefaultDampTime)
    {
        if (_animator == null) return;

        target = Mathf.Clamp01(target);

        float delta = target - _current;
        if (Mathf.Abs(delta) > MaxDeltaPerFrame)
        {
            Debug.LogError(
                $"[EnemyAnimator] Velocity jump of {delta:F3} detected (current={_current:F3} → target={target:F3}). " +
                $"Clamping to ±{MaxDeltaPerFrame} — fix the caller to use damped transitions.");
            target = _current + Mathf.Sign(delta) * MaxDeltaPerFrame;
        }

        _animator.SetFloat(VelocityHash, target, dampTime, Time.deltaTime);
        _current = _animator.GetFloat(VelocityHash);
    }

    /// <summary>
    /// Instantly sync internal cache from the animator (call after external writes you cannot avoid).
    /// </summary>
    public void SyncFromAnimator()
    {
        if (_animator != null)
            _current = _animator.GetFloat(VelocityHash);
    }
}
