using System;
using UnityEngine;
using UnityEngine.UI;

public class BattleTacticUIController : MonoBehaviour
{
    [SerializeField] private Button _startCombatButton;

    public Action OnBattleRoundsStart;

    private void OnEnable()
    {
        _startCombatButton.onClick.AddListener(() => OnBattleRoundsStart?.Invoke());
    }

    private void OnDisable()
    {
        _startCombatButton.onClick.RemoveAllListeners();
    }
}
