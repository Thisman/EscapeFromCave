using UnityEngine;
using VContainer;
using VContainer.Unity;

public class DangeonLifetimeScope : LifetimeScope
{
    [SerializeField] private DialogManager _dialogManager;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(_dialogManager).AsSelf();
    }
}
