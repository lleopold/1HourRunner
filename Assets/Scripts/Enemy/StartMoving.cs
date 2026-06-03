using UnityEngine;
using UnityEngine.AI;

public class StartMoving : IState
{
    private readonly Enemy _enemy;
    private readonly NavMeshAgent _agent;
    private Animator _anim;

    private static readonly int VelocityHash = Animator.StringToHash("velocity");
    private const float ExitVelocityThreshold = 0.75f; // fraction of max speed (0–1)

    public bool CanExit { get; private set; }

    public StartMoving(Enemy enemy, NavMeshAgent agent)
    {
        _enemy = enemy;
        _agent = agent;
    }

    public void OnEnter()
    {
        CanExit = false;

        if (!_agent.enabled) _agent.enabled = true;

        _agent.isStopped = false;
        _agent.updatePosition = true;
        _agent.updateRotation = true;

        if (_anim == null) _anim = _enemy.GetComponent<Animator>();

        // Set destination immediately so the agent starts pathfinding
        if (_enemy._player != null)
            _agent.SetDestination(_enemy._player.transform.position);
    }

    public void Tick()
    {
        if (_anim == null) return;

        float speed = _agent.speed > 0.001f ? _agent.speed : 1f;
        float norm = Mathf.Clamp01(_agent.desiredVelocity.magnitude / speed);
        _anim.SetFloat(VelocityHash, norm, 0.15f, Time.deltaTime);

        if (norm >= ExitVelocityThreshold)
            CanExit = true;
    }

    public void OnExit()
    {
        // agent is already moving; WalkToSelected will take over path management
    }
}
