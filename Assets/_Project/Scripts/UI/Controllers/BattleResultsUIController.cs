using System;
using UnityEngine;
using UnityEngine.UI;

public class BattleResultsUIController : MonoBehaviour
{
    [SerializeField] private Button _exitBattleButton;

    public Action OnExitBattle;

    private void OnEnable()
    {
        _exitBattleButton.onClick.AddListener(() => OnExitBattle?.Invoke());
    }

    private void OnDisable()
    {
        _exitBattleButton.onClick.RemoveAllListeners();
    }
}
