using UnityEngine;
using UnityEngine.AI;

public class WalkToGathering : IState
{
    private readonly Enemy _enemy;
    private NavMeshAgent _navMeshAgent;
    private Animator _animator;
    public float TimeStuck = 0f;
    private Vector3 _lastPosition = Vector3.zero;
    private EnemyConfig _enemyConfig;
    private Transform _target;
    private static readonly float Run = Animator.StringToHash("Run");
    private float _velocity;

    public WalkToGathering(Enemy enemy, NavMeshAgent navMeshAgent, Animator animator, EnemyConfig enemyConfig, Transform target)
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
        _navMeshAgent.enabled = true;
        _navMeshAgent.SetDestination(_target.position);
        _enemy.GetComponent<NavMeshAgent>().destination = _target.position;

    }

    public void OnExit()
    {
        Debug.Log("WalkToGathering OnExit");
        _navMeshAgent.isStopped = true;
    }

    public void Tick()
    {
        if (Vector3.Distance(_enemy.transform.position, _target.position) <= 3f)
        {
            _enemy.GetComponent<NavMeshAgent>().isStopped = true;
        }
        else
        {
            _enemy.GetComponent<NavMeshAgent>().isStopped = false;
            _navMeshAgent.SetDestination(_target.position);
        }
        _velocity = _navMeshAgent.velocity.magnitude / _navMeshAgent.speed;
        _animator.SetFloat("velocity", _velocity);

    }
}
