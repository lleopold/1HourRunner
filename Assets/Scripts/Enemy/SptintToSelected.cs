using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class SptintToSelected : IState
{
    private readonly Enemy _enemy;
    private NavMeshAgent _navMeshAgent;
    private Animator _animator;
    public float TimeStuck = 0f;
    private Vector3 _lastPosition = Vector3.zero;
    private EnemyConfig _enemyConfig;
    private Transform _target;
    private float _velocity;

    public SptintToSelected(Enemy enemy, NavMeshAgent navMeshAgent, Animator animator, EnemyConfig enemyConfig, Transform target)
    {
        _enemy = enemy;
        _navMeshAgent = navMeshAgent;
        _animator = animator;
        _enemyConfig = enemyConfig;
        _target = target;
    }

    public void OnEnter()
    {
        //Debug.Log("WalkToSelected OnEnter"); 
        TimeStuck = 0f;
        _navMeshAgent.enabled = true;
        _navMeshAgent.isStopped = false;
        _navMeshAgent.SetDestination(_target.position);
        _navMeshAgent.speed = _enemyConfig.speed;
        _velocity = _navMeshAgent.velocity.magnitude / _navMeshAgent.speed;


        _animator.SetFloat("Run", 1);//_navMeshAgent.speed
        Debug.Log("WalkToSelected OnEnter:" + _target.name);

    }

    public void OnExit()
    {
        _navMeshAgent.isStopped = true;
    }

    public void Tick()
    {
        if (Vector3.Distance(_enemy.transform.position, _target.position) <= 0.1f)
        {
            _enemy.GetComponent<NavMeshAgent>().isStopped = true;
        }
        else
        {
            _navMeshAgent.SetDestination(_target.position);
        }
        _velocity = _navMeshAgent.velocity.magnitude / _navMeshAgent.speed;
        _animator.SetFloat("velocity", _velocity);
    }
}
