using UnityEngine.LightTransport;

public interface IInteractable
{
    bool TryInteract(InteractionContext ctx);
    InteractableInfoDefinition GetInfo();
}
