using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public sealed class SquadItemView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _countText;

    public void Bind(SquadViewModel squad)
    {
        if (squad == null)
        {
            if (_icon) _icon.sprite = null;
            if (_countText) _countText.text = string.Empty;
            return;
        }

        if (_icon)
            _icon.sprite = squad.Icon;

        if (_countText)
            _countText.text = squad.Count > 0 ? squad.Count.ToString() : string.Empty;
    }
}
