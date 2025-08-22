using UnityEngine;

public class SearchForGathering : IState
{

    private readonly Enemy _enemy;
    private GameObject _gath;

    public SearchForGathering(Enemy enemy, GameObject gath)
    {
        _enemy = enemy;
        _gath = gath;
    }

    public void OnEnter()
    {
    }

    public void OnExit()
    {
        Debug.Log("SearchForGathering OnExit: " + _gath.name);
    }

    public void Tick()
    {
        if (_gath != null)
        {
            _enemy.Target = _gath;
        }
    }

    void Start()
    {
    }

    void Update()
    {
    }
}
