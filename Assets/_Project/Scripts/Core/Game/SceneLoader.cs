using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public sealed class SceneLoader
{
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
