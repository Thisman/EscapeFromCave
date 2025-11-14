using System;
using System.Collections.Generic;

public sealed class BattleContext
{
    public bool IsFinished { get; set; }

    public BattleGridController BattleGridController { get; set; }

    public BattleGridDragAndDropController BattleGridDragAndDropController { get; set; }

    public BattleQueueController BattleQueueController { get; set; }

    public BattleActionControllerResolver BattleActionControllerResolver { get; set; }

    public BattleUIController BattleUIController { get; set; }

    public IReadOnlyList<BattleSquadController> BattleUnits { get; set; } = Array.Empty<BattleSquadController>();

    public IReadOnlySquadModel ActiveUnit { get; set; }

    public BattleAbilityManager BattleAbilitiesManager { get; set; }

    public BattleEffectsManager BattleEffectsManager { get; set; }

    public IBattleAction CurrentAction { get; set; }

    public BattleResult BattleResult { get; set; }

    public InputService InputService { get; set; }

    public ISet<IReadOnlySquadModel> DefendedUnitsThisRound { get; } = new HashSet<IReadOnlySquadModel>();
}
