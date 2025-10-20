using System.Collections;
using UnityEngine;

public class PlayerResourcesController : MonoBehaviour
{
    public int ResourceCount { get; private set; }

    public void AddResources(ResourceModel resourceModel)
    {
        ResourceCount += resourceModel.Count;
    }
}
