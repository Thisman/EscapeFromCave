using UnityEngine;

public class ResourceAnimationController : MonoBehaviour
{
    [SerializeField] private ResourceController _resourceController;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    public void Start()
    {
        _spriteRenderer.sprite = _resourceController.GetResourceModel().Icon;
    }
}
