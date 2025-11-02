using System.Collections.Generic;
using UnityEngine;

public abstract class InteractionTargetResolverDefinitionSO : ScriptableObject
{
    public abstract IReadOnlyList<GameObject> Resolve(InteractionContext ctx);
}
