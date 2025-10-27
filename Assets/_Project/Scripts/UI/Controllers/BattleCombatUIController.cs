using System;
using UnityEngine;
using UnityEngine.UI;

public class BattleCombatUIController : MonoBehaviour
{
    [SerializeField] private Button _leaveCombatButton;
    [SerializeField] private Button _defendButton;
    [SerializeField] private Button _skipTurnButton;

    public Action OnLeaveCombat;
    public Action OnDefend;
    public Action OnSkipTurn;

    public void SetDefendButtonInteractable(bool interactable)
    {
        _defendButton.interactable = interactable;
    }

    private void OnEnable()
    {
        _leaveCombatButton.onClick.AddListener(HandleLeaveCombatClicked);
        _defendButton.onClick.AddListener(HandleDefendClicked);
        _skipTurnButton.onClick.AddListener(HandleSkipTurnClicked);
    }

    private void OnDisable()
    {
        _leaveCombatButton.onClick.RemoveListener(HandleLeaveCombatClicked);
        _defendButton.onClick.RemoveListener(HandleDefendClicked);
        _skipTurnButton.onClick.RemoveListener(HandleSkipTurnClicked);
    }

    private void HandleLeaveCombatClicked() => OnLeaveCombat?.Invoke();

    private void HandleDefendClicked() => OnDefend?.Invoke();

    private void HandleSkipTurnClicked() => OnSkipTurn?.Invoke();
}
