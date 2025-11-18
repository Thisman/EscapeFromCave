using System;
using UnityEngine;

public sealed class BattleActionAbility : IBattleAction, IDisposable, IBattleActionTargetResolverProvider
{
    private readonly BattleContext _ctx;
    private readonly BattleAbilitySO _ability;
    private readonly IBattleActionTargetResolver _targetResolver;
    
    private bool _disposed;
    private bool _resolved;
    private bool _targetRequested;
    private IBattleActionTargetPicker _targetPicker;

    public event Action OnResolve;
    public event Action OnCancel;

    public BattleAbilitySO Ability => _ability;

    public BattleActionAbility(
        BattleContext ctx,
        BattleAbilitySO ability,
        IBattleActionTargetResolver targetResolver,
        IBattleActionTargetPicker targetPicker)
    {
        _ctx = ctx;
        _ability = ability != null ? ability : throw new ArgumentNullException(nameof(ability));
        _targetResolver = targetResolver ?? throw new ArgumentNullException(nameof(targetResolver));
        _targetPicker = targetPicker ?? throw new ArgumentNullException(nameof(targetPicker));
    }

    public IBattleActionTargetResolver TargetResolver => _targetResolver;

    public void Resolve()
    {
        if (_disposed || _resolved || _targetRequested || _targetPicker == null)
            return;

        _targetRequested = true;
        _targetPicker.OnSelect += HandleTargetSelected;
        _targetPicker.RequestTarget();
    }

    private async void HandleTargetSelected(BattleSquadController unit)
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

        var actorModel = _ctx?.ActiveUnit;
        if (actorModel == null)
        {
            CompleteResolve();
            return;
        }

        if (!_targetResolver.ResolveTarget(actorModel, targetModel))
        {
            CompleteResolve();
            return;
        }

        await _ability.Apply(_ctx, unit);

        var abilityManager = _ctx.BattleAbilitiesManager;
        var caster = _ctx.ActiveUnit;
        if (abilityManager != null && caster != null)
        {
            abilityManager.StartCooldown(caster, _ability);
            _ctx.BattleSceneUIController.RefreshAbilityAvailability();
        }

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
