using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Effects/Dialog")]
public sealed class ShowDialogEffect : EffectSO, IAsyncEffect
{
    [TextArea]
    public string Message;

    [SerializeField, Min(0f)] private float _displayDuration = 2f;

    public override void Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        _ = ApplyAsync(ctx, targets);
    }

    public async Task ApplyAsync(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        if (ctx == null)
        {
            Debug.LogWarning("[ShowDialogEffect] Interaction context is null. Skipping dialog display.");
            return;
        }

        if (ctx.DialogController == null)
        {
            Debug.LogWarning("[ShowDialogEffect] DialogController is missing in the context. Assign it in the scene lifetime scope.");
            return;
        }

        await ctx.DialogController.ShowForDurationAsync(Message ?? string.Empty, _displayDuration);
    }
}
