using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "EnterBattleEffect", menuName = "Gameplay/Effects/Enter Battle")]
public sealed class InteractionEffectEnterBattleSO : InteractionEffectSO
{
    private const string BattleSceneName = "BattleScene";

    public override async Task<InteractionEffectResult> Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        if (ctx?.BattleSetupHandler == null || ctx.SceneLoader == null)
            return InteractionEffectResult.Break;

        BattleSceneData data = ctx.BattleSetupHandler.CreateBattleData(ctx, targets);
        if (data == null)
            return InteractionEffectResult.Break;

        var payload = new BattleScenePayload(data);

        var inputRouter = ctx.InputService;
        inputRouter?.EnterBattle();

        BattleResult battleResult = await ctx.SceneLoader
            .LoadAdditiveWithDataAsync<BattleSceneData, BattleResult>(BattleSceneName, payload);

        inputRouter?.EnterGameplay();

        if (ctx.BattleResultHandler != null)
            await ctx.BattleResultHandler.ApplyResultAsync(ctx, battleResult);

        return battleResult.IsVictory ? InteractionEffectResult.Continue : InteractionEffectResult.Break;
    }
}
