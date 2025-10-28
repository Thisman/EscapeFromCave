using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleQueueItemView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _countText;

    public void Render(IReadOnlySquadModel unit)
    {
        _icon.sprite = unit.Icon;
        _countText.text = unit.Count.ToString();
    }
}
