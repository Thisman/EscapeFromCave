using UnityEngine;

public class BattleUnitAnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private BattleUnitController _unitController;

    private void Start()
    {
        var battleModel = _unitController?.GetUnitModel();
        if (battleModel != null && _spriteRenderer != null)
        {
            _spriteRenderer.sprite = battleModel.Definition?.Icon;
            _spriteRenderer.enabled = _spriteRenderer.sprite != null;
        }
    }
}
