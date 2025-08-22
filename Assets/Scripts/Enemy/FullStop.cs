using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FullStop : IState
{

    private readonly Enemy _enemy;
    private NavMeshAgent _navMeshAgent;

    public FullStop(Enemy enemy, NavMeshAgent navMeshAgent)
    {
        _enemy = enemy;
        _navMeshAgent = navMeshAgent;
    }
    public void OnEnter()
    {
        Debug.Log("Stop OnEnter");
        _enemy.GetComponent<NavMeshAgent>().velocity = Vector3.zero;
        _enemy.GetComponent<NavMeshAgent>().isStopped = true;
        SetIdleAnimation();
        //_enemy.GetComponent<Animator>().SetTrigger("GoToIdle");
    }
    private void SetIdleAnimation()
    {
        Debug.Log("SetIdleAnimation");
        _enemy.GetComponent<Animator>().SetFloat("velocity", 0);
    }

    public void OnExit()
    {
        _enemy.Target = null;
        //Debug.Log("Stop OnExit");
        //Debug.Log("Stop OnExit");
    }

    public void Tick()
    {
        if (_enemy.Target == null)
        {
            _enemy.Target = GameObject.Find("Player");
        }
        //Debug.Log("Stop Tick"); 
    }
}
