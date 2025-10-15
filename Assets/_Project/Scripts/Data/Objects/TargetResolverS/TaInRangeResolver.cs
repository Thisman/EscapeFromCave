using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Target Resolver/Tag In Range")]
public sealed class TagInRangeResolver : TargetResolverSO
{
    public string Tag;
    public float Radius = 5f;

    public override IReadOnlyList<GameObject> Resolve(InteractionContext ctx)
    {
        var results = new List<GameObject>();
        foreach (var col in Physics.OverlapSphere(ctx.Target.transform.position, Radius))
        {
            if (col.CompareTag(Tag))
                results.Add(col.gameObject);
        }
        return results;
    }
}
