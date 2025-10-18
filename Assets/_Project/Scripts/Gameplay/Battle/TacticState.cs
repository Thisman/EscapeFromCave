using UnityEngine;

public class TacticState : State<BattleStateContext>
{
    private readonly PanelController _panelController;

    public TacticState(PanelController panelController)
    {
        _panelController = panelController;
    }

    public override void Enter(BattleStateContext context)
    {
        base.Enter(context);

        if (_panelController == null)
        {
            Debug.LogWarning("[TacticState] PanelController is not available");
            return;
        }

        _panelController.Show(nameof(TacticState));
        Debug.Log("TacticState Entered");
    }
}
