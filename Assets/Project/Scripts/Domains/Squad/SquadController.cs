using System;
using System.Collections.Generic;
using UnityEngine;

public class SquadController : MonoBehaviour
{
    public const int MaxAdditionalUnitDefinitions = 5;

    [SerializeField] private int _count = 1;
    [SerializeField] private UnitDefinitionSO _unitDefinition;
    [SerializeField] private UnitDefinitionSO[] _additionalUnitDefinitions = Array.Empty<UnitDefinitionSO>();

    private static readonly UnitDefinitionSO[] EmptyAdditionalDefinitions = Array.Empty<UnitDefinitionSO>();

    private SquadModel _squadModel;

    public void Awake()
    {
        _squadModel ??= new SquadModel(_unitDefinition, _count);
    }

    public IReadOnlySquadModel GetSquadModel()
    {
        return _squadModel;
    }

    public IReadOnlyList<UnitDefinitionSO> GetAdditionalUnitDefinitions()
    {
        if (_additionalUnitDefinitions == null || _additionalUnitDefinitions.Length == 0)
            return EmptyAdditionalDefinitions;

        if (_additionalUnitDefinitions.Length > MaxAdditionalUnitDefinitions)
            Array.Resize(ref _additionalUnitDefinitions, MaxAdditionalUnitDefinitions);

        return _additionalUnitDefinitions;
    }

    private void OnValidate()
    {
        if (_additionalUnitDefinitions == null)
        {
            _additionalUnitDefinitions = EmptyAdditionalDefinitions;
            return;
        }

        if (_additionalUnitDefinitions.Length > MaxAdditionalUnitDefinitions)
        {
            Array.Resize(ref _additionalUnitDefinitions, MaxAdditionalUnitDefinitions);
        }
    }
}
