using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Effects/Dialog")]
public sealed class ShowDialogEffect : EffectSO
{
    [TextArea] public string Message;

    [SerializeField, Min(0f)] private float _secondsPerCharacter = 0.05f;

    public override async Task<EffectResult> Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        if (ctx == null)
        {
            Debug.LogWarning("[ShowDialogEffect] Interaction context is null. Skipping dialog display.");
            return EffectResult.Continue;
        }

        if (ctx.DialogManager == null)
        {
            Debug.LogWarning("[ShowDialogEffect] DialogController is missing in the context. Assign it in the scene lifetime scope.");
            return EffectResult.Continue;
        }

        var message = Message ?? string.Empty;
        var inputRouter = ctx.InputRouter;

        inputRouter?.EnterDialog();

        try
        {
            await ctx.DialogManager.ShowForDurationAsync(message, _secondsPerCharacter);
        }
        finally
        {
            inputRouter?.EnterGameplay();
        }

        return EffectResult.Continue;
    }
}
