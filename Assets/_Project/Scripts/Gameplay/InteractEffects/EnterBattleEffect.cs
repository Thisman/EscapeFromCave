using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Effects/EnterBattle")]
public sealed class EnterBattleEffect : EffectSO, IAsyncEffect
{
    public override void Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        _ = ApplyAsync(ctx, targets);
    }

    public async Task ApplyAsync(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        SceneLoader sceneLoader = ctx.SceneLoader;
        await sceneLoader.LoadAdditiveAsync("Battle", true);
    }
}
