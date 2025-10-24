using UnityEngine;

public class BattleSquadAnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private BattleSquadController _squadController;

    private void Start()
    {
        var battleModel = _squadController?.GetSquadModel();
        if (battleModel != null && _spriteRenderer != null)
        {
            _spriteRenderer.sprite = battleModel.UnitDefinition.Icon;
        }
    }
}
