using UnityEngine;
using UnityEngine.AI;

public class AttackFreely : IState
{
    private readonly Enemy _enemy;
    private GameObject _victim;
    private Animator _animator;
    private EnemyConfig _enemyConfig;
    private bool _attacking;
    private MonoBehaviour _monoBehaviour;
    private NavMeshAgent _navMeshAgent;
    private Coroutine _attackTimeoutCo;

    public AttackFreely(Enemy enemy, GameObject victim, Animator animator, EnemyConfig enemyConfig, MonoBehaviour monoBehaviour)
    {
        _enemy = enemy;
        _victim = victim;
        _animator = animator;
        _enemyConfig = enemyConfig;
        _monoBehaviour = monoBehaviour;
        _navMeshAgent = enemy.GetComponent<NavMeshAgent>();
    }

    public void OnEnter()
    {
        // Stop moving while we swing
        if (_navMeshAgent != null)
        {
            _navMeshAgent.isStopped = true;
            _navMeshAgent.updateRotation = false; // we rotate manually while attacking
        }

        if (_attacking) return;

        if (Random.Range(1, 3) == 1)
        {
            _animator.SetInteger("Kick", 1);
        }
        else
        {
            _animator.SetTrigger("ZombieAttack");
        }

        // Fail-safe: ensure we exit attack even if animation event “end” doesn’t fire
        _attackTimeoutCo = _monoBehaviour.StartCoroutine(AttackEndFailSafe(2.0f));

        _attacking = true;
    }

    public void OnExit()
    {
        _attacking = false;

        // Resume movement control
        if (_navMeshAgent != null)
        {
            _navMeshAgent.isStopped = false;
            _navMeshAgent.updateRotation = true;
        }

        // Clear attack params
        _animator.ResetTrigger("ZombieAttack");
        _animator.SetInteger("Kick", 0);

        if (_attackTimeoutCo != null)
        {
            _monoBehaviour.StopCoroutine(_attackTimeoutCo);
            _attackTimeoutCo = null;
        }
    }

    public void Tick()
    {
        // Face the player while attacking
        if (_victim != null)
        {
            Vector3 direction = _victim.transform.position - _enemy.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                _enemy.transform.rotation = Quaternion.Slerp(_enemy.transform.rotation, lookRotation, 10f * Time.deltaTime);
            }
        }
    }

    private System.Collections.IEnumerator AttackEndFailSafe(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (!_enemy.AttackFinished)
        {
            // If animation didn’t signal end, force it
            _enemy.AttackFinished = true;
        }
    }
}
