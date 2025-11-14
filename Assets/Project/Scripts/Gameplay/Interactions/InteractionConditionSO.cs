using UnityEngine;

public abstract class InteractionConditionSO : ScriptableObject
{
    public abstract bool IsMet(InteractionContext ctx);
}
