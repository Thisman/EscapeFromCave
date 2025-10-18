public interface ISceneLoadingPayload
{
}

public interface ISceneLoadingPayload<out T> : ISceneLoadingPayload
{
    T GetData();
}
