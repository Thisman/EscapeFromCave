using System.Collections.Generic;
using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Gameplay/Effects/ChangeSprite")]
public class ChangeSpriteEffect : EffectSO
{
    [Header("Sprite Change Settings")]
    [Tooltip("Какой спрайт будет установлен после задержки.")]
    public Sprite newSprite;

    [Tooltip("Задержка перед изменением спрайта, в секундах.")]
    [Min(0f)]
    public float delay = 0f;

    [Tooltip("Если true — попытаться изменить спрайт даже у неактивных объектов.")]
    public bool includeInactive = false;

    public override void Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        if (newSprite == null)
        {
            return;
        }

        if (targets == null || targets.Count == 0)
            return;

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