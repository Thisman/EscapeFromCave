using UnityEngine;

public abstract class InteractionConditionDefinitionSO : ScriptableObject
{
    public abstract bool IsMet(InteractionContext ctx);
}
