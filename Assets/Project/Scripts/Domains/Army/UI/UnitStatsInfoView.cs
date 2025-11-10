using TMPro;
using UnityEngine;

public sealed class UnitStatsInfoView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _healthTextUI;
    [SerializeField] private TextMeshProUGUI _damageTextUI;
    [SerializeField] private TextMeshProUGUI _defenseTextUI;
    [SerializeField] private TextMeshProUGUI _initiativeTextUI;

    public void Render(UnitSO unitDefinition)
    {
        _healthTextUI.text = "Здоровье: " + unitDefinition.BaseHealth.ToString();
        _damageTextUI.text = "Урон: " + unitDefinition.MinDamage.ToString() + " - " + unitDefinition.MinDamage.ToString();
        _defenseTextUI.text = "Физ. защита: " + unitDefinition.BasePhysicalDefense.ToString();
        _initiativeTextUI.text = "Скорость: " + unitDefinition.Speed.ToString();
    }
}