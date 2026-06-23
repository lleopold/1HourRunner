using UnityEngine;

/// <summary>
/// Screamer state. Entered via AddAnyTransition(ScreamTrigger) on the StateMachine, so it
/// can fire from any normal AI state. Plays ZScream -> Zagonizing; the blast itself fires
/// from an animation event on the Zagonizing clip (Enemy.OnScreamBlast). All behaviour lives
/// on Enemy so the state stays thin (matches the IdleZombie pattern).
/// </summary>
public class ScreamState : IState
{
    private readonly Enemy _enemy;

    public ScreamState(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter() => _enemy.BeginScream();
    public void Tick()    => _enemy.TickScream();
    public void OnExit()  => _enemy.EndScreamState();
}
