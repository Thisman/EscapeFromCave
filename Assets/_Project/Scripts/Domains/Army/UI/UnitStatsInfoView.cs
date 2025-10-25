using TMPro;
using UnityEngine;

public sealed class UnitStatsInfoView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _healthTextUI;
    [SerializeField] private TextMeshProUGUI _damageTextUI;
    [SerializeField] private TextMeshProUGUI _defenseTextUI;
    [SerializeField] private TextMeshProUGUI _initiativeTextUI;

    public void Render(UnitDefinitionSO unitDefinition)
    {
        UnitLevelDefintion stats = unitDefinition.GetStatsForLevel(0);

        _healthTextUI.text = "Здоровье: " + stats.Health.ToString();
        _damageTextUI.text = "Урон: " + stats.Damage.ToString();
        _defenseTextUI.text = "Защита: " + stats.Defense.ToString();
        _initiativeTextUI.text = "Инициатива: " + stats.Initiative.ToString();
    }
}