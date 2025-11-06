using System;
using System.Collections.Generic;

public sealed class BattleAbilityManager
{
    private readonly Dictionary<IReadOnlySquadModel, Dictionary<BattleAbilityDefinitionSO, AbilityCooldownState>> _cooldowns = new();

    public bool IsAbilityReady(IReadOnlySquadModel unit, BattleAbilityDefinitionSO ability)
    {
        if (!TryGetState(unit, ability, out AbilityCooldownState state))
            return true;

        return state.RemainingCooldown <= 0;
    }

    public int GetRemainingCooldown(IReadOnlySquadModel unit, BattleAbilityDefinitionSO ability)
    {
        if (!TryGetState(unit, ability, out AbilityCooldownState state))
            return 0;

        return Math.Max(0, state.RemainingCooldown);
    }

    public void TriggerCooldown(IReadOnlySquadModel unit, BattleAbilityDefinitionSO ability)
    {
        if (!TryGetState(unit, ability, out AbilityCooldownState state))
            return;

        int cooldown = ability != null ? Math.Max(0, ability.Cooldown) : 0;
        if (cooldown <= 0)
        {
            state.RemainingCooldown = 0;
            return;
        }

        // Don't cooldown in current round
        state.RemainingCooldown = cooldown + 1;
    }

    public void OnTick()
    {
        if (_cooldowns.Count == 0)
            return;

        var units = new List<IReadOnlySquadModel>(_cooldowns.Keys);
        foreach (var unit in units)
        {
            if (unit == null)
            {
                _cooldowns.Remove(unit);
                continue;
            }

            if (!_cooldowns.TryGetValue(unit, out var abilityStates) || abilityStates == null || abilityStates.Count == 0)
                continue;

            foreach (var state in abilityStates.Values)
            {
                if (state.RemainingCooldown <= 0)
                    continue;

                state.RemainingCooldown--;

                if (state.RemainingCooldown < 0)
                {
                    state.RemainingCooldown = 0;
                }
            }
        }
    }

    private bool TryGetState(IReadOnlySquadModel unit, BattleAbilityDefinitionSO ability, out AbilityCooldownState state)
    {
        state = null;

        if (unit == null || ability == null)
            return false;

        EnsureUnitTracked(unit);

        if (!_cooldowns.TryGetValue(unit, out var abilityStates) || abilityStates == null)
            return false;

        if (!abilityStates.TryGetValue(ability, out state))
        {
            state = new AbilityCooldownState(ability);
            abilityStates[ability] = state;
        }

        return true;
    }

    private void EnsureUnitTracked(IReadOnlySquadModel unit)
    {
        if (unit == null)
            return;

        if (!_cooldowns.TryGetValue(unit, out var abilityStates) || abilityStates == null)
        {
            abilityStates = new Dictionary<BattleAbilityDefinitionSO, AbilityCooldownState>();
            _cooldowns[unit] = abilityStates;
        }

        var abilities = unit.Abilities;
        if (abilities == null || abilities.Length == 0)
            return;

        for (int i = 0; i < abilities.Length; i++)
        {
            var ability = abilities[i];
            if (ability == null)
                continue;

            if (!abilityStates.ContainsKey(ability))
            {
                abilityStates[ability] = new AbilityCooldownState(ability);
            }
        }
    }

    private sealed class AbilityCooldownState
    {
        public AbilityCooldownState(BattleAbilityDefinitionSO ability)
        {
            Ability = ability;
        }

        public BattleAbilityDefinitionSO Ability { get; }

        public int RemainingCooldown { get; set; }
    }
}
