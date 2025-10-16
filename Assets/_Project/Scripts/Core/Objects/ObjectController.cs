using Game.Data;
using UnityEngine;

public class ObjectController : MonoBehaviour
{
    [SerializeField] private InteractableDefinitionSO _interactableDefinition;

    private ObjectModel _objectModel;

    private void Start()
    {
        _objectModel = new ObjectModel(_interactableDefinition);
    }

    public ObjectModel GetObjectModel()
    {
        return _objectModel;
    }
}
