using UnityEngine;
using VContainer;

public class ObjectInteraction : MonoBehaviour, IInteractable
{
    [SerializeField] private ObjectController _objectController;
    [Inject] private InteractionProcessor _processor;
    private CooldownState _cooldown;

    public InteractableInfo GetInfo()
    {
        var model = _objectController != null ? _objectController.GetObjectModel() : null;
        return model != null ? model.Definition.Info : default;
    }

    public bool TryInteract(InteractionContext ctx)
    {
        if (_objectController == null || ctx == null)
            return false;

        var model = _objectController.GetObjectModel();
        var definition = model?.Definition;

        if (definition == null) return false;
        if (!_cooldown.Ready(ctx.Time)) return false;
        if (_processor == null) return false;

        if (!_processor.TryProcess(definition, ctx))
            return false;

        _cooldown.Start(ctx.Time, definition.Cooldown);
        return true;
    }
}
