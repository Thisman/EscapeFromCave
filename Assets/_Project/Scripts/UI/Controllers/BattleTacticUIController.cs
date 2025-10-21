using System;
using UnityEngine;
using UnityEngine.UI;

public class BattleTacticUIController : MonoBehaviour
{
    [SerializeField] private Button _startCombatButton;

    public Action OnStartCombat;

    private void OnEnable()
    {
        _startCombatButton.onClick.AddListener(() => OnStartCombat?.Invoke());
    }

    private void OnDisable()
    {
        _startCombatButton.onClick.RemoveAllListeners();
    }
}
