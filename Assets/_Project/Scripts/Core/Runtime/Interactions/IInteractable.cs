using UnityEngine.LightTransport;

public interface IInteractable
{
    bool TryInteract(InteractionContext ctx);
    InteractableInfo GetInfo();
}
