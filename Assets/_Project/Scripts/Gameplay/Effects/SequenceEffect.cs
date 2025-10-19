using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Effects/Sequence")]
public sealed class SequenceEffect : EffectSO
{
    public EffectSO[] Children;

    public override async void Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        foreach (var effect in Children)
        {
            if (effect is IAsyncEffect asyncEffect)
            {
                await asyncEffect.ApplyAsync(ctx, targets);
            }
            else
            {
                effect.Apply(ctx, targets);
            }
        }
    }
}
