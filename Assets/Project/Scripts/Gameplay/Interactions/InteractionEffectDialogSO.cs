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
        ctx.InputService.EnterDialog();

        try
        {
            await ctx.DialogManager.ShowForDurationAsync(Message, _secondsPerCharacter);
        }
        finally
        {
            ctx.InputService.EnterGameplay();
        }

        return InteractionEffectResult.Continue;
    }
}
