using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public sealed class SceneLoader
{
    public async Task LoadAdditiveAsync(string sceneName, bool setActive = true)
    {
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        while (!op.isDone) await Task.Yield();

        if (setActive)
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
    }

    public async Task UnloadAsync(string sceneName)
    {
        var op = SceneManager.UnloadSceneAsync(sceneName);
        while (!op.isDone) await Task.Yield();
    }

    public async Task LoadSingleAsync(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!op.isDone) await Task.Yield();
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
