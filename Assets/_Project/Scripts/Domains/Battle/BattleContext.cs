public sealed class BattleContext : IBattleContext
{
    public bool IsFinished { get; set; }

    public PanelController PanelController { get; set; }

    public BattleGridController BattleGridController { get; set; }

    public BattleGridDragAndDropController BattleGridDragAndDropController { get; set; }
}

