using VContainer;
using VContainer.Unity;

public class PreparationLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<GameEventBusService>(Lifetime.Singleton);
    }
}
