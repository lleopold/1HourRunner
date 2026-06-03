using UnityEngine;
using UnityEngine.AI;

public class TurnToPlayer : IState
{
    private readonly Enemy _enemy;
    private readonly NavMeshAgent _navMeshAgent;
    private readonly Animator _animator;
    private readonly EnemyConfig _enemyConfig;

    private const float TurnSpeed = 360f;       // degrees per second
    private const float FacingThreshold = 15f;  // degrees — close enough to "facing"

    public bool CanExit { get; private set; }

    private static readonly int VelocityHash = Animator.StringToHash("velocity");

    public TurnToPlayer(Enemy enemy, NavMeshAgent navMeshAgent, Animator animator, EnemyConfig enemyConfig)
    {
        _enemy = enemy;
        _navMeshAgent = navMeshAgent;
        _animator = animator;
        _enemyConfig = enemyConfig;
    }

    public void OnEnter()
    {
        CanExit = false;

        if (_navMeshAgent.enabled)
        {
            _navMeshAgent.isStopped = true;
            _navMeshAgent.updateRotation = false;
        }

        _animator.SetFloat(VelocityHash, 0f);
    }

    public void OnExit()
    {
        if (_navMeshAgent.enabled)
            _navMeshAgent.updateRotation = true;
    }

    public void Tick()
    {
        if (_enemy._player == null)
        {
            CanExit = true;
            return;
        }

        Vector3 toPlayer = _enemy._player.transform.position - _enemy.transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.001f)
        {
            CanExit = true;
            return;
        }

        Quaternion targetRot = Quaternion.LookRotation(toPlayer);
        _enemy.transform.rotation = Quaternion.RotateTowards(
            _enemy.transform.rotation,
            targetRot,
            TurnSpeed * Time.deltaTime);

        float angle = Quaternion.Angle(_enemy.transform.rotation, targetRot);
        if (angle < FacingThreshold)
            CanExit = true;
    }
}
