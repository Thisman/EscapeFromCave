using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Effects/Debug")]
public sealed class DebugEffect : EffectSO
{
    public string Message;
    public override void Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        Debug.Log(Message);
    }
}
