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

    private float _attackCooldown = 1.5f; // seconds
    private float _lastAttackTime = -999f;


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
        if (_attacking || Time.time < _lastAttackTime + _attackCooldown)
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
        _lastAttackTime = Time.time;
    }

    public void OnExit()
    {
        _attacking = false;
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
