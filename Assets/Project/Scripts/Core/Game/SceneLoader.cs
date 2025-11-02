using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneLoader
{
    private const string RootSceneName = "RootScene";
    private readonly Dictionary<string, SceneSession> _sessions = new();

    public async Task<TCloseData> LoadAdditiveWithDataAsync<TPayload, TCloseData>(string sceneName, ISceneLoadingPayload<TPayload> payload)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            throw new ArgumentException("Scene name must not be null or empty", nameof(sceneName));
        }

        if (payload == null)
        {
            throw new ArgumentNullException("[SceneLoader] Payload is null for scene", nameof(payload));
        }

        if (_sessions.ContainsKey(sceneName))
        {
            throw new InvalidOperationException($"Scene '{sceneName}' is already loaded with a data session");
        }

        var session = new SceneSession(payload);
        _sessions.Add(sceneName, session);
        Debug.Log($"[SceneLoader] Loading scene '{sceneName}' with payload of type {payload.GetType().Name}.");

        try
        {
            await LoadAdditiveAsync(sceneName).ConfigureAwait(false);
            var result = await session.CompletionSource.Task.ConfigureAwait(false);

            if (result == null)
            {
                Debug.LogWarning($"[SceneLoader] Scene '{sceneName}' completed without close data.");
                return default;
            }

            if (result is TCloseData typed)
                return typed;

            throw new InvalidCastException($"Unable to cast close data for scene '{sceneName}' to {typeof(TCloseData)}");
        }
        finally
        {
            _sessions.Remove(sceneName);
            Debug.Log($"[SceneLoader] Scene '{sceneName}' session was removed.");
        }
    }

    public async Task LoadAdditiveAsync(string sceneName)
    {
        Debug.Log($"[SceneLoader] Loading additive scene '{sceneName}'.");
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!op.isDone)
            await Task.Yield();

        var loaded = SceneManager.GetSceneByName(sceneName);
        if (!loaded.IsValid() || !loaded.isLoaded)
        {
            Debug.LogError($"[SceneLoader] Scene '{sceneName}' failed to load correctly.");
            return;
        }

        SceneManager.SetActiveScene(loaded);

        var count = SceneManager.sceneCount;
        for (int i = 0; i < count; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.name != sceneName)
            {
                ActivateTargetScene(s, false);
            }
        }
    }

    public async Task UnloadAdditiveWithDataAsync(string sceneName, object closeData, string returnToScene = null)
    {
        if (string.IsNullOrEmpty(sceneName))
            throw new ArgumentException("Scene name must not be null or empty", nameof(sceneName));

        if (!_sessions.TryGetValue(sceneName, out var session))
        {
            Debug.LogError($"[SceneLoader] Attempted to unload scene '{sceneName}' with data, but no session was registered.");
            throw new InvalidOperationException($"Scene '{sceneName}' was not loaded with a data session");
        }

        await UnloadAdditiveAsync(sceneName, returnToScene).ConfigureAwait(false);
        session.CompletionSource.TrySetResult(closeData);
        Debug.Log($"[SceneLoader] Scene '{sceneName}' was unloaded with close data of type {closeData?.GetType().Name ?? "<null>"}.");
    }

    public async Task UnloadAdditiveAsync(string sceneName, string returnToScene = null)
    {
        var scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded)
        {
            Debug.LogWarning($"[SceneLoader] Attempted to unload scene '{sceneName}' but it is not loaded.");
            return;
        }

        var op = SceneManager.UnloadSceneAsync(scene);
        while (!op.isDone)
            await Task.Yield();

        if (!string.IsNullOrEmpty(returnToScene))
        {
            var parentScene = SceneManager.GetSceneByName(returnToScene);
            if (parentScene.isLoaded)
            {
                ActivateTargetScene(parentScene, true);
            }
            else
            {
                Debug.LogWarning($"[SceneLoader] Requested to activate return scene '{returnToScene}', but it is not loaded.");
            }
        }
    }

    public bool TryGetScenePayload<TPayload>(string sceneName, out TPayload payload)
    {
        if (_sessions.TryGetValue(sceneName, out var session) && session.TryGetPayload(out payload))
        {
            Debug.Log($"[SceneLoader] Payload for scene '{sceneName}' resolved as type {typeof(TPayload).Name}.");
            return true;
        }

        payload = default;
        Debug.LogWarning($"[SceneLoader] Failed to resolve payload for scene '{sceneName}' as type {typeof(TPayload).Name}.");
        return false;
    }

    public void LoadScene(string sceneName)
    {
        Debug.Log($"[SceneLoader] Loading scene '{sceneName}' in single mode.");
        SceneManager.LoadScene(sceneName);
        var active = SceneManager.GetActiveScene();
        if (active.IsValid() && active.name == sceneName)
        {
            SceneUtils.SetSceneActiveObjects(sceneName, true);
        }
        else
        {
            Debug.LogWarning($"[SceneLoader] Scene '{sceneName}' was requested to load, but the active scene after load is '{active.name}'.");
        }
    }

    private void ActivateTargetScene(Scene sceneToActivate, bool isActive)
    {
        if (!sceneToActivate.IsValid() || !sceneToActivate.isLoaded)
        {
            Debug.LogWarning($"[SceneLoader] Cannot change activation state for scene '{sceneToActivate.name}' because it is invalid or not loaded.");
            return;
        }

        var isRootScene = string.Equals(sceneToActivate.name, RootSceneName, StringComparison.Ordinal);

        if (isActive)
        {
            SceneManager.SetActiveScene(sceneToActivate);
            Debug.Log($"[SceneLoader] Scene '{sceneToActivate.name}' set as active scene.");
        }

        if (!isRootScene || isActive)
        {
            SceneUtils.SetSceneActiveObjects(sceneToActivate.name, isActive);
            Debug.Log($"[SceneLoader] Scene '{sceneToActivate.name}' root objects set active: {isActive}.");
            return;
        }

        Debug.Log($"[SceneLoader] Skipping deactivation of root scene '{sceneToActivate.name}'.");
    }
}
