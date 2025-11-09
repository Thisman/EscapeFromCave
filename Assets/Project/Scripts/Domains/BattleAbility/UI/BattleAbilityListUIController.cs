using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleAbilityListUIController : MonoBehaviour
{
    [SerializeField] private BattleAbilityItemView abilityItemViewPrefab;
    [SerializeField] private Transform contentRoot;

    public event Action<BattleAbilityDefinitionSO> OnSelectAbility;

    private readonly List<BattleAbilityItemView> abilityItemViews = new();

    private BattleAbilityManager _abilityManager;
    private IReadOnlySquadModel _owner;
    private BattleAbilityItemView _selectedItemView;

    public void Render(BattleAbilityDefinitionSO[] abilities, BattleAbilityManager abilityManager, IReadOnlySquadModel owner)
    {
        ClearItems();

        _abilityManager = abilityManager;
        _owner = owner;

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
            UpdateItemAvailability(itemView);
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
            itemView.SetSelected(false);
        }

        _selectedItemView = null;
    }

    public void RefreshAvailability()
    {
        for (int i = 0; i < abilityItemViews.Count; i++)
        {
            BattleAbilityItemView itemView = abilityItemViews[i];
            if (itemView == null)
                continue;

            UpdateItemAvailability(itemView);
        }
    }

    private void HandleAbilitySelected(BattleAbilityItemView itemView, BattleAbilityDefinitionSO ability)
    {
        if (itemView == null)
            return;

        if (_selectedItemView != null && _selectedItemView != itemView)
        {
            _selectedItemView.SetSelected(false);
        }

        _selectedItemView = itemView;
        _selectedItemView.SetSelected(true);

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
        _abilityManager = null;
        _owner = null;
        _selectedItemView = null;
    }

    private void SetActive(bool isActive)
    {
        if (gameObject.activeSelf != isActive)
        {
            gameObject.SetActive(isActive);
        }
    }

    private void UpdateItemAvailability(BattleAbilityItemView itemView)
    {
        if (itemView == null)
            return;

        if (_abilityManager == null || _owner == null)
        {
            itemView.SetInteractable(true);
            return;
        }

        BattleAbilityDefinitionSO ability = itemView.Definition;
        bool isReady = ability != null && _abilityManager.IsAbilityReady(_owner, ability);
        itemView.SetInteractable(isReady);
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
