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
        if (ctx == null)
            throw new System.ArgumentNullException(nameof(ctx));

        SceneLoader sceneLoader = ctx.SceneLoader;
        if (sceneLoader == null)
        {
            Debug.LogWarning("[EnterBattleEffect] SceneLoader is not available in interaction context");
            return;
        }

        var hero = ctx.Actor != null ? ctx.Actor.GetComponent<PlayerController>()?.GetPlayerModel() : null;
        var army = ctx.Actor != null ? ctx.Actor.GetComponent<PlayerArmyController>()?.Army : null;
        var enemyModel = TryGetEnemyModel(targets);

        if (hero == null || army == null || enemyModel == null)
        {
            Debug.LogWarning("[EnterBattleEffect] Failed to gather battle data (hero/army/enemy)");
            return;
        }

        var payload = new BattleSceneData(hero, army, enemyModel);
        await sceneLoader.LoadAdditiveWithDataAsync<BattleSceneData, object>("Battle", payload, true);
    }

    private static UnitModel TryGetEnemyModel(IReadOnlyList<GameObject> targets)
    {
        if (targets == null || targets.Count == 0)
            return null;

        foreach (var go in targets)
        {
            if (go == null)
                continue;

            var enemy = go.GetComponent<EnemyController>();
            if (enemy != null)
                return enemy.GetEnemyModel();
        }

        return null;
    }
}
