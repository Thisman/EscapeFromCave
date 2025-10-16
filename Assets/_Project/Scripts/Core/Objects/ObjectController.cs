using UnityEngine;

public class ObjectController : MonoBehaviour
{
    [SerializeField] private InteractableDefinitionSO _interactableDefinition;

    private ObjectModel _objectModel;

    public void Awake()
    {
        _objectModel = new ObjectModel(_interactableDefinition);
    }

    public ObjectModel GetObjectModel()
    {
        Debug.Log(_objectModel);
        return _objectModel;
    }
}
