using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public enum EffectResult
{
    Continue,
    Restart,
}

public abstract class EffectSO : ScriptableObject
{
    public abstract Task<EffectResult> Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets);
}
