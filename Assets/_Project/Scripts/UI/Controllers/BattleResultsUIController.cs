using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleResultsUIController : MonoBehaviour
{
    private const string VictoryStatusText = "Победа";
    private const string DefeatStatusText = "Поражение";
    private const string FleeStatusText = "Побег";

    [SerializeField] private Button _exitBattleButton;
    [SerializeField] private TextMeshProUGUI _statusText;

    public Action OnExitBattle;

    private void OnEnable()
    {
        _exitBattleButton.onClick.AddListener(() => OnExitBattle?.Invoke());
    }

    private void OnDisable()
    {
        _exitBattleButton.onClick.RemoveAllListeners();
    }

    public void Render(BattleResult result)
    {
        if (_statusText == null)
        {
            Debug.LogWarning("[BattleResultsUIController] Status text reference is missing.");
            return;
        }

        _statusText.text = result.Status switch
        {
            BattleResultStatus.Victory => VictoryStatusText,
            BattleResultStatus.Defeat => DefeatStatusText,
            BattleResultStatus.Flee => FleeStatusText,
            _ => string.Empty
        };
    }
}
