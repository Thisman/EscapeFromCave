using System;
using System.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class RootSceneManager : MonoBehaviour
{
    [Inject] private SceneLoader _sceneLoader;
    [Inject] private AudioManager _audioManager;

    private void Start()
    {
        _ = RunStartupAsync();
    }

    private async Task RunStartupAsync()
    {
        try
        {
            await _audioManager.LoadFolderAsync("");
            await _sceneLoader.LoadAdditiveAsync("MainMenuScene");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to initialize root scene: {exception}", this);
        }
    }
}
