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
        if (Definition == null)
        {
            Debug.LogWarning($"[InteractionController] '{name}' does not have an interaction definition assigned. Actor: {ctx.Actor?.name ?? "<null>"}.");
            return false;
        }

        if (!_cooldown.Ready(ctx.Time))
        {
            Debug.LogWarning($"[InteractionController] Interaction '{Definition.name}' on '{name}' is on cooldown. Remaining: {_cooldown.Remaining(ctx.Time):F2}s.");
            return false;
        }

        if (Definition.Conditions.Length != 0)
        {
            if (!Definition.Conditions.All(c => c.IsMet(ctx)))
            {
                Debug.LogWarning($"[InteractionController] Conditions for '{Definition.name}' failed for actor '{ctx.Actor?.name ?? "<null>"}' on '{name}'.");
                return false;
            }
        }

        if (Definition.TargetResolver == null)
        {
            Debug.LogError($"[InteractionController] Definition '{Definition.name}' on '{name}' is missing a target resolver.");
            return false;
        }

        IReadOnlyList<GameObject> targets = Definition.TargetResolver.Resolve(ctx);
        int targetCount = targets?.Count ?? 0;
        if (targetCount == 0)
        {
            Debug.LogWarning($"[InteractionController] Target resolver '{Definition.TargetResolver.name}' resolved no targets for '{Definition.name}'.");
        }

        _cooldown.Start(ctx.Time, Definition.Cooldown);

        int restartAttempts = 0;
        while (true)
        {
            bool shouldRestart = false;

            foreach (var eff in Definition.Effects)
            {
                if (eff == null)
                {
                    Debug.LogWarning($"[InteractionController] '{Definition.name}' has a null effect reference on '{name}'.");
                    continue;
                }

                var result = await eff.Apply(ctx, targets);
                if (result == EffectResult.Restart)
                {
                    restartAttempts++;
                    Debug.Log($"[InteractionController] Effect '{eff.name}' requested restart for interaction '{Definition.name}' on '{name}'. Attempt: {restartAttempts}.");
                    shouldRestart = true;
                    break;
                }
            }

            if (!shouldRestart)
            {
                break;
            }
        }

        Debug.Log($"[InteractionController] Interaction '{Definition.name}' executed by '{ctx.Actor?.name ?? "<null>"}' on '{name}'. Targets affected: {targetCount}.");
        return true;
    }
}
