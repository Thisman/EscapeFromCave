using System;
using UnityEngine;

public class ObjectModel : IReadOnlyObjectModel
{
    public InteractableDefinitionSO Definition { get; }

    public ObjectModel(InteractableDefinitionSO definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public InteractableInfoDefinition GetInfo()
    {
        return Definition.Info;
    }
}
