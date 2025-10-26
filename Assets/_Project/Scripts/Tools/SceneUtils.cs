using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneUtils
{
    public static void SetSceneActiveObjects(string sceneName, bool active)
    {
        var scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded)
        {
            Debug.LogWarning($"[SceneUtils] Attempted to change activation state for scene '{sceneName}', but it is not loaded.");
            return;
        }

        foreach (var root in scene.GetRootGameObjects())
        {
            root.SetActive(active);
        }
    }
}
