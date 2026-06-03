using UnityEngine;
using UnityEngine.AI;

public class StartMoving : IState
{
    private readonly Enemy _enemy;
    private readonly NavMeshAgent _agent;
    private readonly EnemyAnimator _enemyAnimator;

    private const float ExitVelocityThreshold = 0.75f;

    public bool CanExit { get; private set; }

    public StartMoving(Enemy enemy, NavMeshAgent agent, EnemyAnimator enemyAnimator)
    {
        _enemy = enemy;
        _agent = agent;
        _enemyAnimator = enemyAnimator;
    }

    public void OnEnter()
    {
        CanExit = false;

        if (!_agent.enabled) _agent.enabled = true;

        _agent.isStopped = false;
        _agent.updatePosition = true;
        _agent.updateRotation = true;

        // Set destination immediately so the agent starts pathfinding
        if (_enemy._player != null)
            _agent.SetDestination(_enemy._player.transform.position);
    }

    public void Tick()
    {
        float speed = _agent.speed > 0.001f ? _agent.speed : 1f;
        float norm = Mathf.Clamp01(_agent.desiredVelocity.magnitude / speed);
        _enemyAnimator.SetVelocity(norm, 0.15f);

        if (norm >= ExitVelocityThreshold)
            CanExit = true;
    }

    public void OnExit()
    {
        // agent is already moving; WalkToSelected will take over path management
    }
}
