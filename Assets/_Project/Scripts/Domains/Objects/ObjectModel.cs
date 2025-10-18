using System;
using UnityEngine;

public class ObjectModel : IReadOnlyObjectModel
{
    public InteractableDefinitionSO Definition { get; }

    public ObjectModel(InteractableDefinitionSO definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public InteractableInfo GetInfo()
    {
        return Definition.Info;
    }
}
