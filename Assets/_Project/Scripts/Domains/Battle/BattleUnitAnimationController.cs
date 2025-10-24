using UnityEngine;

public class BattleUnitAnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private BattleUnitController _unitController;

    private void Start()
    {
        if (_spriteRenderer == null || _unitController == null)
            return;

        var model = _unitController.GetSquadModel();
        if (model?.UnitDefinition != null)
            _spriteRenderer.sprite = model.UnitDefinition.Icon;
    }
}
