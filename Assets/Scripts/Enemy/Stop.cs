using UnityEngine;
using UnityEngine.AI;

public class Stop : IState
{
    private readonly Enemy _enemy;
    private readonly NavMeshAgent _agent;
    private readonly EnemyAnimator _enemyAnimator;

    private float _t;
    private const float MinWaitDuration = 0.4f; // Minimum time to spend braking
    private const float DampTime = 0.6f;        // How fast animation reaches 0
    private const float VelocityEps = 0.05f;

    // Exit when both physical movement and animation have settled
    public bool CanExit =>
        _t >= MinWaitDuration &&
        _enemyAnimator.Velocity < VelocityEps &&
        _agent.velocity.magnitude < 0.1f;

    public Stop(Enemy enemy, NavMeshAgent agent, EnemyAnimator enemyAnimator)
    {
        _enemy = enemy;
        _agent = agent;
        _enemyAnimator = enemyAnimator;
    }

    public void OnEnter()
    {
        if (!_agent.enabled) _agent.enabled = true;

        // CRITICAL: Clear path but DO NOT set isStopped = true.
        // This lets the agent "glide" using its acceleration/deceleration properties.
        _agent.ResetPath();
        _agent.isStopped = false;

        _t = 0f;
    }

    public void Tick()
    {
        _t += Time.deltaTime;

        // Linear physical glide to zero instead of exponential Lerp
        if (_agent.isActiveAndEnabled)
        {
            _agent.velocity = Vector3.MoveTowards(_agent.velocity, Vector3.zero, Time.deltaTime * 12f);
        }

        // Linear animation stop
        float currentAnim = _enemyAnimator.Velocity;
        float newAnim = Mathf.MoveTowards(currentAnim, 0f, Time.deltaTime / DampTime);
        _enemyAnimator.SetVelocity(newAnim, 0.12f); 
        _enemyAnimator.SyncFromAnimator();
    }

    public void OnExit()
    {
        // Now that we've "arrived" at a stop, we can hard-stop the agent to prevent drifting
        if (_agent.isActiveAndEnabled)
            _agent.isStopped = true;
    }
}