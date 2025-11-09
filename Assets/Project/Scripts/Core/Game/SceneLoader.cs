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

        SceneSession session = new(payload);
        _sessions.Add(sceneName, session);

        GameLogger.Log($"Loading scene '{sceneName}' with payload of type {payload.GetType().Name}.");

        try
        {
            await LoadAdditiveAsync(sceneName).ConfigureAwait(false);
            var result = await session.CompletionSource.Task.ConfigureAwait(false);

            if (result == null)
            {
                GameLogger.Warn($"Scene '{sceneName}' completed without close data.");
                return default;
            }

            if (result is TCloseData typed)
                return typed;

            throw new InvalidCastException($"Unable to cast close data for scene '{sceneName}' to {typeof(TCloseData)}");
        }
        finally
        {
            _sessions.Remove(sceneName);
            GameLogger.Log($"Scene '{sceneName}' session was removed.");
        }
    }

    public async Task LoadAdditiveAsync(string sceneName)
    {
        GameLogger.Log($"Loading additive scene '{sceneName}'.");

        AsyncOperation sceneLoading = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!sceneLoading.isDone)
            await Task.Yield();

        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (!loadedScene.IsValid() || !loadedScene.isLoaded)
        {
            GameLogger.Error($"Scene '{sceneName}' failed to load correctly.");
            return;
        }

        SceneManager.SetActiveScene(loadedScene);

        int count = SceneManager.sceneCount;
        for (int i = 0; i < count; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.name != sceneName)
            {
                ActivateTargetScene(scene, false);
            }
        }
    }

    public async Task UnloadAdditiveWithDataAsync(string sceneName, object closeData, string returnToScene = null)
    {
        if (string.IsNullOrEmpty(sceneName))
            throw new ArgumentException("Scene name must not be null or empty", nameof(sceneName));

        if (!_sessions.TryGetValue(sceneName, out var session))
        {
            throw new InvalidOperationException($"Scene '{sceneName}' was not loaded with a data session");
        }

        await UnloadAdditiveAsync(sceneName, returnToScene).ConfigureAwait(false);
        session.CompletionSource.TrySetResult(closeData);
        GameLogger.Log($"Scene '{sceneName}' was unloaded with close data of type {closeData?.GetType().Name ?? "<null>"}.");
    }

    public async Task UnloadAdditiveAsync(string sceneName, string returnToScene = null)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded)
        {
            GameLogger.Warn($"Attempted to unload scene '{sceneName}' but it is not loaded.");
            return;
        }

        AsyncOperation sceneLoading = SceneManager.UnloadSceneAsync(scene);
        while (!sceneLoading.isDone)
            await Task.Yield();

        if (!string.IsNullOrEmpty(returnToScene))
        {
            Scene parentScene = SceneManager.GetSceneByName(returnToScene);
            if (parentScene.isLoaded)
            {
                ActivateTargetScene(parentScene, true);
            }
            else
            {
                GameLogger.Warn($"Requested to activate return scene '{returnToScene}', but it is not loaded.");
            }
        }
    }

    public bool TryGetScenePayload<TPayload>(string sceneName, out TPayload payload)
    {
        if (_sessions.TryGetValue(sceneName, out var session) && session.TryGetPayload(out payload))
        {
            GameLogger.Log($"Payload for scene '{sceneName}' resolved as type {typeof(TPayload).Name}.");
            return true;
        }

        payload = default;
        GameLogger.Warn($"Failed to resolve payload for scene '{sceneName}' as type {typeof(TPayload).Name}.");
        return false;
    }

    public void LoadScene(string sceneName)
    {
        GameLogger.Log($"Loading scene '{sceneName}' in single mode.");

        SceneManager.LoadScene(sceneName);
        Scene active = SceneManager.GetActiveScene();

        if (active.IsValid() && active.name == sceneName)
        {
            SceneUtils.SetSceneActiveObjects(sceneName, true);
        }
        else
        {
            GameLogger.Warn($"Scene '{sceneName}' was requested to load, but the active scene after load is '{active.name}'.");
        }
    }

    private void ActivateTargetScene(Scene sceneToActivate, bool isActive)
    {
        if (!sceneToActivate.IsValid() || !sceneToActivate.isLoaded)
        {
            GameLogger.Warn($"Cannot change activation state for scene '{sceneToActivate.name}' because it is invalid or not loaded.");
            return;
        }

        var isRootScene = string.Equals(sceneToActivate.name, RootSceneName, StringComparison.Ordinal);

        if (isActive)
        {
            SceneManager.SetActiveScene(sceneToActivate);
            GameLogger.Log($"Scene '{sceneToActivate.name}' set as active scene.");
        }

        if (!isRootScene || isActive)
        {
            SceneUtils.SetSceneActiveObjects(sceneToActivate.name, isActive);
            GameLogger.Log($"Scene '{sceneToActivate.name}' root objects set active: {isActive}.");
            return;
        }

        GameLogger.Log($"Skipping deactivation of root scene '{sceneToActivate.name}'.");
    }
}
