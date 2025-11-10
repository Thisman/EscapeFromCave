using System;
using UnityEngine;

public class ObjectModel : IReadOnlyObjectModel
{
    public ObjectSO Definition { get; }

    public ObjectModel(ObjectSO definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }
}
