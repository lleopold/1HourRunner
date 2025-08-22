using System;
using UnityEngine;
using UnityEngine.AI;

public class Roam : IState
{
    private readonly Enemy _enemy;
    private readonly NavMeshAgent _navMeshAgent;
    private readonly Animator _animator;
    private readonly EnemyConfig _enemyConfig;
    //roam state
    public float roamRadius = 5f; // Radius within which the zombie will roam
    public float roamInterval = 5f; // Time interval for updating the destination
    private Vector3 spawnPosition; // Spawn position of the zombie
    MonoBehaviour _monoBehaviour;
    private float _roamingTimePassed;
    private float _velocity;


    public Roam(Enemy enemy, Transform player, NavMeshAgent navMeshAgent, Animator animator, EnemyConfig enemyConfig, MonoBehaviour monoBehaviour)
    {
        _enemy = enemy;
        _navMeshAgent = navMeshAgent;
        _animator = animator;
        _enemyConfig = enemyConfig;
        _monoBehaviour = monoBehaviour;
        _roamingTimePassed = 0;


    }
    private void RoamFreely()
    {
        //Debug.Log("Roaming:");
        Vector3 randomRoamPoint = spawnPosition + UnityEngine.Random.insideUnitSphere * roamRadius;
        randomRoamPoint.y = spawnPosition.y; // Ensure the y-coordinate remains the same
        _navMeshAgent.SetDestination(randomRoamPoint);
        _navMeshAgent.isStopped = false;
        _navMeshAgent.speed = _enemyConfig.speed / 10f;
    }
    public void Tick()
    {
        if (_roamingTimePassed >= roamInterval)
        {
            RoamFreely();
            _roamingTimePassed = 0;
        }
        else
        {
            _roamingTimePassed += Time.deltaTime;
        }
        if (!_navMeshAgent.pathPending && _navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance)
        {
            // Agent has reached its destination
            // Call your logic here
        }
        else
        {
            //_animator.SetFloat("Walk", _Walkingspeed);//todo
        }
        _velocity = _navMeshAgent.velocity.magnitude / _navMeshAgent.speed;
        _velocity = Mathf.Clamp(_velocity, 0, 0.2f);
        //Debug.Log("Roam Velocity: " + _velocity);
        _animator.SetFloat("velocity", _velocity);


    }
    private DateTime GetCurrentTime()
    {
        return DateTime.Now;
    }

    public void OnEnter()
    {
        spawnPosition = _enemy.transform.position;
    }


    public void OnExit()
    {
        _animator.SetFloat("velocity", 0);
    }
}
