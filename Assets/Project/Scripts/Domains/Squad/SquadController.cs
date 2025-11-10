using System;
using UnityEngine;

[Serializable]
public class AdditionalSquad
{
    public AdditionalSquad(UnitSO definition, int count)
    {
        _definition = definition;
        _count = Math.Max(0, count);
    }

    [SerializeField] private UnitSO _definition;

    [Min(1), SerializeField] private int _count = 1;

    public UnitSO Definition => _definition;

    public int Count => _count;
}

public class SquadController : MonoBehaviour
{
    [SerializeField] private int _count = 1;
    [SerializeField] private UnitSO _unitDefinition;
    [SerializeField] private AdditionalSquad[] _additionalSquads = Array.Empty<AdditionalSquad>();

    private SquadModel _squadModel;

    public void Awake()
    {
        _squadModel ??= new SquadModel(_unitDefinition, _count);
    }

    public IReadOnlySquadModel GetSquadModel()
    {
        return _squadModel;
    }

    public AdditionalSquad[] GetAdditionalSquads()
    {
        return _additionalSquads;
    }
}
