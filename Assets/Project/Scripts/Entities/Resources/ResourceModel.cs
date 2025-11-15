using UnityEngine;

public class ResourceModel: IReadonlyResourceModel
{
    public int Count { get; set; }

    public Sprite Icon { get; set; }

    public ResourceModel(Sprite icon, int count)
    {
        Icon = icon;
        Count = count;
    }
}
