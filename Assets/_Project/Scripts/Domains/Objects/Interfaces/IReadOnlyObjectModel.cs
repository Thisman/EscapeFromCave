public interface IReadOnlyObjectModel
{
    InteractableDefinitionSO Definition { get; }
    InteractableInfo GetInfo();
}
