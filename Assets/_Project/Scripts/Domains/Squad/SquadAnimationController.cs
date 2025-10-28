using UnityEngine;

public class SquadAnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private SquadController _unitController;

    private void Start()
    {
        _spriteRenderer.sprite = _unitController.GetSquadModel().Icon;
    }
}
