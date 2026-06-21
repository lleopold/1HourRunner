using UnityEngine;
using UnityEngine.AI;

public class AttackFreely : IState
{
    private readonly Enemy _enemy;
    private readonly Animator _animator;
    private readonly EnemyConfig _enemyConfig;
    private readonly MonoBehaviour _monoBehaviour;
    private readonly NavMeshAgent _navMeshAgent;
    private readonly EnemyAnimator _enemyAnimator;

    private const float TelegraphDuration = 0.1f;
    private const float AttackTimeoutSeconds = 2.5f;
    private const float AttackTrackSpeed = 720f; // deg/s the zombie keeps facing the player mid-swing (high so a close-circling player can't outrun it)

    private bool _attacking;
    private Coroutine _attackTimeoutCo;
    private Coroutine _sequenceCo;

    public AttackFreely(Enemy enemy, GameObject victim, Animator animator, EnemyConfig enemyConfig, MonoBehaviour monoBehaviour, EnemyAnimator enemyAnimator)
    {
        _enemy = enemy;
        _animator = animator;
        _enemyConfig = enemyConfig;
        _monoBehaviour = monoBehaviour;
        _enemyAnimator = enemyAnimator;
        _navMeshAgent = enemy.GetComponent<NavMeshAgent>();
    }

    public void OnEnter()
    {
        if (_attacking) return;
        _attacking = true;
        _enemy.AttackFinished = false;

        if (_navMeshAgent != null)
        {
            _navMeshAgent.ResetPath();         // clear destination so agent has nowhere to slide toward
            _navMeshAgent.isStopped = true;
            _navMeshAgent.updateRotation = false;
        }

        _sequenceCo = _monoBehaviour.StartCoroutine(AttackSequence());
    }

    public void OnExit()
    {
        _attacking = false;

        if (_attackTimeoutCo != null) { _monoBehaviour.StopCoroutine(_attackTimeoutCo); _attackTimeoutCo = null; }
        if (_sequenceCo != null) { _monoBehaviour.StopCoroutine(_sequenceCo); _sequenceCo = null; }

        _animator.ResetTrigger("ZombieAttack");
        _animator.SetInteger("Kick", 0);

        _enemy.AlertIndicator?.Cancel();
    }

    public void Tick()
    {
        if (_navMeshAgent != null)
            _navMeshAgent.isStopped = true;

        _enemyAnimator.SetVelocity(0f);

        // Keep facing the player during the swing so the hit lands; agent rotation is off here,
        // so we drive it manually. A fast player can still flank by leaving melee range.
        var player = _enemy._player;
        if (player != null)
        {
            Vector3 toPlayer = player.transform.position - _enemy.transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.001f)
            {
                Quaternion target = Quaternion.LookRotation(toPlayer);
                _enemy.transform.rotation = Quaternion.RotateTowards(
                    _enemy.transform.rotation, target, AttackTrackSpeed * Time.deltaTime);
            }
        }
    }

    private System.Collections.IEnumerator AttackSequence()
    {
        _animator.speed = 2f;
        // Wait until zombie has fully stopped
        while (_enemyAnimator.Velocity > 0.05f)
            yield return null;

        // Telegraph
        if (_enemy.AlertIndicator != null)
        {
            bool done = false;
            _enemy.AlertIndicator.Telegraph(_monoBehaviour, TelegraphDuration, () => done = true);
            while (!done) yield return null;
        }
        else
        {
            yield return new WaitForSeconds(TelegraphDuration);
        }

        // Pick attack type and play it
        bool useKick = (Random.Range(0, 2) == 0);
        if (useKick)
            _animator.SetInteger("Kick", 1);
        else
            _animator.SetTrigger("ZombieAttack");

        // AttackFinished set by EnemyKicking / EnemyCrossPunch StateMachineBehaviour OnStateExit.
        // Failsafe in case animator event never fires.
        _attackTimeoutCo = _monoBehaviour.StartCoroutine(AttackEndFailSafe(AttackTimeoutSeconds));
    }

    private System.Collections.IEnumerator AttackEndFailSafe(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (!_enemy.AttackFinished)
        {
            Debug.LogWarning("[AttackFreely] Failsafe triggered — AttackFinished was never set by animator.");
            _enemy.AttackFinished = true;
        }
    }
}
