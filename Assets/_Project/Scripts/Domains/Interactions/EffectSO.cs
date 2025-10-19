using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public abstract class EffectSO : ScriptableObject
{
    public abstract Task Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets);
}
