using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "EnterBattleEffect", menuName = "Gameplay/Effects/Enter Battle")]
public sealed class InteractionEffectEnterBattleSO : InteractionEffectSO
{
    private const string BattleSceneName = "BattleScene";

    public override async Task<InteractionEffectResult> Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        BattleSceneData data = ctx.BattleSetupHandler.CreateBattleData(ctx, targets);
        var payload = new BattleScenePayload(data);

        ctx.InputService.EnterBattle();

        // TODO: реализовать загрузку сцен через события RequestDialogShow
        BattleResult battleResult = await ctx.SceneLoader
            .LoadAdditiveWithDataAsync<BattleSceneData, BattleResult>(BattleSceneName, payload);

        ctx.InputService.EnterGameplay();

        await ctx.BattleResultHandler.ApplyResultAsync(ctx, battleResult);

        return battleResult.IsVictory ? InteractionEffectResult.Continue : InteractionEffectResult.Break;
    }
}
