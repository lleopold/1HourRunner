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

    public WalkToSelected(Enemy enemy, NavMeshAgent navMeshAgent, Animator animator, EnemyConfig enemyConfig, Transform target)
    {
        _enemy = enemy;
        _navMeshAgent = navMeshAgent;
        _animator = animator;
        _enemyConfig = enemyConfig;
        _target = target;
    }

    public void OnEnter()
    {
        TimeStuck = 0f;

        if (!_navMeshAgent.enabled) _navMeshAgent.enabled = true;
        _navMeshAgent.isStopped = false;
        _navMeshAgent.updatePosition = true;
        _navMeshAgent.updateRotation = true;

        // Ensure we have a non-zero speed
        _navMeshAgent.speed = (_enemyConfig.speed > 0.01f) ? _enemyConfig.speed : 3.5f;

        _navMeshAgent.ResetPath();

        // Prefer the current player transform if available
        var playerT = _enemy._player != null ? _enemy._player.transform : _target;
        if (playerT != null) _navMeshAgent.SetDestination(playerT.position);

        _velocity = 0f;
        Debug.Log("WalkToSelected OnEnter:" + (playerT != null ? playerT.name : "null"));
    }

    public void OnExit()
    {
        _navMeshAgent.isStopped = true;
    }

    public void Tick()
    {
        // Use the latest player transform if available
        Transform playerT = _enemy._player != null ? _enemy._player.transform : _target;
        if (playerT == null) return;

        if (!_navMeshAgent.enabled) _navMeshAgent.enabled = true;

        // If we somehow ended up off the NavMesh, try to recover
        if (!_navMeshAgent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(_enemy.transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                _navMeshAgent.Warp(hit.position);
            }
        }

        if (_navMeshAgent.isStopped) _navMeshAgent.isStopped = false;

        // Ensure speed is sane
        if (_navMeshAgent.speed <= 0.01f)
        {
            _navMeshAgent.speed = (_enemyConfig.speed > 0.01f) ? _enemyConfig.speed : 3.5f;
        }

        _navMeshAgent.SetDestination(playerT.position);

        // Guard divide-by-zero
        _velocity = (_navMeshAgent.speed > 0.001f)
            ? _navMeshAgent.velocity.magnitude / _navMeshAgent.speed
            : 0f;
        _animator.SetFloat("velocity", _velocity);

        // Log every 5s
        if (Time.time - _lastLogTime >= 5f)
        {
            Debug.Log($"[WalkToSelected] onNavMesh={_navMeshAgent.isOnNavMesh}, enabled={_navMeshAgent.enabled}, stopped={_navMeshAgent.isStopped}, speed={_navMeshAgent.speed:F2}, hasPath={_navMeshAgent.hasPath}, status={_navMeshAgent.pathStatus}, remaining={_navMeshAgent.remainingDistance:F2}, desiredVel={_navMeshAgent.desiredVelocity.magnitude:F2}, pos={_enemy.transform.position}, target={playerT.position}");
            _lastLogTime = Time.time;
        }
    }
}
