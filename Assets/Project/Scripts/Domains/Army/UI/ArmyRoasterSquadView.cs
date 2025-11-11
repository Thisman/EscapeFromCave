using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public sealed class ArmyRoasterSquadView : MonoBehaviour, ISquadModelProvider
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _countText;

    private IReadOnlySquadModel _model;

    public void Render(IReadOnlySquadModel squad)
    {
        _model = squad;

        if (squad == null)
        {
            if (_icon) _icon.sprite = null;
            if (_countText) _countText.text = string.Empty;
            return;
        }

        if (_icon)
            _icon.sprite = squad.Icon;

        if (_countText)
            _countText.text = squad.Count.ToString();
    }

    public IReadOnlySquadModel GetSquadModel()
    {
        return _model;
    }
}
