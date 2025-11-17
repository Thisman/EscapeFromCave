using UnityEngine;

[RequireComponent(typeof(ResourceController))]

public class ResourceAnimationController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private ResourceController _resourceController;

    public void Start()
    {
        _spriteRenderer.sprite = _resourceController.GetResourceModel().Icon;
    }
}
