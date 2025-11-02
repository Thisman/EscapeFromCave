using System;
using System.Threading.Tasks;
using UnityEngine;

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
            Debug.Log($"[SceneSession] Successfully resolved payload to type {typeof(TPayload).Name}.");
            return true;
        }

        payload = default;
        Debug.LogWarning($"[SceneSession] Unable to resolve payload to type {typeof(TPayload).Name}. Actual type: {Payload?.GetType().Name ?? "<null>"}.");
        return false;
    }
}
