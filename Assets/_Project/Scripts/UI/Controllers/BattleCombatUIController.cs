using System;
using UnityEngine;
using UnityEngine.UI;

public class BattleCombatUIController : MonoBehaviour
{
    [SerializeField] private Button _leaveCombatButton;

    public Action OnLeaveCombat;

    private void OnEnable()
    {
        _leaveCombatButton.onClick.AddListener(() => OnLeaveCombat?.Invoke());
    }

    private void OnDisable()
    {
        _leaveCombatButton.onClick.RemoveAllListeners();
    }
}
