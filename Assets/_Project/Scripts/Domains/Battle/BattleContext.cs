using System;
using System.Collections.Generic;

public sealed class BattleContext : IBattleContext
{
    public bool IsFinished { get; set; }

    public PanelManager PanelManager { get; set; }

    public BattleGridController BattleGridController { get; set; }

    public BattleGridDragAndDropController BattleGridDragAndDropController { get; set; }

    public BattleQueueController BattleQueueController { get; set; }

    public BattleQueueUIController BattleQueueUIController { get; set; }

    public BattleActionControllerResolver BattleActionControllerResolver { get; set; }

    public BattleTacticUIController BattleTacticUIController { get; set; }

    public BattleCombatUIController BattleCombatUIController { get; set; }

    public BattleResultsUIController BattleResultsUIController { get; set; }

    public IReadOnlyList<BattleSquadController> BattleUnits { get; set; } = Array.Empty<BattleSquadController>();

    public IReadOnlySquadModel ActiveUnit { get; set; }

    public IBattleAction CurrentAction { get; set; }
}

