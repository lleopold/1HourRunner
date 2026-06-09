using UnityEngine;
using UnityEngine.AI;

public class Stop : IState
{
    private readonly NavMeshAgent _agent;
    private readonly EnemyAnimator _enemyAnimator;

    private const float StoppedVelocity = 0.1f;

    // Exit once the agent has actually decelerated to a near-stop.
    public bool CanExit => _agent.velocity.magnitude < StoppedVelocity;

    public Stop(Enemy enemy, NavMeshAgent agent, EnemyAnimator enemyAnimator)
    {
        _agent = agent;
        _enemyAnimator = enemyAnimator;
    }

    public void OnEnter()
    {
        if (!_agent.enabled) _agent.enabled = true;

        // Clear the path but leave isStopped = false so the agent
        // decelerates naturally via its own acceleration, instead of snapping.
        _agent.ResetPath();
        _agent.isStopped = false;
    }

    public void Tick()
    {
        // Animation reads the body's real speed — they can't desync.
        if (_agent.isActiveAndEnabled)
            _enemyAnimator.SetVelocity(_agent.velocity.magnitude / _agent.speed);
    }

    public void OnExit()
    {
        // Lock it so it can't drift after we've arrived at a stop.
        if (_agent.isActiveAndEnabled)
            _agent.isStopped = true;
    }
}