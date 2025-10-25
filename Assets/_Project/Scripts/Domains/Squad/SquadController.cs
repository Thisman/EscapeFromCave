using UnityEngine;

public class SquadController : MonoBehaviour
{
    [SerializeField] private UnitDefinitionSO _unitDefinition;

    private SquadModel _squadModel;

    public void Awake()
    {
        if (_unitDefinition == null)
        {
            Debug.LogWarning("[SquadController] Unit definition is not assigned. Squad model will not be created.");
            return;
        }

        if (_squadModel == null)
            _squadModel = new SquadModel(_unitDefinition, 1);
    }

    public IReadOnlySquadModel GetSquadModel()
    {
        return _squadModel;
    }
}
