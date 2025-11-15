using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public enum InteractionEffectResult
{
    Continue,
    Break,
}

public abstract class InteractionEffectSO : ScriptableObject
{
    public abstract Task<InteractionEffectResult> Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets);
}
