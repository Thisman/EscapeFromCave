using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneLoader
{
    private sealed class SceneSession
    {
        public SceneSession(object payload)
        {
            Payload = payload;
            CompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public object Payload { get; }
        public TaskCompletionSource<object> CompletionSource { get; }
    }

    private readonly Dictionary<string, SceneSession> _sessions = new();

    public async Task LoadAdditiveAsync(string sceneName, bool setActive = true)
    {
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!op.isDone)
            await Task.Yield();

        var loaded = SceneManager.GetSceneByName(sceneName);
        if (setActive)
        {
            SetActiveSceneExclusive(loaded);
        }
        else
        {
            SceneUtils.SetSceneActiveObjects(sceneName, false);
        }
    }

    public async Task<TCloseData> LoadAdditiveWithDataAsync<TCloseData>(string sceneName, object payload, bool setActive = true)
    {
        if (string.IsNullOrEmpty(sceneName))
            throw new ArgumentException("Scene name must not be null or empty", nameof(sceneName));

        if (_sessions.ContainsKey(sceneName))
            throw new InvalidOperationException($"Scene '{sceneName}' is already loaded with a data session");

        var session = new SceneSession(payload);
        _sessions.Add(sceneName, session);

        try
        {
            await LoadAdditiveAsync(sceneName, setActive).ConfigureAwait(false);
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

    public bool TryGetScenePayload<TPayload>(string sceneName, out TPayload payload)
    {
        if (_sessions.TryGetValue(sceneName, out var session) && session.Payload is TPayload typed)
        {
            payload = typed;
            return true;
        }

        payload = default;
        return false;
    }

    public async Task CloseAdditiveWithDataAsync(string sceneName, object closeData, string returnToScene = null)
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
                SetActiveSceneExclusive(parentScene);
            }
        }
    }

    public async Task LoadSingleAsync(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!op.isDone)
            await Task.Yield();

        // На всякий случай принудительно активируем корневые объекты новой активной сцены
        var active = SceneManager.GetActiveScene();
        if (active.IsValid() && active.name == sceneName)
        {
            SceneUtils.SetSceneActiveObjects(sceneName, true);
        }
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

    private void SetActiveSceneExclusive(Scene sceneToActivate)
    {
        if (!sceneToActivate.IsValid() || !sceneToActivate.isLoaded)
            return;

        SceneManager.SetActiveScene(sceneToActivate);

        var count = SceneManager.sceneCount;
        for (int i = 0; i < count; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            bool isActive = s == sceneToActivate;
            SceneUtils.SetSceneActiveObjects(s.name, isActive);
        }
    }
}
