using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class ObjectInteraction : MonoBehaviour, IInteractable
{
    [SerializeField] private ObjectController _objectController;
    private CooldownState _cooldown;

    public InteractableInfo GetInfo() => _objectController.GetObjectModel().Definition.Info;

    public bool TryInteract(InteractionContext ctx)
    {
        InteractableDefinitionSO definition = _objectController.GetObjectModel().Definition;

        if (definition == null) return false;
        if (!_cooldown.Ready(ctx.Time)) return false;
        if (!definition.Conditions.All(c => c.IsMet(ctx))) return false;

        var targets = definition.TargetResolver.Resolve(ctx);

        foreach (var eff in definition.Effects) eff.Apply(ctx, targets);

        _cooldown.Start(ctx.Time, definition.Cooldown);
        return true;
    }
}
