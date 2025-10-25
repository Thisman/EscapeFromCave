using UnityEngine;
using VContainer;
using VContainer.Unity;

public class DangeonLifetimeScope : LifetimeScope
{
    [SerializeField] private DialogManager _dialogManager;

    protected override void Configure(IContainerBuilder builder)
    {
        if (_dialogManager != null)
        {
            builder.RegisterInstance(_dialogManager).AsSelf();
        }
        else
        {
            Debug.LogWarning("[GameLifetimeScope] DialogManager reference is missing. Dialog interactions will be unavailable.");
        }
    }
}
