using VContainer;

public static class InteractionModule
{
    public static void Register(IContainerBuilder builder)
    {
        builder.Register<InteractionProcessor>(Lifetime.Singleton);
    }
}
