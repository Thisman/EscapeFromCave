using System;
using System.Collections.Generic;

public sealed class BattleContext : IBattleContext
{
    public bool IsFinished { get; set; }

    public PanelController PanelController { get; set; }

    public BattleGridController BattleGridController { get; set; }

    public BattleGridDragAndDropController BattleGridDragAndDropController { get; set; }

    public BattleQueueController BattleQueueController { get; set; }

    public BattleQueueUIController BattleQueueUIController { get; set; }

    public IReadOnlyList<BattleUnitController> BattleUnits { get; set; } = Array.Empty<BattleUnitController>();
}

