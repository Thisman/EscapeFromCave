using System;

public interface IBattleActionTargetPicker : IDisposable
{
    event Action<BattleSquadController> OnSelect;

    void RequestTarget();
}
