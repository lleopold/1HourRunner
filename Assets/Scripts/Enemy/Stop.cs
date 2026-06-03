using UnityEngine;
using UnityEngine.AI;

public class Stop : IState
{
    private readonly Enemy _enemy;
    private readonly NavMeshAgent _agent;
    private readonly EnemyAnimator _enemyAnimator;

    private float _t;
    private const float MinWaitDuration = 0.3f;
    private const float DampTime = 0.6f;
    private const float VelocityEps = 0.05f;

    public bool CanExit =>
        _t >= MinWaitDuration &&
        _enemyAnimator.Velocity < VelocityEps;

    public Stop(Enemy enemy, NavMeshAgent agent, EnemyAnimator enemyAnimator)
    {
        _enemy = enemy;
        _agent = agent;
        _enemyAnimator = enemyAnimator;
    }

    public void OnEnter()
    {
        if (!_agent.enabled) _agent.enabled = true;

        // Immediate stop — reset path BEFORE re-enabling updateRotation to avoid a one-frame rotation snap
        _agent.isStopped = true;
        _agent.ResetPath();
        _agent.updatePosition = true;
        _agent.updateRotation = true;

        _t = 0f;
        // Do not snap the blend tree; Tick will damp it to 0 over WaitDuration.
    }

    public void Tick()
    {
        _t += Time.deltaTime;

        _enemyAnimator.SetVelocity(0f, DampTime);
        _enemyAnimator.SyncFromAnimator();
    }

    public void OnExit()
    {
        // Do NOT snap velocity here — let the next state (StartMoving/AttackFreely) own the blend tree.
    }
}
