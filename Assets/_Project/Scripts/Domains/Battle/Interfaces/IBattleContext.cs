using System.Collections.Generic;

public interface IBattleContext
{
    bool IsFinished { get; set; }
    PanelManager PanelManager { get; set; }

    BattleGridController BattleGridController { get; set; }

    BattleGridDragAndDropController BattleGridDragAndDropController { get; set; }

    public BattleQueueController BattleQueueController { get; set; }

    public BattleQueueUIController BattleQueueUIController { get; set; }

    IReadOnlyList<BattleSquadController> BattleUnits { get; set; }

    IReadOnlySquadModel ActiveUnit { get; set; }

    IBattleAction CurrentAction { get; set; }
}
