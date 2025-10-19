using UnityEngine;

public class ObjectController : MonoBehaviour
{
    [SerializeField] private ObjectDefinitionSO _interactableDefinition;

    private ObjectModel _objectModel;

    public void Awake()
    {
        _objectModel = new ObjectModel(_interactableDefinition);
    }

    public IReadOnlyObjectModel GetObjectModel()
    {
        return _objectModel;
    }
}
