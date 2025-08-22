using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Stop : IState
{

    private readonly Enemy _enemy;
    private NavMeshAgent _navMeshAgent;

    public Stop(Enemy enemy, NavMeshAgent navMeshAgent)
    {
        _enemy = enemy;
        _navMeshAgent = navMeshAgent;
    }
    public void OnEnter()
    {
        Debug.Log("Stop OnEnter");
        _enemy.GetComponent<NavMeshAgent>().isStopped = true;
        //_enemy.GetComponent<Animator>().SetTrigger("GoToIdle");
    }

    public void OnExit()
    {
        _enemy.Target = GameObject.Find("Player");
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
