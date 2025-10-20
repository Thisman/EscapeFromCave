using System.Collections;
using UnityEngine;

public class ResourceController : MonoBehaviour
{
    [SerializeField] private int _count;
    [SerializeField] private Sprite _icon;

    private ResourceModel _resourceModel;

    private void Awake()
    {
        _resourceModel = new ResourceModel(_icon, _count);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.GetComponent<PlayerResourcesController>().AddResources(_resourceModel);
            Destroy(gameObject);
        }
    }

    public IReadonlyResourceModel GetResourceModel()
    {
        return _resourceModel;
    }
}
