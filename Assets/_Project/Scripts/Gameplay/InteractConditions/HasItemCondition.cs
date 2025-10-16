using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Conditions/Has Item")]
public sealed class HasItemCondition : ConditionSO
{
    public string RequiredItemId;

    public override bool IsMet(InteractionContext ctx)
    {
        //var inventory = ctx.Actor.GetComponent<Inventory>();
        //return inventory != null && inventory.HasItem(RequiredItemId);
        return true;
    }
}
