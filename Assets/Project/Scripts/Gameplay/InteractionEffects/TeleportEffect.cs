using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "TeleportEffect", menuName = "Gameplay/Effects/Teleport")]
public sealed class TeleportEffect : EffectDefinitionSO
{
    [SerializeField]
    private Vector3[] _points = Array.Empty<Vector3>();

    [SerializeField]
    private bool _preserveOriginalZ = true;

    public override Task<EffectResult> Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        if (ctx == null)
        {
            GameLogger.Warn("[TeleportEffect] Interaction context is null. Unable to teleport player.");
            return Task.FromResult(EffectResult.Continue);
        }

        if (ctx.Actor == null)
        {
            GameLogger.Warn("[TeleportEffect] Actor is not specified in the interaction context. Unable to teleport player.");
            return Task.FromResult(EffectResult.Continue);
        }

        if (_points == null || _points.Length == 0)
        {
            GameLogger.Warn("[TeleportEffect] No teleport points configured. Unable to teleport player.");
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
