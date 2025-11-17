using UnityEngine;

public interface IReadonlyResourceModel
{
    int Count { get; }

    Sprite Icon { get; }
}
