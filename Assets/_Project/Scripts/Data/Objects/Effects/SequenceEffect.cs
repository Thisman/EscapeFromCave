using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Effects/Sequence")]
public sealed class SequenceEffect : EffectSO
{
    public EffectSO[] Children;

    public override void Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        foreach (var e in Children)
            e.Apply(ctx, targets);
    }
}
