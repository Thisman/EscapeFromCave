using System;
using System.Threading.Tasks;

public sealed class SceneSession
{
    public SceneSession(ISceneLoadingPayload payload)
    {
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        CompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public ISceneLoadingPayload Payload { get; }

    public TaskCompletionSource<object> CompletionSource { get; }

    public bool TryGetPayload<TPayload>(out TPayload payload)
    {
        if (Payload is ISceneLoadingPayload<TPayload> typedPayload)
        {
            payload = typedPayload.GetData();
            return true;
        }

        payload = default;
        return false;
    }
}
