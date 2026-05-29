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
    private Coroutine _sequenceCo;

    // Telegraph timing tweakable
    private const float TelegraphDuration = 0.5f;

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
        if (_attacking) return;
        _attacking = true;
        _enemy.AttackFinished = false;

        if (_navMeshAgent != null)
        {
            _navMeshAgent.isStopped = true;
            _navMeshAgent.updateRotation = false;
        }

        // Start full attack sequence (telegraph -> animation)
        _sequenceCo = _monoBehaviour.StartCoroutine(AttackSequence());
    }

    public void OnExit()
    {
        _attacking = false;

        if (_attackTimeoutCo != null)
        {
            _monoBehaviour.StopCoroutine(_attackTimeoutCo);
            _attackTimeoutCo = null;
        }

        if (_sequenceCo != null)
        {
            _monoBehaviour.StopCoroutine(_sequenceCo);
            _sequenceCo = null;
        }

        // Hide indicator if still showing
        _enemy.AlertIndicator?.Cancel();

        // Reset animator params
        _animator.ResetTrigger("ZombieAttack");
        _animator.SetInteger("Kick", 0);
    }

    public void Tick()
    {
        // Rotation while attacking intentionally skipped (commented logic retained in original)
    }

    private System.Collections.IEnumerator AttackSequence()
    {
        bool useKick = (Random.Range(1, 3) == 1);

        bool canAttack = true;
        // Optional: you could add conditions here (e.g. still in melee range)
        if (!canAttack)
        {
            _enemy.AttackFinished = true;
            yield break;
        }

        // Telegraph phase
        if (_enemy.AlertIndicator != null)
        {
            bool telegraphDone = false;
            _enemy.AlertIndicator.Telegraph(_monoBehaviour, TelegraphDuration, () => telegraphDone = true);
            while (!telegraphDone)
                yield return null;
        }
        else
        {
            yield return new WaitForSeconds(TelegraphDuration);
        }

        // Play actual attack animation
        if (useKick)
        {
            _animator.SetInteger("Kick", 1);
        }
        else
        {
            _animator.SetTrigger("ZombieAttack");
        }

        // Fail-safe exit
        _attackTimeoutCo = _monoBehaviour.StartCoroutine(AttackEndFailSafe(2.0f));
    }

    private System.Collections.IEnumerator AttackEndFailSafe(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (!_enemy.AttackFinished)
        {
            _enemy.AttackFinished = true;
            Debug.Log("AttackEndFailSafe triggered");
        }
    }
}
