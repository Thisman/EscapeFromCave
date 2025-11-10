using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Threading.Tasks;

[CreateAssetMenu(fileName = "ChangeSpriteEffect", menuName = "Gameplay/Effects/Change Sprite")]
public class InteractionEffectChangeSpriteSO : InteractionEffectSO
{
    public Sprite newSprite;

    [Min(0f)] public float delay = 0f;

    public bool includeInactive = false;

    public override Task<InteractionEffectResult> Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        if (newSprite == null)
        {
            return Task.FromResult(InteractionEffectResult.Continue);
        }

        foreach (var target in targets)
        {
            if (!includeInactive && !target.activeInHierarchy)
                continue;

            SpriteRenderer renderer = target.GetComponentInChildren<SpriteRenderer>();
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
                if (ctx.Actor.TryGetComponent<MonoBehaviour>(out var runner))
                    runner.StartCoroutine(DelayedChange(renderer, delay));
                else
                    ApplySprite(renderer); // fallback
            }
        }

        return Task.FromResult(InteractionEffectResult.Continue);
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