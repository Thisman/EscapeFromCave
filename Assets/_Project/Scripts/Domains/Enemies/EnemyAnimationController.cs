using UnityEngine;

public class EnemyAnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private EnemyController _enemyController;

    private void Start()
    {
        _spriteRenderer.sprite = _enemyController.GetEnemyModel().Definition.Icon;
    }
}
