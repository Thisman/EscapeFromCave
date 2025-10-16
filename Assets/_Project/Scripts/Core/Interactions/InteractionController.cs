using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class InteractionController : MonoBehaviour, IInteractable
{
    [SerializeField] private InteractableDefinitionSO definition;
    private CooldownState _cooldown;

    public InteractableInfo GetInfo() => definition.Info;

    public bool TryInteract(InteractionContext ctx)
    {
        if (!_cooldown.Ready(ctx.Time)) return false;
        if (!definition.Conditions.All(c => c.IsMet(ctx))) return false;

        var targets = definition.TargetResolver.Resolve(ctx);

        foreach (var eff in definition.Effects) eff.Apply(ctx, targets);

        _cooldown.Start(ctx.Time, definition.Cooldown);
        return true;
    }
}
