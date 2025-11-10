using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "DestroyEffect", menuName = "Gameplay/Effects/Destroy")]
public class InteractionEffectDestroySO : InteractionEffectSO
{
    [Min(0f)] public float delay = 0f;

    public bool includeInactive = false;

    public override Task<InteractionEffectResult> Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        foreach (var target in targets)
        {
            if (target == null)
                continue;

            // Можно добавить проверку на includeInactive
            if (!includeInactive && !target.activeInHierarchy)
                continue;

            if (delay <= 0f)
            {
                Destroy(target);
            }
            else
            {
                // уничтожаем с задержкой
                Destroy(target, delay);
            }
        }

        return Task.FromResult(InteractionEffectResult.Continue);
    }
}
