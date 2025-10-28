using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleAbilityListUIController : MonoBehaviour
{
    [SerializeField] private BattleAbilityItemView abilityItemViewPrefab;
    [SerializeField] private Transform contentRoot;

    public event Action<BattleAbilityDefinitionSO> OnSelectAbility;

    private readonly List<BattleAbilityItemView> abilityItemViews = new();

    public void Render(BattleAbilityDefinitionSO[] abilities)
    {
        ClearItems();

        if (abilities == null || abilities.Length == 0)
        {
            SetActive(false);
            return;
        }

        if (abilityItemViewPrefab == null)
        {
            Debug.LogWarning("BattleAbilityListUIController: ability item prefab is not assigned.");
            SetActive(false);
            return;
        }

        Transform parent = contentRoot != null ? contentRoot : transform;

        for (int i = 0; i < abilities.Length; i++)
        {
            BattleAbilityDefinitionSO ability = abilities[i];
            if (ability == null)
            {
                continue;
            }

            BattleAbilityItemView itemView = Instantiate(abilityItemViewPrefab, parent);
            itemView.Render(ability);
            itemView.OnClick += HandleAbilitySelected;
            abilityItemViews.Add(itemView);
        }

        SetActive(abilityItemViews.Count > 0);
    }

    public BattleAbilityItemView FindItem(BattleAbilityDefinitionSO ability)
    {
        if (ability == null)
        {
            return null;
        }

        for (int i = 0; i < abilityItemViews.Count; i++)
        {
            BattleAbilityItemView itemView = abilityItemViews[i];
            if (itemView == null)
            {
                continue;
            }

            if (itemView.Definition == ability)
            {
                return itemView;
            }
        }

        return null;
    }

    public void ResetHighlights()
    {
        for (int i = 0; i < abilityItemViews.Count; i++)
        {
            BattleAbilityItemView itemView = abilityItemViews[i];
            if (itemView == null)
            {
                continue;
            }

            itemView.ResetHighlight();
        }
    }

    private void HandleAbilitySelected(BattleAbilityDefinitionSO ability)
    {
        OnSelectAbility?.Invoke(ability);
    }

    private void ClearItems()
    {
        for (int i = 0; i < abilityItemViews.Count; i++)
        {
            BattleAbilityItemView itemView = abilityItemViews[i];
            if (itemView == null)
            {
                continue;
            }

            itemView.OnClick -= HandleAbilitySelected;
            Destroy(itemView.gameObject);
        }

        abilityItemViews.Clear();
    }

    private void SetActive(bool isActive)
    {
        if (gameObject.activeSelf != isActive)
        {
            gameObject.SetActive(isActive);
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < abilityItemViews.Count; i++)
        {
            BattleAbilityItemView itemView = abilityItemViews[i];
            if (itemView != null)
            {
                itemView.OnClick -= HandleAbilitySelected;
            }
        }
    }
}
