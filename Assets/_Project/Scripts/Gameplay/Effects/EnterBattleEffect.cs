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
            Debug.LogWarning("[EnterBattleEffect] Interaction context is null. Unable to enter battle.");
            return;
        }

        if (ctx.SceneLoader == null)
        {
            Debug.LogWarning("[EnterBattleEffect] SceneLoader is missing in the interaction context. Unable to load battle scene.");
            return;
        }

        var player = ctx.Actor;
        if (player == null)
        {
            Debug.LogWarning("[EnterBattleEffect] Actor is missing in the interaction context. Unable to determine player for battle.");
            return;
        }

        var enemy = ResolveEnemy(ctx, targets);
        if (enemy == null)
        {
            Debug.LogWarning("[EnterBattleEffect] Failed to resolve enemy target for the battle scene.");
            return;
        }

        if (!TryGetArmy(player, out var army))
        {
            Debug.LogWarning($"[EnterBattleEffect] Player '{player.name}' does not provide an army. Battle scene will not be loaded.");
            return;
        }

        var payload = new BattleScenePayload(player, army, enemy);

        var inputRouter = ctx.InputRouter;
        inputRouter?.EnterBattle();

        try
        {
            await ctx.SceneLoader.LoadAdditiveWithDataAsync<BattleSceneData, object>(BattleSceneName, payload);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EnterBattleEffect] Failed to load battle scene '{BattleSceneName}'. {ex}");
            throw;
        }
        finally
        {
            inputRouter?.EnterGameplay();
        }
    }

    private static bool TryGetArmy(GameObject player, out IReadOnlyArmyModel army)
    {
        army = null;

        if (player == null)
        {
            return false;
        }

        if (!player.TryGetComponent(out PlayerArmyController armyController))
        {
            return false;
        }

        army = armyController.Army;
        return army != null;
    }

    private static GameObject ResolveEnemy(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        if (targets != null)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var candidate = targets[i];
                if (candidate != null)
                {
                    return candidate;
                }
            }
        }

        return ctx?.Target;
    }
}
