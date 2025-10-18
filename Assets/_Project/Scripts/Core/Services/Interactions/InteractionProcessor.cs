using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class InteractionProcessor
{
    private static readonly IReadOnlyList<GameObject> EmptyTargets = Array.Empty<GameObject>();

    public bool TryProcess(InteractableDefinitionSO definition, InteractionContext context)
    {
        if (definition == null)
            return false;
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (!AreConditionsSatisfied(definition, context))
            return false;

        var targets = ResolveTargets(definition, context);
        ExecuteEffects(definition, context, targets);
        return true;
    }

    private static bool AreConditionsSatisfied(InteractableDefinitionSO definition, InteractionContext context)
    {
        if (definition.Conditions == null || definition.Conditions.Length == 0)
            return true;

        foreach (var condition in definition.Conditions)
        {
            if (condition == null)
                continue;

            if (!condition.IsMet(context))
                return false;
        }

        return true;
    }

    private static IReadOnlyList<GameObject> ResolveTargets(InteractableDefinitionSO definition, InteractionContext context)
    {
        if (definition.TargetResolver == null)
            return EmptyTargets;

        var result = definition.TargetResolver.Resolve(context);
        return result ?? EmptyTargets;
    }

    private static void ExecuteEffects(InteractableDefinitionSO definition, InteractionContext context, IReadOnlyList<GameObject> targets)
    {
        if (definition.Effects == null || definition.Effects.Length == 0)
            return;

        foreach (var effect in definition.Effects)
        {
            if (effect == null)
                continue;

            effect.Apply(context, targets);
        }
    }
}
