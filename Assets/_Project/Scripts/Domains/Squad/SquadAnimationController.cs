using UnityEngine;

public class SquadAnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private SquadController _unitController;

    private void Start()
    {
        if (_spriteRenderer == null || _unitController == null)
            return;

        if (_unitController.GetSquadModel() is { Definition: { } definition })
            _spriteRenderer.sprite = definition.Icon;
}
}
