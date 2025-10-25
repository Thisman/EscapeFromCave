using UnityEngine;
using VContainer;

public class RootSceneManager : MonoBehaviour
{
    [Inject] private SceneLoader _sceneLoader;

    private async void Start()
    {
        await _sceneLoader.LoadAdditiveAsync("MainMenuScene");
    }
}
