using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Effects/Dialog")]
public sealed class ShowDialogEffect : EffectSO
{
    [TextArea]
    public string Message;

    [SerializeField, Min(0f)] private float _displayDuration = 2f;

    public override async Task Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
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

        var message = Message ?? string.Empty;
        var inputRouter = ctx.InputRouter;

        inputRouter?.EnterDialog();

        try
        {
            await ctx.DialogController.ShowForDurationAsync(message, _displayDuration);
        }
        finally
        {
            inputRouter?.EnterGameplay();
        }
    }
}
