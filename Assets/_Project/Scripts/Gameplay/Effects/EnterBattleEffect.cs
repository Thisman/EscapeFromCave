using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Effects/EnterBattle")]
public sealed class EnterBattleEffect : EffectSO
{
    public override async Task Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        Debug.Log("Enter battle effect applied.");
        await Task.CompletedTask;
    }
}
