using System.Collections.Generic;
using UnityEngine;

public sealed class BattleSetupHandler
{
    public BattleSceneData CreateBattleData(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        if (ctx == null)
            return null;

        var actor = ctx.Actor;
        BattleSquadSetup heroSetup = ResolveHero(actor);
        List<BattleSquadSetup> armySetup = ResolveArmy(actor);
        GameObject enemyObject = ResolveEnemyObject(ctx, targets);
        List<BattleSquadSetup> enemiesSetup = ResolveEnemy(enemyObject);

        return new BattleSceneData(heroSetup, armySetup, enemiesSetup, actor, enemyObject);
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

        if (enemy == null)
            return enemies;

        if (TryResolveSquadModel(enemy, out var squadModel) && TryCreateSetup(squadModel, out var setup))
            enemies.Add(setup);

        var squadController = enemy.GetComponent<SquadController>();
        if (squadController == null)
            return enemies;

        AdditionalSquad[] additionalSquads = squadController.GetAdditionalSquads();
        for (int i = 0; i < additionalSquads.Length; i++)
        {
            AdditionalSquad additionalSquad = additionalSquads[i];
            if (TryCreateSetup(additionalSquad, out var additionalSetup))
                enemies.Add(additionalSetup);
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
