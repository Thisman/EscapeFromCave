using System;
using System.Collections.Generic;
using UnityEngine;

public class SquadController : MonoBehaviour
{
    public const int MaxAdditionalUnitDefinitions = 5;

    [SerializeField] private int _count = 1;
    [SerializeField] private UnitDefinitionSO _unitDefinition;
    [SerializeField] private AdditionalUnitDefinition[] _additionalUnitDefinitions = Array.Empty<AdditionalUnitDefinition>();

    private static readonly AdditionalUnitDefinition[] EmptyAdditionalDefinitions = Array.Empty<AdditionalUnitDefinition>();

    private SquadModel _squadModel;

    public void Awake()
    {
        _squadModel ??= new SquadModel(_unitDefinition, _count);
    }

    public IReadOnlySquadModel GetSquadModel()
    {
        return _squadModel;
    }

    public IReadOnlyList<AdditionalUnitDefinition> GetAdditionalUnitDefinitions()
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

        for (int i = 0; i < _additionalUnitDefinitions.Length; i++)
        {
            if (_additionalUnitDefinitions[i] == null)
                _additionalUnitDefinitions[i] = new AdditionalUnitDefinition();

            _additionalUnitDefinitions[i].Normalize();
        }
    }

    [Serializable]
    public sealed class AdditionalUnitDefinition
    {
        [SerializeField] private UnitDefinitionSO _definition;
        [Min(1)]
        [SerializeField] private int _count = 1;

        public UnitDefinitionSO Definition => _definition;

        public int Count => Mathf.Max(1, _count);

        public bool IsValid => _definition != null && _count > 0;

        public void Normalize()
        {
            if (_count < 1)
                _count = 1;
        }
    }
}
