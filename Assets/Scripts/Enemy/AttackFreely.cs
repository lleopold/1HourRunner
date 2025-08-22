using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackFreely : IState
{
    private readonly Enemy _enemy;
    private GameObject _victim;
    private Animator _animator;
    private EnemyConfig _enemyConfig;
    private float _nextAttack;
    private bool _attacking;
    private MonoBehaviour _monoBehaviour;


    public AttackFreely(Enemy enemy, GameObject victim, Animator animator, EnemyConfig enemyConfig, MonoBehaviour monoBehaviour)
    {
        _enemy = enemy;
        _victim = victim;
        _animator = animator;
        this._enemyConfig = enemyConfig;
        _monoBehaviour = monoBehaviour;

    }

    public void OnEnter()
    {
        if (_attacking)
            return;

        if (Random.Range(1, 3) == 1)
        {
            _animator.SetInteger("Kick", 1);
        }
        else
        {
            _animator.SetTrigger("ZombieAttack");
        }
        _attacking = true;
        //_enemy.AttackFinished = true; //For people with cheaper tickets

        //if (_victim != null)
        //{
        //    // Check if it's time for the next attack and the enemy is not already attacking
        //    if (!_attacking && Time.time >= _nextAttack)
        //    {
        //        _animator.SetInteger("Attack", 3);
        //        _attacking = true;
        //    }
        //}
        //Debug.Log("AttackFreely OnEnter");
    }

    public void OnExit()
    {
        //Debug.Log("AttackFreely OnExit");
    }


    public void Tick()
    {

    }
    void Start()
    {
        Debug.Log("AttackFreely Start");
    }

    void Update()
    {
        Debug.Log("AttackFreely Update");
    }
}
