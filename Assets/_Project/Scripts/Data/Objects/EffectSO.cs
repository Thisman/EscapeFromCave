using System.Collections.Generic;
using UnityEngine;

public abstract class EffectSO : ScriptableObject
{
    public abstract void Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets);
}
