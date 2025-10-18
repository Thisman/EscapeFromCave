using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Effects/EnterBattle")]
public sealed class EnterBattleEffect : EffectSO
{
    public override async void Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        SceneLoader _sceneLoader = ctx.SceneLoader;
        await _sceneLoader.LoadAdditiveAsync("Battle", true);
    }
}
