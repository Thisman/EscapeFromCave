using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Threading.Tasks;

[CreateAssetMenu(fileName = "ChangeSpriteEffect", menuName = "Gameplay/Effects/Change Sprite")]
public class ChangeSpriteEffect : EffectDefinitionSO
{
    public Sprite newSprite;

    [Min(0f)] public float delay = 0f;

    public bool includeInactive = false;

    public override Task<EffectResult> Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        if (newSprite == null)
        {
            return Task.FromResult(EffectResult.Continue);
        }

        if (targets == null || targets.Count == 0)
            return Task.FromResult(EffectResult.Continue);

        foreach (var target in targets)
        {
            if (target == null)
                continue;

            if (!includeInactive && !target.activeInHierarchy)
                continue;

            var renderer = target.GetComponentInChildren<SpriteRenderer>();
            if (renderer == null)
            {
                continue;
            }

            if (delay <= 0f)
            {
                ApplySprite(renderer);
            }
            else
            {
                var runner = ctx.Actor.GetComponent<MonoBehaviour>();
                if (runner != null)
                    runner.StartCoroutine(DelayedChange(renderer, delay));
                else
                    ApplySprite(renderer); // fallback
            }
        }

        return Task.FromResult(EffectResult.Continue);
    }

    private void ApplySprite(SpriteRenderer renderer)
    {
        renderer.sprite = newSprite;
    }

    private IEnumerator DelayedChange(SpriteRenderer renderer, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (renderer != null)
            ApplySprite(renderer);
    }
}