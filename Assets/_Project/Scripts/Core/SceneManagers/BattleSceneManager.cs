using UnityEngine;
using VContainer;

public class BattleSceneManager : MonoBehaviour
{
    private async void Start()
    {
        var ctx = new BattleContext();
        var action = new ActionPipelineMachine(ctx);
        var combat = new CombatLoopMachine(ctx, action);
        var phase = new BattlePhaseMachine(ctx, combat);

        phase.Fire(BattleTrigger.Start);
    }
}
