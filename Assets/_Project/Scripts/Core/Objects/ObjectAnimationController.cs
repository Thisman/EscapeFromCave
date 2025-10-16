using UnityEngine;

public class ObjectAnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private ObjectController _objectController;

    private void Start()
    {
        _spriteRenderer.sprite = _objectController.GetObjectModel().Definition.Icon;
    }
}
