using UnityEngine;

public class BattleSquadAnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private BattleSquadController _unitController;

    private void Start()
    {
        if (_spriteRenderer == null || _unitController == null)
            return;

        var model = _unitController.Model;
        if (model?.UnitDefinition != null)
            _spriteRenderer.sprite = model.UnitDefinition.Icon;
    }
}
