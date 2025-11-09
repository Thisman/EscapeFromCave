using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class InteractionController : MonoBehaviour
{
    [SerializeField] public InteractationDefinitionSO Definition;

    private CooldownState _cooldown;

    public async Task<bool> TryInteract(InteractionContext ctx)
    {
        if (!_cooldown.Ready(ctx.Time))
        {
            GameLogger.Warn($"[InteractionController] Interaction '{Definition.name}' on '{name}' is on cooldown. Remaining: {_cooldown.Remaining(ctx.Time):F2}s.");
            return false;
        }

        if (Definition.Conditions.Length != 0)
        {
            if (!Definition.Conditions.All(c => c.IsMet(ctx)))
            {
                GameLogger.Warn($"[InteractionController] Conditions for '{Definition.name}' failed for actor '{ctx.Actor.name ?? "<null>"}' on '{name}'.");
                return false;
            }
        }

        IReadOnlyList<GameObject> targets = Definition.TargetResolver.Resolve(ctx);
        int targetCount = targets?.Count ?? 0;
        if (targetCount == 0)
        {
            GameLogger.Warn($"[InteractionController] Target resolver '{Definition.TargetResolver.name}' resolved no targets for '{Definition.name}'.");
        }

        _cooldown.Start(ctx.Time, Definition.Cooldown);


        foreach (var eff in Definition.Effects)
        {
            var result = await eff.Apply(ctx, targets);
            if (result == EffectResult.Break)
            {
                break;
            }
        }

        return true;
    }
}
