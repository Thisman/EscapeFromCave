using UnityEngine;

public sealed class BattleSquadAnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private BattleSquadController _battleSquadController;

    private void Start()
    {
        if (_spriteRenderer == null)
        {
            Debug.LogWarning("BattleSquadAnimationController: SpriteRenderer is not assigned.");
            return;
        }

        if (_battleSquadController == null)
        {
            Debug.LogWarning("BattleSquadAnimationController: BattleSquadController is not assigned.");
            return;
        }

        var model = _battleSquadController.GetBattleSquadModel();
        var definition = model?.Squad?.UnitDefinition;
        if (definition == null)
        {
            Debug.LogWarning("BattleSquadAnimationController: Squad definition is missing.");
            return;
        }

        _spriteRenderer.sprite = definition.Icon;
    }
}
