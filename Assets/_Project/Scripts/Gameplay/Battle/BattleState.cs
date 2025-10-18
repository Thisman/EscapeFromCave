using UnityEngine;

public abstract class BattleState : State<BattleStateContext>
{
    protected PanelController PanelController { get; }

    protected BattleState(PanelController panelController)
    {
        PanelController = panelController;
    }

    public override void Enter(BattleStateContext context)
    {
        base.Enter(context);
        ShowLayer(nameof(BattleState));
    }

    protected void ShowLayer(string layerName)
    {
        if (PanelController == null)
        {
            Debug.LogWarning($"[{GetType().Name}] PanelController is not available");
            return;
        }

        PanelController.Show(layerName);
    }
}
