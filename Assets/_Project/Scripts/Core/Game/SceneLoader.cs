using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneLoader
{
    private readonly Dictionary<string, SceneSession> _sessions = new();

    public async Task<TCloseData> LoadAdditiveWithDataAsync<TPayload, TCloseData>(string sceneName, ISceneLoadingPayload<TPayload> payload)
    {
        if (string.IsNullOrEmpty(sceneName))
            throw new ArgumentException("Scene name must not be null or empty", nameof(sceneName));

        if (payload == null)
            throw new ArgumentNullException(nameof(payload));

        if (_sessions.ContainsKey(sceneName))
            throw new InvalidOperationException($"Scene '{sceneName}' is already loaded with a data session");

        var session = new SceneSession(payload);
        _sessions.Add(sceneName, session);

        try
        {
            await LoadAdditiveAsync(sceneName).ConfigureAwait(false);
            var result = await session.CompletionSource.Task.ConfigureAwait(false);

            if (result == null)
                return default;

            if (result is TCloseData typed)
                return typed;

            throw new InvalidCastException($"Unable to cast close data for scene '{sceneName}' to {typeof(TCloseData)}");
        }
        finally
        {
            _sessions.Remove(sceneName);
        }
    }

    public async Task LoadAdditiveAsync(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!op.isDone)
            await Task.Yield();

        var loaded = SceneManager.GetSceneByName(sceneName);
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
            throw new InvalidOperationException($"Scene '{sceneName}' was not loaded with a data session");

        await UnloadAdditiveAsync(sceneName, returnToScene).ConfigureAwait(false);
        session.CompletionSource.TrySetResult(closeData);
    }

    public async Task UnloadAdditiveAsync(string sceneName, string returnToScene = null)
    {
        var scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded)
            return;

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
        }
    }

    public bool TryGetScenePayload<TPayload>(string sceneName, out TPayload payload)
    {
        if (_sessions.TryGetValue(sceneName, out var session) && session.TryGetPayload(out payload))
        {
            return true;
        }

        payload = default;
        return false;
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        var active = SceneManager.GetActiveScene();
        if (active.IsValid() && active.name == sceneName)
        {
            SceneUtils.SetSceneActiveObjects(sceneName, true);
        }
    }

    private void ActivateTargetScene(Scene sceneToActivate, bool isActive)
    {
        if (!sceneToActivate.IsValid() || !sceneToActivate.isLoaded)
            return;

        if (isActive)
        {
            SceneManager.SetActiveScene(sceneToActivate);
        }

        SceneUtils.SetSceneActiveObjects(sceneToActivate.name, isActive);
    }
}
