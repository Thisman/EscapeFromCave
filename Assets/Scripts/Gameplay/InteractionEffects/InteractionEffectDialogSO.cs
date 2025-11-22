using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogEffect", menuName = "Gameplay/Effects/Dialog")]
public sealed class InteractionEffectDialogSO : InteractionEffectSO
{
    [TextArea] public string Message;

    public override async Task<InteractionEffectResult> Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        ctx.InputService.EnterDialog();

        // TODO: реализовать показ сообщений через событие RequestDialogShow
        try
        {
            await ctx.DialogManager.ShowForDurationAsync(Message);
        }
        finally
        {
            ctx.InputService.EnterGameplay();
        }

        return InteractionEffectResult.Continue;
    }
}
