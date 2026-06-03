using UnityEngine;
using UnityEngine.AI;

public class WalkToSelected : IState
{
    private readonly Enemy _enemy;
    private NavMeshAgent _navMeshAgent;
    private Animator _animator;
    public float TimeStuck = 0f;
    private Vector3 _lastPosition = Vector3.zero;
    private EnemyConfig _enemyConfig;
    private Transform _target;
    private float _velocity;
    private float _lastLogTime = 0f;

    // New: pathing + diagnostics helpers
    private Vector3 _lastRequestedDestination = Vector3.positiveInfinity;
    private float _nextRepathTime = 0f;
    private const float RepathInterval = 0.2f;                // seconds between repaths
    private const float DestinationMoveThreshold = 0.5f;       // meters before re-issue SetDestination
    private const float StuckMoveEpsSqr = 0.0004f;             // ~2cm squared movement between frames
    private const float StuckRecoverSeconds = 2f;              // time of no progress before recovery
    private const bool DebugLogs = true;                      // flip to true to see periodic logs
    private readonly EnemyAnimator _enemyAnimator;
    private static readonly int VelocityHash = Animator.StringToHash("velocity");

    public WalkToSelected(Enemy enemy, NavMeshAgent navMeshAgent, Animator animator, EnemyConfig enemyConfig, Transform target, EnemyAnimator enemyAnimator)
    {
        _enemy = enemy;
        _navMeshAgent = navMeshAgent;
        _animator = animator;
        _enemyConfig = enemyConfig;
        _target = target;
        _enemyAnimator = enemyAnimator;
    }

    public void OnEnter()
    {
        _enemy.AttackFinished = false; // reset for next attack cycle

        TimeStuck = 0f;
        if (!_navMeshAgent.enabled) _navMeshAgent.enabled = true;
        _navMeshAgent.isStopped = false;
        _navMeshAgent.updatePosition = true;
        _navMeshAgent.updateRotation = true;

        _navMeshAgent.speed = (_enemyConfig.speed > 0.01f) ? _enemyConfig.speed : 3.5f;
        _navMeshAgent.ResetPath();

        var playerT = _enemy._player != null ? _enemy._player.transform : _target;
        if (playerT != null)
        {
            _navMeshAgent.SetDestination(playerT.position);
            _lastRequestedDestination = playerT.position;
        }
        else
        {
            _lastRequestedDestination = Vector3.positiveInfinity;
        }

        _nextRepathTime = Time.time + RepathInterval;
        _lastPosition = _enemy.transform.position;
        // Do NOT reset the animator here — let it carry its current value.
        // Tick() will ramp it up via desiredVelocity as soon as the path resolves.

        Debug.Log("WalkToSelected OnEnter:" + (playerT != null ? playerT.name : "null"));
    }

    public void OnExit()
    {
        _navMeshAgent.isStopped = true;
    }

    public void Tick()
    {
        // Resolve current target
        Transform playerT = _enemy._player != null ? _enemy._player.transform : _target;
        if (playerT == null) return;

        // Ensure agent is usable
        if (!_navMeshAgent.enabled)
            _navMeshAgent.enabled = true;

        if (!_navMeshAgent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(_enemy.transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                _navMeshAgent.Warp(hit.position);
            }
            else
            {
                // Can't recover onto NavMesh now; bail out this tick
                return;
            }
        }

        if (_navMeshAgent.isStopped) _navMeshAgent.isStopped = false;

        // Ensure speed is sane (prefer config)
        if (_navMeshAgent.speed <= 0.01f)
        {
            _navMeshAgent.speed = (_enemyConfig.speed > 0.01f) ? _enemyConfig.speed : 3.5f;
            Debug.LogWarning($"[WalkToSelected] NavMeshAgent speed was zero, reset to {_navMeshAgent.speed}");
        }

        // Repath throttled: only when the target moved enough or periodically, or path is invalid/partial
        Vector3 targetPos = playerT.position;
        bool needRepath =
            !_navMeshAgent.hasPath ||
            _navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid ||
            _navMeshAgent.pathStatus == NavMeshPathStatus.PathPartial;

        if (Time.time >= _nextRepathTime)
        {
            if (_lastRequestedDestination == Vector3.positiveInfinity ||
                (targetPos - _lastRequestedDestination).sqrMagnitude >= (DestinationMoveThreshold * DestinationMoveThreshold))
            {
                needRepath = true;
            }
        }

        if (needRepath)
        {
            _navMeshAgent.SetDestination(targetPos);
            _lastRequestedDestination = targetPos;
            _nextRepathTime = Time.time + RepathInterval;
        }

        // Animator: drive blend tree from desiredVelocity.
        // While pathPending, desiredVelocity is 0 but the agent hasn't moved yet either,
        // so just hold the current value and let it ramp naturally once the path resolves.
        float speed = _navMeshAgent.speed;
        if (!_navMeshAgent.pathPending)
        {
            float norm = (speed > 0.001f)
                ? Mathf.Clamp01(_navMeshAgent.desiredVelocity.magnitude / speed)
                : 0f;
            _velocity = norm;
            _enemyAnimator.SetVelocity(_velocity, 0.05f);
        }

        // Stuck detection & recovery
        Vector3 pos = _enemy.transform.position;
        if (_navMeshAgent.hasPath && _navMeshAgent.remainingDistance > 0.2f)
        {
            float movedSqr = (pos - _lastPosition).sqrMagnitude;
            if (movedSqr < StuckMoveEpsSqr && _navMeshAgent.desiredVelocity.sqrMagnitude > 0.01f)
            {
                TimeStuck += Time.deltaTime;

                if (TimeStuck >= StuckRecoverSeconds)
                {
                    if (NavMesh.SamplePosition(pos, out NavMeshHit hit2, 1.5f, NavMesh.AllAreas))
                    {
                        _navMeshAgent.Warp(hit2.position);
                        _navMeshAgent.ResetPath();
                        _lastRequestedDestination = Vector3.positiveInfinity; // force repath next tick window
                    }
                    TimeStuck = 0f;
                }
            }
            else
            {
                TimeStuck = 0f;
            }

            _lastPosition = pos;
        }

        // Optional periodic debug
        if (DebugLogs && Time.time - _lastLogTime >= 1f)
        {
            Debug.Log($"[WalkToSelected] onNavMesh={_navMeshAgent.isOnNavMesh}, " +
                $"velocity={_velocity}," +
                $"speed={_navMeshAgent.speed:F2}, hasPath={_navMeshAgent.hasPath}, " +
                $"enabled={_navMeshAgent.enabled}, stopped={_navMeshAgent.isStopped}, " +
                $"status={_navMeshAgent.pathStatus}, remaining={_navMeshAgent.remainingDistance:F2}, " +
                $"desiredVel={_navMeshAgent.desiredVelocity.magnitude:F2}, " +
                $"pos={_enemy.transform.position}, " +
                $"target={playerT.position}");
            _lastLogTime = Time.time;
        }
    }
}
