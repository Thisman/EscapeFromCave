using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "EnterBattleEffect", menuName = "Gameplay/Effects/Enter Battle")]
public sealed class InteractionEffectEnterBattleSO : InteractionEffectSO
{
    private const string BattleSceneName = "BattleScene";

    public override async Task<InteractionEffectResult> Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        BattleSquadSetup heroSetup = ResolveHero(ctx.Actor);
        List<BattleSquadSetup> armySetup = ResolveArmy(ctx.Actor);
        GameObject enemyObject = ResolveEnemyObject(ctx, targets);
        List<BattleSquadSetup> enemiesSetup = ResolveEnemy(enemyObject);

        var data = new BattleSceneData(heroSetup, armySetup, enemiesSetup, ctx.Actor, enemyObject);
        var payload = new BattleScenePayload(data);

        var inputRouter = ctx.InputService;
        inputRouter?.EnterBattle();

        BattleResult battleResult = await ctx.SceneLoader
            .LoadAdditiveWithDataAsync<BattleSceneData, BattleResult>(BattleSceneName, payload);

        inputRouter?.EnterGameplay();

        if (ctx?.BattleResultHandler != null)
            await ctx.BattleResultHandler.ApplyResultAsync(ctx, battleResult);

        return battleResult.IsVictory ? InteractionEffectResult.Continue : InteractionEffectResult.Break;
    }

    private static BattleSquadSetup ResolveHero(GameObject actor)
    {
        if (actor == null)
            return default;

        if (actor.TryGetComponent<PlayerController>(out var playerController))
        {
            if (TryCreateSetup(playerController.GetPlayer(), out var setup))
                return setup;
        }

        if (TryResolveSquadModel(actor, out var squadModel) && TryCreateSetup(squadModel, out var fallbackSetup))
            return fallbackSetup;

        return default;
    }

    private static List<BattleSquadSetup> ResolveArmy(GameObject actor)
    {
        var result = new List<BattleSquadSetup>();

        if (actor == null)
            return result;

        if (actor.TryGetComponent<PlayerArmyController>(out var armyController))
        {
            foreach (var squad in armyController.Army.GetSquads())
            {
                if (TryCreateSetup(squad, out var setup))
                    result.Add(setup);
            }
        }

        return result;
    }

    private static GameObject ResolveEnemyObject(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        if (ctx?.Target != null)
            return ctx.Target;

        if (targets != null)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var candidate = targets[i];
                if (candidate != null && candidate != ctx?.Actor)
                    return candidate;
            }
        }

        return null;
    }

    private static List<BattleSquadSetup> ResolveEnemy(GameObject enemy)
    {
        List<BattleSquadSetup> enemies = new();

        if (TryResolveSquadModel(enemy, out var squadModel) && TryCreateSetup(squadModel, out var setup))
            enemies.Add(setup);

        AdditionalSquad[] additionalSquads = enemy.GetComponent<SquadController>().GetAdditionalSquads();
        for (int i = 0; i < additionalSquads.Length; i++)
        {
            AdditionalSquad additinalSquad = additionalSquads[i];
            if (TryCreateSetup(additinalSquad, out var additionalSetup))
            {
                enemies.Add(additionalSetup);
            }
        }

        return enemies;
    }

    private static bool TryResolveSquadModel(GameObject source, out IReadOnlySquadModel model)
    {
        model = null;
        if (source == null)
            return false;

        if (TryGetModelFromComponent(source.GetComponentInParent<BattleSquadController>()?.GetSquadModel(), out model))
            return true;

        if (TryGetModelFromComponent(source.GetComponentInParent<SquadController>()?.GetSquadModel(), out model))
            return true;

        if (TryGetModelFromComponent(source.GetComponentInParent<PlayerController>()?.GetPlayer(), out model))
            return true;

        return false;
    }

    private static bool TryGetModelFromComponent(IReadOnlySquadModel candidate, out IReadOnlySquadModel model)
    {
        model = candidate;
        return model != null;
    }

    private static bool TryCreateSetup(IReadOnlySquadModel model, out BattleSquadSetup setup)
    {
        if (model != null && model.Count > 0)
        {
            setup = new BattleSquadSetup(model.Definition, model.Count, model.Experience);
            return true;
        }

        setup = default;
        return false;
    }

    private static bool TryCreateSetup(AdditionalSquad additionalSquad, out BattleSquadSetup setup)
    {
        setup = new BattleSquadSetup(additionalSquad.Definition, additionalSquad.Count);
        return true;
    }

}
