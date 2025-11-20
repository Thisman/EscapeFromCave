using UnityEngine;
using VContainer;
using VContainer.Unity;

public class DungeonLifetimeScope : LifetimeScope
{
    [SerializeField] private DialogManager _dialogManager;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(_dialogManager).AsSelf();
        builder.Register<GameEventBusService>(Lifetime.Singleton);
    }
}
