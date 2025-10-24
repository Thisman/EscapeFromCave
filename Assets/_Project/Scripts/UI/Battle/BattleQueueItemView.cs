using UnityEngine;
using UnityEngine.UI;

public sealed class BattleQueueItemView : MonoBehaviour
{
    [SerializeField] private Image _icon;

    public void Bind(IReadOnlyUnitModel unit)
    {
        if (_icon == null)
            return;

        if (unit?.Definition != null)
        {
            _icon.sprite = unit.Definition.Icon;
            _icon.enabled = unit.Definition.Icon != null;
        }
        else
        {
            _icon.sprite = null;
            _icon.enabled = false;
        }
    }
}
