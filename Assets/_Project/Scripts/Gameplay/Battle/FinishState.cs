using UnityEngine;

public class FinishState : State<BattleStateContext>
{
    private readonly PanelController _panelController;

    public FinishState(PanelController panelController)
    {
        _panelController = panelController;
    }

    public override void Enter(BattleStateContext context)
    {
        base.Enter(context);

        if (_panelController == null)
        {
            Debug.LogWarning("[FinishState] PanelController is not available");
            return;
        }

        _panelController.Show(nameof(FinishState));
    }
}
