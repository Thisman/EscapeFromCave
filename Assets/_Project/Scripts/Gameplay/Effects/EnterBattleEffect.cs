using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Effects/EnterBattle")]
public sealed class EnterBattleEffect : EffectSO
{
    private const string BattleSceneName = "BattleScene";

    public override async Task Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        if (ctx == null)
        {
            Debug.LogWarning("[EnterBattleEffect] Interaction context is null. Unable to start battle.");
            return;
        }

        if (ctx.SceneLoader == null)
        {
            Debug.LogWarning("[EnterBattleEffect] SceneLoader is not available in the interaction context. Battle scene cannot be loaded.");
            return;
        }

        var heroSetup = ResolveHero(ctx.Actor);
        var armySetups = ResolveArmy(ctx.Actor);
        var enemyObject = ResolveEnemyObject(ctx, targets);
        var enemySetup = ResolveEnemy(enemyObject);

        if (!heroSetup.IsValid && armySetups.Count == 0)
        {
            Debug.LogWarning("[EnterBattleEffect] No hero or army data found for battle. Aborting battle start.");
            return;
        }

        if (!enemySetup.IsValid)
        {
            Debug.LogWarning("[EnterBattleEffect] No enemy data found for battle. Aborting battle start.");
            return;
        }

        var data = new BattleSceneData(heroSetup, armySetups, enemySetup, ctx.Actor, enemyObject);
        var payload = new BattleScenePayload(data);

        ctx.InputRouter?.EnterBattle();

        var loadTask = ctx.SceneLoader.LoadAdditiveWithDataAsync<BattleSceneData, object>(BattleSceneName, payload);
        _ = HandleLoadTaskAsync(loadTask);
        await Task.CompletedTask;
    }

    private static BattleSquadSetup ResolveHero(GameObject actor)
    {
        if (actor == null)
            return default;

        if (actor.TryGetComponent<PlayerController>(out var playerController))
        {
            if (TryCreateSetup(playerController.GetPlayerSquad(), out var setup))
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
            foreach (var squad in armyController.GetSquads())
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

    private static BattleSquadSetup ResolveEnemy(GameObject enemy)
    {
        if (enemy == null)
            return default;

        if (TryResolveSquadModel(enemy, out var squadModel) && TryCreateSetup(squadModel, out var setup))
            return setup;

        return default;
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

        if (TryGetModelFromComponent(source.GetComponentInParent<PlayerController>()?.GetPlayerSquad(), out model))
            return true;

        return false;
    }

    private static bool TryGetModelFromComponent(IReadOnlySquadModel candidate, out IReadOnlySquadModel model)
    {
        model = candidate;
        return model != null && model.UnitDefinition != null;
    }

    private static bool TryCreateSetup(IReadOnlySquadModel model, out BattleSquadSetup setup)
    {
        if (model != null && model.UnitDefinition != null && model.Count > 0)
        {
            setup = new BattleSquadSetup(model.UnitDefinition, model.Count);
            return true;
        }

        setup = default;
        return false;
    }

    private static async Task HandleLoadTaskAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EnterBattleEffect] Battle scene load task failed: {ex}");
        }
    }
}
