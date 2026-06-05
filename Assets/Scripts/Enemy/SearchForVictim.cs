using UnityEngine;

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
        //Debug.Log("SearchForVictim OnExit");
    }

    public void Tick()
    {
        if (_victim == null)
        {
            _victim = GameObject.FindWithTag("Player");
            if (_victim == null) _victim = GameObject.Find("Player");
        }

        if (_victim != null)
        {
            _enemy.Target = _victim;
            // Ensure the enemy's internal player reference is also set
            if (_enemy._player == null) _enemy._player = _victim;
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
