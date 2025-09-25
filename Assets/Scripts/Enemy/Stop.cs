using UnityEngine;
using UnityEngine.AI;

public class Stop : IState
{
    private readonly Enemy _enemy;
    private readonly NavMeshAgent _navMeshAgent;
    private Animator _animator;

    private float _waitTimer;
    private const float WaitDuration = 0.3f;

    // Expose to the state machine so it can decide when to leave Stop.
    public bool CanExit => _waitTimer >= WaitDuration;

    public Stop(Enemy enemy, NavMeshAgent navMeshAgent)
    {
        _enemy = enemy;
        _navMeshAgent = navMeshAgent;
    }

    public void OnEnter()
    {
        Debug.Log("Stop OnEnter");

        if (!_navMeshAgent.enabled) _navMeshAgent.enabled = true;
        _navMeshAgent.isStopped = true;
        _navMeshAgent.updatePosition = true;
        _navMeshAgent.updateRotation = true;
        _navMeshAgent.ResetPath();

        _waitTimer = 0f;

        if (_animator == null)
            _animator = _enemy.GetComponent<Animator>();

        if (_animator != null)
        {
            // Drive the locomotion blend tree to idle.
            _animator.SetFloat("velocity", 0f);
            // If you have an idle trigger, you can also use it:
            // _animator.SetTrigger("GoToIdle");
        }
    }

    public void OnExit()
    {
        _enemy.Target = GameObject.Find("Player");
    }

    public void Tick()
    {
        _waitTimer += Time.deltaTime;

        if (_enemy.Target == null)
        {
            _enemy.Target = GameObject.Find("Player");
        }
    }
}
