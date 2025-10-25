using UnityEngine;

public class SquadController : MonoBehaviour
{
    [SerializeField] private int _count = 1;
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
            _squadModel = new SquadModel(_unitDefinition, _count);
    }

    public IReadOnlySquadModel GetSquadModel()
    {
        return _squadModel;
    }
}
