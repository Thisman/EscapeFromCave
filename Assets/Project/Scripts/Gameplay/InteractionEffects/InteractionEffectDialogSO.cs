using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogEffect", menuName = "Gameplay/Effects/Dialog")]
public sealed class InteractionEffectDialogSO : InteractionEffectSO
{
    [TextArea] public string Message;

    [SerializeField, Min(0f)] private float _secondsPerCharacter = 0.05f;

    public override async Task<InteractionEffectResult> Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        if (ctx == null)
        {
            GameLogger.Warn("[ShowDialogEffect] Interaction context is null. Skipping dialog display.");
            return InteractionEffectResult.Continue;
        }

        if (ctx.DialogManager == null)
        {
            GameLogger.Warn("[ShowDialogEffect] DialogController is missing in the context. Assign it in the scene lifetime scope.");
            return InteractionEffectResult.Continue;
        }

        var message = Message ?? string.Empty;
        var inputRouter = ctx.InputService;

        inputRouter?.EnterDialog();

        try
        {
            await ctx.DialogManager.ShowForDurationAsync(message, _secondsPerCharacter);
        }
        finally
        {
            inputRouter?.EnterGameplay();
        }

        return InteractionEffectResult.Continue;
    }
}
