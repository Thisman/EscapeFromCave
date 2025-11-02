using System;
using UnityEngine;

public class ObjectModel : IReadOnlyObjectModel
{
    public ObjectDefinitionSO Definition { get; }

    public ObjectModel(ObjectDefinitionSO definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }
}
