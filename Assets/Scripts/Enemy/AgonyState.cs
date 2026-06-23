using UnityEngine;

/// <summary>
/// Receiver state. A zombie caught by a nearby scream gets _pendingAgony set; the
/// AddAnyTransition(() => _pendingAgony) pulls it here. Plays Zagony; the boost + red VFX
/// fire from an animation event on the Zagony clip (Enemy.OnAgonyEnd). Thin by design.
/// </summary>
public class AgonyState : IState
{
    private readonly Enemy _enemy;

    public AgonyState(Enemy enemy)
    {
        _enemy = enemy;
    }

    public void OnEnter() => _enemy.BeginAgony();
    public void Tick()    => _enemy.TickAgony();
    public void OnExit()  => _enemy.EndAgonyState();
}
