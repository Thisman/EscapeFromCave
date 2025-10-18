using UnityEngine;

public class BattleRoundState : State<BattleStateContext>
{
    private readonly PanelController _panelController;

    public BattleRoundState(PanelController panelController)
    {
        _panelController = panelController;
    }

    public override void Enter(BattleStateContext context)
    {
        Debug.Log("Entering Battle Round State");
        _panelController?.Show(nameof(BattleRoundState));
    }
}
