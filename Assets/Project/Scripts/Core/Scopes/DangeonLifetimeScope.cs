using UnityEngine;
using VContainer;
using VContainer.Unity;

public class DangeonLifetimeScope : LifetimeScope
{
    [SerializeField] private DialogManager _dialogManager;
    [SerializeField] private SquadInfoUIController _squadInfoUIController;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(_dialogManager).AsSelf();
        builder.RegisterInstance(_squadInfoUIController).AsSelf();
    }
}
