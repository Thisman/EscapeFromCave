using UnityEngine;

public class BattleUnitAnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private BattleUnitController _unitController;

    private void Start()
    {
        var model = _unitController?.GetUnitModel();
        if (model != null && _spriteRenderer != null)
        {
            _spriteRenderer.sprite = model.Definition.Icon;
        }
    }
}
