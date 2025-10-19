using UnityEngine;

public class BattleUnitAnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private BattleUnitController _battleUnitController;

    private void Start()
    {
        _spriteRenderer.sprite = _battleUnitController.GetEnemyModel().Definition.Icon;
    }
}
