/*
2026-06-05 AI-Tag
This was created with the help of Assistant, a Unity Artificial Intelligence product.
*/
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class IdleZombie : IState
{
    private readonly EnemyAnimator _enemyAnimator;
    private readonly NavMeshAgent _agent;

    public IdleZombie(EnemyAnimator enemyAnimator, NavMeshAgent agent)
    {
        _enemyAnimator = enemyAnimator;
        _agent = agent;
    }

    public void OnEnter()
    {
        // Ensure we are fully stopped and in idle pose
        if (_agent.isActiveAndEnabled)
            _agent.isStopped = true;
            
        _enemyAnimator.SetVelocity(0f);
    }

    public void Tick() { }
    public void OnExit() { }
}
