using System;
using UnityEngine;
using UnityEngine.UI;

public class BattleCombatUIController : MonoBehaviour
{
    [SerializeField] private Button _leaveCombatButton;
    [SerializeField] private Button _defendButton;
    [SerializeField] private Button _skipTurnButton;
    [SerializeField] private BattleAbilityListUIController _abilityListController;

    public Action OnLeaveCombat;
    public Action OnDefend;
    public Action OnSkipTurn;
    public Action<BattleAbilityDefinitionSO> OnSelectAbility;

    public void SetDefendButtonInteractable(bool interactable)
    {
        _defendButton.interactable = interactable;
    }

    public void RenderAbilityList(BattleAbilityDefinitionSO[] abilities)
    {
        _abilityListController.Render(abilities);
    }

    public void HighlightAbility(BattleAbilityDefinitionSO ability)
    {
        if (ability == null)
        {
            _abilityListController.ResetHighlights();
            return;
        }

        _abilityListController.ResetHighlights();

        BattleAbilityItemView itemView = _abilityListController.FindItem(ability);
        itemView?.Highlight();
    }

    public void ResetAbilityHighlight()
    {
        _abilityListController?.ResetHighlights();
    }

    private void OnEnable()
    {
        _leaveCombatButton.onClick.AddListener(HandleLeaveCombatClicked);
        _defendButton.onClick.AddListener(HandleDefendClicked);
        _skipTurnButton.onClick.AddListener(HandleSkipTurnClicked);
        _abilityListController.OnSelectAbility += HandleAbilitySelected;
    }

    private void OnDisable()
    {
        _leaveCombatButton.onClick.RemoveListener(HandleLeaveCombatClicked);
        _defendButton.onClick.RemoveListener(HandleDefendClicked);
        _skipTurnButton.onClick.RemoveListener(HandleSkipTurnClicked);
        _abilityListController.OnSelectAbility -= HandleAbilitySelected;
    }

    private void HandleLeaveCombatClicked() => OnLeaveCombat?.Invoke();

    private void HandleDefendClicked() => OnDefend?.Invoke();

    private void HandleSkipTurnClicked() => OnSkipTurn?.Invoke();

    private void HandleAbilitySelected(BattleAbilityDefinitionSO ability)
    {
        OnSelectAbility?.Invoke(ability);
    }
}
