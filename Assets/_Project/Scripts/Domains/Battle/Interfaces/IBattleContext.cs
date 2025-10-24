using System.Collections.Generic;

public interface IBattleContext
{
    bool IsFinished { get; set; }
    PanelController PanelController { get; set; }

    BattleGridController BattleGridController { get; set; }

    BattleGridDragAndDropController BattleGridDragAndDropController { get; set; }

    public BattleQueueController BattleQueueController { get; set; }

    public BattleQueueUIController BattleQueueUIController { get; set; }

    IReadOnlyList<BattleUnitController> BattleUnits { get; set; }

    IReadOnlyUnitModel ActiveUnit { get; set; }

    IBattleAction CurrentAction { get; set; }
}
