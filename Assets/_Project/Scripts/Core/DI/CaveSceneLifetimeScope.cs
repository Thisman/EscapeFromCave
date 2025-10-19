using UnityEngine;
using VContainer;
using VContainer.Unity;

public class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private DialogController dialogController;

    protected override void Configure(IContainerBuilder builder)
    {
        if (dialogController != null)
        {
            builder.RegisterInstance(dialogController).AsSelf();
        }
        else
        {
            Debug.LogWarning("[GameLifetimeScope] DialogController reference is missing. Dialog interactions will be unavailable.");
        }
    }
}
