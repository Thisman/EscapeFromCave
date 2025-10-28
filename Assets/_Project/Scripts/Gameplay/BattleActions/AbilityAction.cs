using System;
using UnityEngine;

public sealed class AbilityAction : IBattleAction, IDisposable
{
    private readonly BattleAbilityDefinitionSO _ability;
    private IActionTargetPicker _targetPicker;
    private bool _disposed;
    private bool _resolved;
    private bool _targetRequested;

    public event Action OnResolve;
    public event Action OnCancel;

    public BattleAbilityDefinitionSO Ability => _ability;

    public AbilityAction(BattleAbilityDefinitionSO ability, IActionTargetPicker targetPicker)
    {
        _ability = ability ?? throw new ArgumentNullException(nameof(ability));
        _targetPicker = targetPicker ?? throw new ArgumentNullException(nameof(targetPicker));
    }

    public void Resolve()
    {
        if (_disposed || _resolved || _targetRequested || _targetPicker == null)
            return;

        _targetRequested = true;
        _targetPicker.OnSelect += HandleTargetSelected;
        _targetPicker.RequestTarget();
    }

    private void HandleTargetSelected(BattleSquadController unit)
    {
        if (_disposed || _resolved)
            return;

        if (_targetPicker != null)
        {
            _targetPicker.OnSelect -= HandleTargetSelected;
            _targetPicker.Dispose();
            _targetPicker = null;
        }

        _targetRequested = false;

        if (unit == null)
        {
            CompleteResolve();
            return;
        }

        var targetModel = unit.GetSquadModel();
        if (targetModel == null)
        {
            CompleteResolve();
            return;
        }

        var definition = targetModel.Definition;
        if (definition == null || !definition.IsEnemy())
        {
            Debug.LogWarning("[AbilityAction] Selected target is not a valid enemy.");
            CompleteResolve();
            return;
        }

        string abilityName = !string.IsNullOrEmpty(_ability.AbilityName)
            ? _ability.AbilityName
            : _ability.name;
        string targetName = !string.IsNullOrEmpty(definition.UnitName)
            ? definition.UnitName
            : definition.name;

        Debug.Log($"Ability '{abilityName}' used on target '{targetName}'.");

        CompleteResolve();
    }

    private void CompleteResolve()
    {
        if (_resolved)
            return;

        if (_disposed)
            return;

        _resolved = true;
        OnResolve?.Invoke();
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_targetPicker != null)
        {
            _targetPicker.OnSelect -= HandleTargetSelected;
            _targetPicker.Dispose();
            _targetPicker = null;
        }

        _disposed = true;
        _targetRequested = false;

        if (!_resolved)
            OnCancel?.Invoke();
    }
}
