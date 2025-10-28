using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public enum EffectResult
{
    Continue,
    Break,
}

public abstract class EffectDefinitionSO : ScriptableObject
{
    public abstract Task<EffectResult> Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets);
}
