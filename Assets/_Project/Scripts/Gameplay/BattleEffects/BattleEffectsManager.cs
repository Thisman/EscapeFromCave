using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BattleEffectsManager
{
    private readonly Dictionary<IReadOnlySquadModel, List<BattleEffectModel>> _effectsByTarget = new();

    private readonly List<BattleEffectModel> _removalBuffer = new();

    private readonly List<IReadOnlySquadModel> _targetBuffer = new();

    public void ApplyEffect(BattleContext ctx, IReadOnlySquadModel target, BattleEffectDefinitionSO effect)
    {
        if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

        if (target == null)
            throw new ArgumentNullException(nameof(target));

        if (effect == null)
            throw new ArgumentNullException(nameof(effect));

        var effects = GetOrCreateEffectList(target);
        var existingEffect = FindEffectInstance(effects, effect);

        if (existingEffect != null)
        {
            ApplyStackingPolicy(existingEffect, effect);
            effect.OnApply(ctx);

            if (effect.Trigger == BattleEffectTrigger.OnApply)
                effect.OnTick(ctx);

            if (ShouldRemoveImmediately(existingEffect))
                RemoveEffectInternal(ctx, target, existingEffect);

            return;
        }

        var controller = FindEffectsController(ctx, target);
        var instance = new BattleEffectModel(effect, target);
        effects.Add(instance);
        controller?.AddEffect(instance);

        effect.OnAttach(ctx);
        effect.OnApply(ctx);

        if (effect.Trigger == BattleEffectTrigger.OnApply)
            effect.OnTick(ctx);

        if (ShouldRemoveImmediately(instance))
            RemoveEffectInternal(ctx, target, instance);
    }

    public void RemoveEffect(BattleContext ctx, IReadOnlySquadModel target, BattleEffectDefinitionSO effect)
    {
        if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

        if (target == null)
            throw new ArgumentNullException(nameof(target));

        if (effect == null)
            throw new ArgumentNullException(nameof(effect));

        if (!_effectsByTarget.TryGetValue(target, out var effects) || effects == null)
            return;

        var instance = FindEffectInstance(effects, effect);
        if (instance == null)
            return;

        RemoveEffectInternal(ctx, target, instance);
    }

    public void OnTick(BattleContext ctx, BattleRoundTrigger trigger)
    {
        if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

        _targetBuffer.Clear();

        foreach (var target in _effectsByTarget.Keys)
        {
            _targetBuffer.Add(target);
        }

        foreach (var target in _targetBuffer)
        {
            if (!_effectsByTarget.TryGetValue(target, out var effects) || effects == null)
                continue;

            if (effects.Count == 0)
                continue;

            _removalBuffer.Clear();

            foreach (var instance in effects)
            {
                if (instance == null)
                    continue;

                var definition = instance.Definition;
                if (definition == null)
                    continue;

                definition.OnBattleRoundState(ctx);

                if (definition.Trigger == BattleEffectTrigger.OnPhase)
                    definition.OnTick(ctx);

                if (!instance.ShouldProcessTrigger(trigger))
                    continue;

                if (!instance.TickDuration())
                    continue;

                _removalBuffer.Add(instance);
            }

            if (_removalBuffer.Count == 0)
                continue;

            foreach (var instance in _removalBuffer)
                RemoveEffectInternal(ctx, target, instance);
        }
    }

    private static void ApplyStackingPolicy(BattleEffectModel instance, BattleEffectDefinitionSO effect)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        if (effect == null)
            throw new ArgumentNullException(nameof(effect));

        switch (effect.StackPolicy)
        {
            case BattleEffectStackPolicy.RefreshDuration:
                instance.RefreshDuration();
                break;

            case BattleEffectStackPolicy.AddStacks:
                instance.IncreaseStack();
                break;

            case BattleEffectStackPolicy.Ignore:
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(effect.StackPolicy), effect.StackPolicy, "Unsupported stack policy");
        }
    }

    private void RemoveEffectInternal(BattleContext ctx, IReadOnlySquadModel target, BattleEffectModel instance)
    {
        if (instance == null)
            return;

        if (!_effectsByTarget.TryGetValue(target, out var effects) || effects == null)
            return;

        if (!effects.Remove(instance))
            return;

        if (effects.Count == 0)
            _effectsByTarget.Remove(target);

        var controller = FindEffectsController(ctx, target);
        controller?.RemoveEffect(instance);

        instance.Definition?.OnRemove(ctx);
    }

    private List<BattleEffectModel> GetOrCreateEffectList(IReadOnlySquadModel target)
    {
        if (!_effectsByTarget.TryGetValue(target, out var effects) || effects == null)
        {
            effects = new List<BattleEffectModel>();
            _effectsByTarget[target] = effects;
        }

        return effects;
    }

    private static BattleEffectModel FindEffectInstance(List<BattleEffectModel> effects, BattleEffectDefinitionSO effect)
    {
        if (effects == null || effect == null)
            return null;

        foreach (var instance in effects)
        {
            if (instance?.Definition == effect)
                return instance;
        }

        return null;
    }

    private static bool ShouldRemoveImmediately(BattleEffectModel instance)
    {
        if (instance?.Definition == null)
            return true;

        if (instance.Definition.DurationMode == BattleEffectDurationMode.Instant)
            return true;

        if (instance.Definition.DurationMode is BattleEffectDurationMode.TurnCount or BattleEffectDurationMode.RoundCount)
            return instance.RemainingDuration <= 0;

        return false;
    }

    private static BattleSquadEffectsController FindEffectsController(BattleContext ctx, IReadOnlySquadModel target)
    {
        if (ctx?.BattleUnits == null)
            return null;

        foreach (var unit in ctx.BattleUnits)
        {
            if (unit == null)
                continue;

            if (unit.GetSquadModel() != target)
                continue;

            return unit.GetComponent<BattleSquadEffectsController>();
        }

        return null;
    }
}
