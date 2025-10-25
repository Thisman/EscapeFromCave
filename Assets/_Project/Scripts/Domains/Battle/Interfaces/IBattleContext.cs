using System.Collections.Generic;

public interface IBattleContext
{
    bool IsFinished { get; set; }

    PanelManager PanelManager { get; set; }

    BattleGridController BattleGridController { get; set; }

    BattleGridDragAndDropController BattleGridDragAndDropController { get; set; }

    BattleQueueController BattleQueueController { get; set; }

    BattleQueueUIController BattleQueueUIController { get; set; }

    BattleActionControllerResolver BattleActionControllerResolver { get; set; }

    BattleTacticUIController BattleTacticUIController { get; set; }

    BattleCombatUIController BattleCombatUIController { get; set; }

    BattleResultsUIController BattleResultsUIController { get; set; }

    IReadOnlyList<BattleSquadController> BattleUnits { get; set; }

    IReadOnlySquadModel ActiveUnit { get; set; }

    IBattleAction CurrentAction { get; set; }
}
