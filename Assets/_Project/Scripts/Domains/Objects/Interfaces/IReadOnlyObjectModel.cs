public interface IReadOnlyObjectModel
{
    InteractableDefinitionSO Definition { get; }
    InteractableInfoDefinition GetInfo();
}
