using Game.Data;
using System;
using UnityEngine;

public class ObjectModel
{
    public InteractableDefinitionSO Definition { get; }

    public ObjectModel(InteractableDefinitionSO definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }
}
