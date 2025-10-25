using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "DestroyEffect", menuName = "Gameplay/Effects/Destroy", order = 0)]
public class DestroyEffect : EffectSO
{
    [Min(0f)] public float delay = 0f;

    public bool includeInactive = false;

    public override Task Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        if (targets == null || targets.Count == 0)
            return Task.CompletedTask;

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

        return Task.CompletedTask;
    }
}
