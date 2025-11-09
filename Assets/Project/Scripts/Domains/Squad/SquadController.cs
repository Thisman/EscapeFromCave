using System;
using UnityEngine;

[Serializable]
public class AdditionalSquadSetup
{
    public AdditionalSquadSetup(UnitDefinitionSO definition, int count)
    {
        _definition = definition;
        _count = Math.Max(0, count);
    }

    [SerializeField] private UnitDefinitionSO _definition;

    [Min(1), SerializeField] private int _count = 1;

    public UnitDefinitionSO Definition => _definition;

    public int Count => _count;
}

public class SquadController : MonoBehaviour
{
    [SerializeField] private int _count = 1;
    [SerializeField] private UnitDefinitionSO _unitDefinition;
    [SerializeField] private AdditionalSquadSetup[] _additionalSquads = Array.Empty<AdditionalSquadSetup>();

    private SquadModel _squadModel;

    public void Awake()
    {
        _squadModel ??= new SquadModel(_unitDefinition, _count);
    }

    public IReadOnlySquadModel GetSquadModel()
    {
        return _squadModel;
    }

    public AdditionalSquadSetup[] GetAdditionalSquads()
    {
        return _additionalSquads;
    }
}
