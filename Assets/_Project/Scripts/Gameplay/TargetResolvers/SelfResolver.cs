using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Target Resolver/Self")]
public sealed class SelfResolver : TargetResolverSO
{
    public override IReadOnlyList<GameObject> Resolve(InteractionContext ctx)
        => new[] { ctx.Target };
}
