using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public sealed class BattleEffectsManager
{
    private readonly Dictionary<BattleSquadEffectsController, List<BattleEffectState>> _activeEffects = new();

    public async Task AddEffect(BattleContext _ctx, BattleEffectSO effect, BattleSquadEffectsController target)
    {
        if (effect == null)
            throw new ArgumentNullException(nameof(effect));
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        target.AddEffect(effect);
        await effect.OnAttach(_ctx, target);

        if (!_activeEffects.TryGetValue(target, out var effects))
        {
            effects = new List<BattleEffectState>();
            _activeEffects[target] = effects;
        }

        effects.Add(new BattleEffectState(effect, _ctx));
        BattleLogger.LogEffectApplied(effect, target);
    }

    public void RemoveEffect(BattleEffectSO effect, BattleSquadEffectsController target)
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
                effect.OnRemove(effects[i].Context, target);
                effects.RemoveAt(i);
                break;
            }
        }

        if (effects.Count == 0)
        {
            _activeEffects.Remove(target);
        }
    }

    public async Task Trigger(BattleEffectTrigger trigger, BattleSquadEffectsController target = null)
    {
        if (_activeEffects.Count == 0)
            return;

        if (target != null)
        {
            await TriggerForController(trigger, target);
            return;
        }

        var controllers = new List<BattleSquadEffectsController>(_activeEffects.Keys);
        foreach (var controller in controllers)
        {
            await TriggerForController(trigger, controller);
        }
    }

    private async Task TriggerForController(BattleEffectTrigger trigger, BattleSquadEffectsController controller)
    {
        if (controller == null)
        {
            if (_activeEffects.TryGetValue(controller, out var orphanedEffects))
            {
                foreach (var orphan in orphanedEffects)
                {
                    orphan.Effect.OnRemove(orphan.Context, null);
                }
            }

            _activeEffects.Remove(controller);
            return;
        }

        if (!_activeEffects.TryGetValue(controller, out var effects) || effects.Count == 0)
        {
            _activeEffects.Remove(controller);
            return;
        }

        for (int i = effects.Count - 1; i >= 0; i--)
        {
            var state = effects[i];
            if (state.Effect.Trigger != trigger)
                continue;

            state.TickCount++;
            BattleLogger.LogEffectTriggered(state.Effect, controller, trigger);
            await state.Effect.OnTick(state.Context, controller);

            if (ShouldEffectExpire(state))
            {
                controller.RemoveEffect(state.Effect);
                state.Effect.OnRemove(state.Context, controller);
                effects.RemoveAt(i);
            }
        }

        if (effects.Count == 0)
        {
            _activeEffects.Remove(controller);
        }
    }

    private static bool ShouldEffectExpire(BattleEffectState state)
    {
        return state.Effect.MaxTick > 0 && state.TickCount >= state.Effect.MaxTick;
    }

    private sealed class BattleEffectState
    {
        public BattleEffectState(BattleEffectSO effect, BattleContext context)
        {
            Effect = effect;
            Context = context;
        }

        public BattleEffectSO Effect { get; }

        public BattleContext Context { get; }

        public int TickCount { get; set; }
    }
}
