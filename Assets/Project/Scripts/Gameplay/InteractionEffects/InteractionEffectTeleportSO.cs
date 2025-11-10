using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "TeleportEffect", menuName = "Gameplay/Effects/Teleport")]
public sealed class InteractionEffectTeleportSO : InteractionEffectSO
{
    [SerializeField]
    private Vector3[] _points = Array.Empty<Vector3>();

    [SerializeField]
    private bool _preserveOriginalZ = true;

    public override Task<InteractionEffectResult> Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        if (_points == null || _points.Length == 0)
        {
            GameLogger.Warn("[TeleportEffect] No teleport points configured. Unable to teleport player.");
            return Task.FromResult(InteractionEffectResult.Continue);
        }

        Transform actorTransform = ctx.Actor.transform;
        int selectedIndex = UnityEngine.Random.Range(0, _points.Length);
        Vector3 destination = _points[selectedIndex];

        if (_preserveOriginalZ)
        {
            destination.z = actorTransform.position.z;
        }

        actorTransform.position = destination;
        return Task.FromResult(InteractionEffectResult.Continue);
    }
}
