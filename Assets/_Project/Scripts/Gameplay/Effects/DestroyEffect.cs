using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DestroyEffect", menuName = "Gameplay/Effects/Destroy", order = 0)]
public class DestroyEffect : EffectSO
{
    [Header("Destroy Settings")]
    [Tooltip("Задержка перед удалением, в секундах.")]
    [Min(0f)]
    public float delay = 0f;

    [Tooltip("Если true — уничтожать сразу, даже если объект неактивен.")]
    public bool includeInactive = false;

    public override void Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        if (targets == null || targets.Count == 0)
            return;

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

#if UNITY_EDITOR
            Debug.Log($"[DestroyEffectSO] Уничтожен объект '{target.name}' (delay = {delay:0.00}s)");
#endif
        }
    }
}
