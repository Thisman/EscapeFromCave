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

    private void OnEnable()
    {
        if (_leaveCombatButton != null)
            _leaveCombatButton.onClick.AddListener(HandleLeaveCombatClicked);

        if (_defendButton != null)
            _defendButton.onClick.AddListener(HandleDefendClicked);

        if (_skipTurnButton != null)
            _skipTurnButton.onClick.AddListener(HandleSkipTurnClicked);
    }

    private void OnDisable()
    {
        if (_leaveCombatButton != null)
            _leaveCombatButton.onClick.RemoveListener(HandleLeaveCombatClicked);

        if (_defendButton != null)
            _defendButton.onClick.RemoveListener(HandleDefendClicked);

        if (_skipTurnButton != null)
            _skipTurnButton.onClick.RemoveListener(HandleSkipTurnClicked);
    }

    private void HandleLeaveCombatClicked() => OnLeaveCombat?.Invoke();

    private void HandleDefendClicked() => OnDefend?.Invoke();

    private void HandleSkipTurnClicked() => OnSkipTurn?.Invoke();
}
