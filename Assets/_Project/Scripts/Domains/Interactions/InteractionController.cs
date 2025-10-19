using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class InteractionController : MonoBehaviour
{
    [SerializeField] public InteractationDefinitionSO Definition;

    private CooldownState _cooldown;

    public bool TryInteract(InteractionContext ctx)
    {
        if (Definition == null) return false;
        if (!_cooldown.Ready(ctx.Time)) return false;
        if (Definition.Conditions.Length != 0)
        {
            if (!Definition.Conditions.All(c => c.IsMet(ctx))) return false;
        }

        var targets = Definition.TargetResolver.Resolve(ctx);

        foreach (var eff in Definition.Effects) eff.Apply(ctx, targets);

        _cooldown.Start(ctx.Time, Definition.Cooldown);
        return true;
    }
}
