using System.Collections.Generic;
using UnityEngine;

public static class BattleLogger
{
    private const string BattlePrefix = "[Battle]";
    private static readonly Dictionary<BattlePhaseStates, int> PhaseCounters = new();
    private static readonly Dictionary<BattleRoundStates, int> RoundStateCounters = new();

    public static void LogPhaseEntered(BattlePhaseStates phase)
    {
        int occurrence = IncrementCounter(PhaseCounters, phase);
        Debug.Log($"{BattlePrefix}[Phase] Entered phase '{phase}' (#{occurrence}).");
    }

    public static void LogRoundStateEntered(BattleRoundStates state)
    {
        int occurrence = IncrementCounter(RoundStateCounters, state);
        Debug.Log($"{BattlePrefix}[Round] Entered state '{state}' (#{occurrence}).");
    }

    public static void LogActiveUnit(IReadOnlySquadModel unit)
    {
        Debug.Log($"{BattlePrefix}[Turn] Active unit: {FormatUnitName(unit)}.");
    }

    public static void LogUnitAction(IReadOnlySquadModel unit, string actionName)
    {
        actionName = string.IsNullOrWhiteSpace(actionName) ? "Unknown action" : actionName;
        Debug.Log($"{BattlePrefix}[Action] {FormatUnitName(unit)} performs '{actionName}'.");
    }

    public static void LogEffectAdded(BattleEffectSO effect, BattleSquadEffectsController target)
    {
        Debug.Log($"{BattlePrefix}[Effect] {FormatUnitName(target)} gains effect '{FormatEffectName(effect)}'.");
    }

    public static void LogEffectTriggered(BattleEffectSO effect, BattleSquadEffectsController target, BattleEffectTrigger trigger)
    {
        Debug.Log($"{BattlePrefix}[Effect] '{FormatEffectName(effect)}' triggered on {FormatUnitName(target)} ({trigger}).");
    }

    public static void LogUnitDodged(IReadOnlySquadModel unit, DamageType damageType)
    {
        Debug.Log($"{BattlePrefix}[Damage] {FormatUnitName(unit)} dodged {damageType} damage.");
    }

    public static void LogDamageTaken(IReadOnlySquadModel unit, BattleDamageData damageData, int appliedDamage, float defense)
    {
        string damageType = damageData?.DamageType.ToString() ?? "Unknown";
        int rawDamage = damageData?.Value ?? 0;
        Debug.Log($"{BattlePrefix}[Damage] {FormatUnitName(unit)} takes {appliedDamage} {damageType} damage (raw={rawDamage}, defense={defense:P0}).");
    }

    public static void LogUnitHealth(IReadOnlySquadModel unit, int newHealth)
    {
        Debug.Log($"{BattlePrefix}[Health] {FormatUnitName(unit)} new health: {Mathf.Max(0, newHealth)}.");
    }

    public static void LogUnitDefeated(IReadOnlySquadModel unit)
    {
        Debug.Log($"{BattlePrefix}[Death] {FormatUnitName(unit)} has been defeated.");
    }

    private static int IncrementCounter<T>(Dictionary<T, int> counters, T key)
    {
        if (!counters.TryGetValue(key, out int count))
        {
            count = 0;
        }

        count++;
        counters[key] = count;
        return count;
    }

    private static string FormatUnitName(IReadOnlySquadModel unit)
    {
        return string.IsNullOrWhiteSpace(unit?.UnitName) ? "Unknown Unit" : unit.UnitName;
    }

    private static string FormatUnitName(BattleSquadEffectsController target)
    {
        if (target == null)
            return "Unknown Unit";

        var squadController = target.GetComponentInParent<BattleSquadController>();
        return FormatUnitName(squadController?.GetSquadModel());
    }

    private static string FormatEffectName(BattleEffectSO effect)
    {
        if (effect == null)
            return "Unknown Effect";

        if (!string.IsNullOrWhiteSpace(effect.Name))
            return effect.Name;

        return effect.name;
    }
}
