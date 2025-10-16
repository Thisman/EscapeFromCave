using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DestroyEffect", menuName = "Gameplay/Effects/Destroy", order = 0)]
public class DestroyEffect : EffectSO
{
    [Header("Destroy Settings")]
    [Tooltip("�������� ����� ���������, � ��������.")]
    [Min(0f)]
    public float delay = 0f;

    [Tooltip("���� true � ���������� �����, ���� ���� ������ ���������.")]
    public bool includeInactive = false;

    public override void Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        if (targets == null || targets.Count == 0)
            return;

        foreach (var target in targets)
        {
            if (target == null)
                continue;

            // ����� �������� �������� �� includeInactive
            if (!includeInactive && !target.activeInHierarchy)
                continue;

            if (delay <= 0f)
            {
                Destroy(target);
            }
            else
            {
                // ���������� � ���������
                Destroy(target, delay);
            }

#if UNITY_EDITOR
            Debug.Log($"[DestroyEffectSO] ��������� ������ '{target.name}' (delay = {delay:0.00}s)");
#endif
        }
    }
}
