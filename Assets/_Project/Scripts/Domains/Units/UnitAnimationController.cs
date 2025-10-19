using UnityEngine;

public class UnitAnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private UnitController _unitController;

    private void Start()
    {
        _spriteRenderer.sprite = _unitController.GetUnitModel().Definition.Icon;
    }
}
