using System.Collections.Generic;
using UnityEngine;

public abstract class TargetResolverSO : ScriptableObject
{
    public abstract IReadOnlyList<GameObject> Resolve(InteractionContext ctx);
}
