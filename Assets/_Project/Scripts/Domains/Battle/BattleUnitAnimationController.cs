using UnityEngine;

public class BattleUnitAnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private BattleUnitController _battleUnitController;

    private void Start()
    {
        _spriteRenderer.sprite = _battleUnitController.GetEnemyModel().Definition.Icon;
    }

    public void SetFriendlyOrientation(bool isFriendlySlot)
    {
        if (_spriteRenderer == null)
        {
            Debug.LogWarning("[BattleUnitAnimationController] Missing SpriteRenderer reference for orientation change.");
            return;
        }

        _spriteRenderer.flipX = isFriendlySlot;
    }
}
