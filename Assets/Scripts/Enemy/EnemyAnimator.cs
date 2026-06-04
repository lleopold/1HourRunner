using UnityEngine;

/// <summary>
/// Owns all velocity writes to the enemy Animator.
/// Velocity NEVER snaps — every write is forced through a minimum damp time.
/// </summary>
public class EnemyAnimator
{
    private readonly Animator _animator;
    private static readonly int VelocityHash = Animator.StringToHash("velocity");

    private const float DefaultDampTime = 0.15f;
    private const float MinDampTime     = 0.12f; // absolute floor — velocity can never jump instantly

    private float _current = 0f;

    public float Velocity => _current;

    public EnemyAnimator(Animator animator)
    {
        _animator = animator;
        _current  = animator != null ? animator.GetFloat(VelocityHash) : 0f;
    }

    /// <summary>
    /// Set target velocity. dampTime is clamped to MinDampTime so snapping is physically impossible.
    /// </summary>
    public void SetVelocity(float target, float dampTime = DefaultDampTime)
    {
        if (_animator == null) return;

        target = Mathf.Clamp01(target);

        if (dampTime < MinDampTime)
        {
            Debug.LogError(
                $"[EnemyAnimator] SetVelocity called with dampTime={dampTime:F3} < MinDampTime={MinDampTime:F3}. " +
                $"This would cause flickering. Fix the caller.");
            dampTime = MinDampTime;
        }

        _animator.SetFloat(VelocityHash, target, dampTime, Time.deltaTime);
        _current = _animator.GetFloat(VelocityHash);
    }

    /// <summary>
    /// Sync internal cache from the animator (use after external writes you cannot avoid).
    /// </summary>
    public void SyncFromAnimator()
    {
        if (_animator != null)
            _current = _animator.GetFloat(VelocityHash);
    }
}
