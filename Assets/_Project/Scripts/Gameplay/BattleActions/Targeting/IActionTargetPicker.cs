using System;

public interface IActionTargetPicker : IDisposable
{
    event Action<BattleSquadController> OnSelect;

    void RequestTarget();
}
