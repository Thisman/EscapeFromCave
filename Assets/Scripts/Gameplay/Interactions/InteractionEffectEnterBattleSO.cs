using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "EnterBattleEffect", menuName = "Gameplay/Effects/Enter Battle")]
public sealed class InteractionEffectEnterBattleSO : InteractionEffectSO
{
    private const string BattleSceneName = "BattleScene";
    private const string MainMenuSceneName = "MainMenuScene";

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
        HandleBattleResult(ctx, battleResult);

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
            setup = new BattleSquadSetup(model.Definition, model.Count);
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
            if (unit?.Definition == null || unit.Count <= 0)
                continue;

            if (unit.Definition.IsHero())
            {
                heroUnit ??= unit;
                continue;
            }

            if (unit.Definition.IsAlly())
                armyUnits.Add(unit);
        }

        UpdateHeroSquad(actor, heroUnit);
        UpdateArmySquads(actor, armyUnits);
    }

    private static void UpdateHeroSquad(GameObject actor, IReadOnlySquadModel heroUnit)
    {
        if (heroUnit == null || heroUnit.Definition == null)
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

        var existing = controller.GetPlayer();

        if (existing is SquadModel existingModel && existingModel.Definition == heroUnit.Definition)
        {
            ApplyCountsToSquad(existingModel, heroUnit);
        }
        else
        {
            var replacement = new SquadModel(heroUnit.Definition, heroUnit.Count);
            controller.Initialize(replacement);
        }
    }

    private static void ApplyCountsToSquad(SquadModel target, IReadOnlySquadModel source)
    {
        if (target == null || source?.Definition == null)
            return;

        if (target.Definition != source.Definition)
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
                if (unit?.Definition != null && unit.Count > 0)
                {
                    var squad = new SquadModel(unit.Definition, unit.Count);
                    armyController.Army.SetSlot(slot, squad);
                    continue;
                }
            }

            armyController.Army.ClearSlot(slot);
        }
    }
}
