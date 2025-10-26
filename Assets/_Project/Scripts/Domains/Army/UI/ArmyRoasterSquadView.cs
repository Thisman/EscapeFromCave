using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public sealed class ArmyRoasterSquadView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _countText;

    public void Render(IReadOnlySquadModel squad)
    {
        if (squad == null)
        {
            if (_icon) _icon.sprite = null;
            if (_countText) _countText.text = string.Empty;
            return;
        }

        if (_icon)
            _icon.sprite = squad.UnitDefinition != null ? squad.UnitDefinition.Icon : null;

        if (_countText)
            _countText.text = squad.Count.ToString();
    }
}
