using UnityEngine;
using VContainer;

public class MainMenuSceneManager : MonoBehaviour
{
    [SerializeField] private MainMenuUIController _mainMenuSceneUIController;

    [Inject] private readonly SceneLoader _sceneLoader;
    [Inject] private readonly InputRouter _inputRouter;

    private void OnEnable()
    {
        _inputRouter.EnterMenu();
        _mainMenuSceneUIController.OnStartGame += HandleStartGame;
    }

    private void OnDisable()
    {
        _mainMenuSceneUIController.OnStartGame -= HandleStartGame;
    }

    private async void HandleStartGame()
    {
        await _sceneLoader.LoadAdditiveAsync("PreparationScene");
        await _sceneLoader.UnloadAdditiveAsync("MainMenuScene");
    }
}
