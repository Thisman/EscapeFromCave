using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Effects/Teleport Player Random Point")]
public sealed class TeleportEffect : EffectSO
{
    [SerializeField]
    private Vector3[] _points = Array.Empty<Vector3>();

    [SerializeField]
    private bool _preserveOriginalZ = true;

    public override Task<EffectResult> Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        if (ctx == null)
        {
            Debug.LogWarning("[TeleportEffect] Interaction context is null. Unable to teleport player.");
            return Task.FromResult(EffectResult.Continue);
        }

        if (ctx.Actor == null)
        {
            Debug.LogWarning("[TeleportEffect] Actor is not specified in the interaction context. Unable to teleport player.");
            return Task.FromResult(EffectResult.Continue);
        }

        if (_points == null || _points.Length == 0)
        {
            Debug.LogWarning("[TeleportEffect] No teleport points configured. Unable to teleport player.");
            return Task.FromResult(EffectResult.Continue);
        }

        var actorTransform = ctx.Actor.transform;
        var selectedIndex = UnityEngine.Random.Range(0, _points.Length);
        var destination = _points[selectedIndex];

        if (_preserveOriginalZ)
        {
            destination.z = actorTransform.position.z;
        }

        actorTransform.position = destination;
        return Task.FromResult(EffectResult.Continue);
    }
}
