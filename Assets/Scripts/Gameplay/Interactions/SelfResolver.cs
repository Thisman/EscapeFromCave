using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SelfTargetResolver", menuName = "Gameplay/Interactions/Self Target Resolver")]
public sealed class SelfResolver : InteractionTargetResolverSO
{
    public override IReadOnlyList<GameObject> Resolve(InteractionContext ctx)
        => new[] { ctx.Target };
}
