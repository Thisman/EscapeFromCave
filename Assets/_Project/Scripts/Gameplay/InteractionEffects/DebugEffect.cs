using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Effects/Debug")]
public sealed class DebugEffect : EffectSO
{
    public string Message;
    public override Task<EffectResult> Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        Debug.Log(Message);

        return Task.FromResult(EffectResult.Continue);
    }
}
