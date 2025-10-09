using UnityEngine;
using UnityEngine.AI;

public class Stop : IState
{
    private readonly Enemy _enemy;
    private readonly NavMeshAgent _agent;
    private Animator _anim;

    private float _t;
    private const float WaitDuration = 0.60f; // also used as damping time
    private static readonly int VelocityHash = Animator.StringToHash("velocity");

    public bool CanExit => _t >= WaitDuration;

    public Stop(Enemy enemy, NavMeshAgent agent)
    {
        _enemy = enemy;
        _agent = agent;
    }

    public void OnEnter()
    {
        if (!_agent.enabled) _agent.enabled = true;

        // Immediate stop
        _agent.updatePosition = true;
        _agent.updateRotation = true;
        _agent.isStopped = true;
        _agent.ResetPath();
        _agent.velocity = Vector3.zero;

        if (_anim == null) _anim = _enemy.GetComponent<Animator>();

        _t = 0f;
        // Do not snap the blend tree; Tick will damp it to 0 over WaitDuration.
    }

    public void Tick()
    {
        _t += Time.deltaTime;

        if (_anim)
        {
            // Valid overload: SetFloat(id, value, dampTime, deltaTime)
            _anim.SetFloat(VelocityHash, 0f, WaitDuration, Time.deltaTime);

            // Optional clamp to avoid tail drift
            if (_anim.GetFloat(VelocityHash) < 0.01f)
                _anim.SetFloat(VelocityHash, 0f);
        }
    }

    public void OnExit()
    {
        if (_anim) _anim.SetFloat(VelocityHash, 0f);
        // Leave agent stopped; the next state should decide when to resume.
    }
}
