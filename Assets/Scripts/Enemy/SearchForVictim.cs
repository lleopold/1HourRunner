using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SearchForVictim : IState
{

    private readonly Enemy _enemy;
    private GameObject _victim;

    public SearchForVictim(Enemy enemy, GameObject victim)
    {
        _enemy = enemy;
        _victim = victim;
    }

    public void OnEnter()
    {
        //Debug.Log("SearchForVictim OnEnter");
    }

    public void OnExit()
    {
        Debug.Log("SearchForVictim OnExit");
    }

    public void Tick()
    {
        if (_victim != null)
        {
            _enemy.Target = _victim;
        }
    }

    void Start()
    {
        Debug.Log("SearchForVictim Start");
    }

    void Update()
    {
        Debug.Log("SearchForVictim Update");
    }
}
