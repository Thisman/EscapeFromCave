using System.Collections.Generic;
using UnityEngine;

public abstract class InteractionTargetResolverSO : ScriptableObject
{
    public abstract IReadOnlyList<GameObject> Resolve(InteractionContext ctx);
}
