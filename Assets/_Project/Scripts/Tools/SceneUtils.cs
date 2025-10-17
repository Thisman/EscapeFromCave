using UnityEngine.SceneManagement;

public static class SceneUtils
{
    public static void SetSceneActiveObjects(string sceneName, bool active)
    {
        var scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded) return;

        foreach (var root in scene.GetRootGameObjects())
            root.SetActive(active);
    }
}
