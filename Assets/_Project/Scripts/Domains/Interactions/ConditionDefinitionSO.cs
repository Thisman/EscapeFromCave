using UnityEngine;

public abstract class ConditionDefinitionSO : ScriptableObject
{
    public abstract bool IsMet(InteractionContext ctx);
}
