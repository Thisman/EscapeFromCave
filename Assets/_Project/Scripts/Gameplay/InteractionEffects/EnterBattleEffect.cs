using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Effects/EnterBattle")]
public sealed class EnterBattleEffect : EffectSO
{
    private const string BattleSceneName = "BattleScene";
    private const string MainMenuSceneName = "MainMenuScene";

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

        var inputRouter = ctx.InputRouter;
        inputRouter?.EnterBattle();

        BattleResult battleResult = default;
        bool battleCompleted = false;

        try
        {
            battleResult = await ctx.SceneLoader
                .LoadAdditiveWithDataAsync<BattleSceneData, BattleResult>(BattleSceneName, payload);
            battleCompleted = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EnterBattleEffect] Battle scene session failed: {ex}");
        }
        finally
        {
            inputRouter?.EnterGameplay();
        }

        if (battleCompleted)
        {
            HandleBattleResult(ctx, battleResult);
        }
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

    private static void HandleBattleResult(InteractionContext ctx, BattleResult result)
    {
        if (ctx == null)
            return;

        if (result.Status == BattleResultStatus.Defeat)
        {
            ctx.SceneLoader?.LoadScene(MainMenuSceneName);
            return;
        }

        if (result.Status != BattleResultStatus.Victory && result.Status != BattleResultStatus.Flee)
            return;

        UpdateHeroArmy(ctx.Actor, result.BattleUnitsResult.FriendlyUnits);
    }

    private static void UpdateHeroArmy(GameObject actor, IReadOnlyList<IReadOnlySquadModel> friendlyUnits)
    {
        if (actor == null || friendlyUnits == null || friendlyUnits.Count == 0)
            return;

        IReadOnlySquadModel heroUnit = null;
        var armyUnits = new List<IReadOnlySquadModel>();

        for (int i = 0; i < friendlyUnits.Count; i++)
        {
            var unit = friendlyUnits[i];
            if (unit?.UnitDefinition == null || unit.Count <= 0)
                continue;

            var type = unit.UnitDefinition.Type;

            if (type == UnitType.Hero)
            {
                if (heroUnit == null)
                    heroUnit = unit;
                continue;
            }

            if (type == UnitType.Ally)
                armyUnits.Add(unit);
        }

        UpdateHeroSquad(actor, heroUnit);
        UpdateArmySquads(actor, armyUnits);
    }

    private static void UpdateHeroSquad(GameObject actor, IReadOnlySquadModel heroUnit)
    {
        if (heroUnit == null || heroUnit.UnitDefinition == null)
            return;

        if (actor.TryGetComponent<PlayerController>(out var playerController))
        {
            ApplySquadToPlayer(playerController, heroUnit);
            return;
        }

        if (TryResolveSquadModel(actor, out var squadModel) && squadModel is SquadModel playerSquad)
        {
            ApplyCountsToSquad(playerSquad, heroUnit);
        }
    }

    private static void ApplySquadToPlayer(PlayerController controller, IReadOnlySquadModel heroUnit)
    {
        if (controller == null)
            return;

        var existing = controller.GetPlayerSquad();

        if (existing is SquadModel existingModel && existingModel.UnitDefinition == heroUnit.UnitDefinition)
        {
            ApplyCountsToSquad(existingModel, heroUnit);
        }
        else
        {
            var replacement = new SquadModel(heroUnit.UnitDefinition, heroUnit.Count);
            controller.Initialize(replacement);
        }
    }

    private static void ApplyCountsToSquad(SquadModel target, IReadOnlySquadModel source)
    {
        if (target == null || source?.UnitDefinition == null)
            return;

        if (target.UnitDefinition != source.UnitDefinition)
            return;

        target.Clear();
        if (source.Count > 0)
            target.TryAdd(source.Count);
    }

    private static void UpdateArmySquads(GameObject actor, List<IReadOnlySquadModel> units)
    {
        if (actor == null || units == null)
            return;

        if (!actor.TryGetComponent<PlayerArmyController>(out var armyController))
            return;

        int maxSlots = armyController.MaxSlots;
        int unitIndex = 0;

        for (int slot = 0; slot < maxSlots; slot++)
        {
            if (unitIndex < units.Count)
            {
                var unit = units[unitIndex++];
                if (unit?.UnitDefinition != null && unit.Count > 0)
                {
                    var squad = new SquadModel(unit.UnitDefinition, unit.Count);
                    armyController.SetSlot(slot, squad);
                    continue;
                }
            }

            armyController.ClearSlot(slot);
        }
    }

}
