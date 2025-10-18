using VContainer;

public static class GameServicesModule
{
    public static void Register(IContainerBuilder builder)
    {
        builder.Register<GameSession>(Lifetime.Singleton).As<IGameSession>();
        builder.Register<GameFlowService>(Lifetime.Singleton);
        builder.Register<SceneLoader>(Lifetime.Singleton);
    }
}
