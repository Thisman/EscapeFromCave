using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BattleResultHandler
{
    private const string MainMenuSceneName = "MainMenuScene";
    private const float DefaultBattleExperienceReward = 100f;

    public async Task ApplyResultAsync(InteractionContext ctx, BattleResult result)
    {
        if (ctx == null || result == null)
            return;

        if (result.Status == BattleResultStatus.Defeat)
        {
            await ReturnToMainMenuAsync(ctx);
            return;
        }

        if (result.Status != BattleResultStatus.Victory && result.Status != BattleResultStatus.Flee)
            return;

        UpdateHeroArmy(ctx.Actor, result.BattleUnitsResult.FriendlyUnits);
        RewardActorUnits(ctx.Actor);
    }

    private static async Task ReturnToMainMenuAsync(InteractionContext ctx)
    {
        var sceneLoader = ctx.SceneLoader;
        if (sceneLoader == null)
            return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid())
            await sceneLoader.UnloadAdditiveAsync(activeScene.name);

        await sceneLoader.LoadAdditiveAsync(MainMenuSceneName);
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
            ApplyCountsToSquad(playerSquad, heroUnit);
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
            var replacement = new SquadModel(heroUnit.Definition, heroUnit.Count, heroUnit.Experience);
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

        target.TrySetExperience(source.Experience);
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
                    var squad = new SquadModel(unit.Definition, unit.Count, unit.Experience);
                    armyController.Army.SetSlot(slot, squad);
                    continue;
                }
            }

            armyController.Army.ClearSlot(slot);
        }
    }

    private static void RewardActorUnits(GameObject actor)
    {
        if (actor == null)
            return;

        RewardHero(actor);
        RewardArmy(actor);
    }

    private static void RewardHero(GameObject actor)
    {
        if (actor.TryGetComponent<PlayerController>(out var playerController))
        {
            if (playerController.GetPlayer() is SquadModel heroSquad)
                heroSquad.TryAddExperience(DefaultBattleExperienceReward);

            return;
        }

        if (TryResolveSquadModel(actor, out var heroModel) && heroModel is SquadModel heroSquadModel)
            heroSquadModel.TryAddExperience(DefaultBattleExperienceReward);
    }

    private static void RewardArmy(GameObject actor)
    {
        if (!actor.TryGetComponent<PlayerArmyController>(out var armyController))
            return;

        if (armyController.Army is not ArmyModel armyModel)
            return;

        var squads = armyModel.GetSquads();
        if (squads == null)
            return;

        for (int i = 0; i < squads.Count; i++)
        {
            if (squads[i] is SquadModel squad)
                squad.TryAddExperience(DefaultBattleExperienceReward);
        }
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
}
