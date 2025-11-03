using System;
using System.Collections.Generic;

public sealed class BattleEffectsManager
{
    private readonly Dictionary<BattleSquadEffectsController, List<BattleEffectState>> _activeEffects = new();

    public void AddEffect(BattleContext _ctx, BattleEffectDefinitionSO effect, BattleSquadEffectsController target)
    {
        if (effect == null)
            throw new ArgumentNullException(nameof(effect));
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        target.AddEffect(effect);
        effect.OnAttach(_ctx, target);

        if (effect.DurationMode == BattleEffectDurationMode.Instant)
        {
            FinalizeEffect(target, effect);
            return;
        }

        if (!_activeEffects.TryGetValue(target, out var effects))
        {
            effects = new List<BattleEffectState>();
            _activeEffects[target] = effects;
        }

        effects.Add(new BattleEffectState(effect));
    }

    public void RemoveEffect(BattleEffectDefinitionSO effect, BattleSquadEffectsController target)
    {
        if (effect == null)
            throw new ArgumentNullException(nameof(effect));
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        target.RemoveEffect(effect);

        if (!_activeEffects.TryGetValue(target, out var effects))
        {
            return;
        }

        for (int i = effects.Count - 1; i >= 0; i--)
        {
            if (effects[i].Effect == effect)
            {
                effect.OnRemove();
                effects.RemoveAt(i);
                break;
            }
        }

        if (effects.Count == 0)
        {
            _activeEffects.Remove(target);
        }
    }

    public void OnTick()
    {
        var controllers = new List<BattleSquadEffectsController>(_activeEffects.Keys);
        foreach (var controller in controllers)
        {
            if (controller == null)
            {
                if (_activeEffects.TryGetValue(controller, out var orphanedEffects))
                {
                    foreach (var orphan in orphanedEffects)
                    {
                        orphan.Effect.OnRemove();
                    }
                }

                _activeEffects.Remove(controller);
                continue;
            }

            if (!_activeEffects.TryGetValue(controller, out var effects) || effects.Count == 0)
            {
                _activeEffects.Remove(controller);
                continue;
            }

            for (int i = effects.Count - 1; i >= 0; i--)
            {
                var state = effects[i];
                state.TickCount++;
                state.Effect.OnTick();

                if (ShouldEffectExpire(state))
                {
                    controller.RemoveEffect(state.Effect);
                    state.Effect.OnRemove();
                    effects.RemoveAt(i);
                }
            }

            if (effects.Count == 0)
            {
                _activeEffects.Remove(controller);
            }
        }
    }

    private static bool ShouldEffectExpire(BattleEffectState state)
    {
        var durationMode = state.Effect.DurationMode;
        return durationMode switch
        {
            BattleEffectDurationMode.Instant => true,
            BattleEffectDurationMode.TurnCount or BattleEffectDurationMode.RoundCount
                => state.Effect.MaxTick > 0 && state.TickCount >= state.Effect.MaxTick,
            BattleEffectDurationMode.UntilEvent => false,
            BattleEffectDurationMode.Infinite => false,
            _ => false,
        };
    }

    private void FinalizeEffect(BattleSquadEffectsController target, BattleEffectDefinitionSO effect)
    {
        target.RemoveEffect(effect);
        effect.OnRemove();
    }

    private sealed class BattleEffectState
    {
        public BattleEffectState(BattleEffectDefinitionSO effect)
        {
            Effect = effect;
        }

        public BattleEffectDefinitionSO Effect { get; }

        public int TickCount { get; set; }
    }
}
