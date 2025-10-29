using UnityEngine;
using VContainer;
using VContainer.Unity;

public class RootSceneManager : MonoBehaviour
{
    [Inject] private SceneLoader _sceneLoader;
    [Inject] private AudioManager _audioManager;

    private async void Start()
    {
        await _audioManager.LoadFolderAsync("");
        await _sceneLoader.LoadAdditiveAsync("MainMenuScene");
    }
}
