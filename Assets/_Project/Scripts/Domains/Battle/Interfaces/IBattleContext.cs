public interface IBattleContext
{
    bool IsFinished { get; set; }
    PanelController PanelController { get; set; }

    BattleGridController BattleGridController { get; set; }

    BattleGridDragAndDropController BattleGridDragAndDropController { get; set; }
}
