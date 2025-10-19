using UnityEngine;
using VContainer;

public class MainMenuSceneManager : MonoBehaviour
{
    [Inject] private readonly SceneLoader _sceneLoader;
    [Inject] private readonly InputRouter _inputRouter;

    [SerializeField] private MainMenuSceneUIController _mainMenuSceneUIController;

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
        Debug.Log("[MainMenuSceneManager] Start Game button clicked. Transitioning to Preparation Scene.");
        await _sceneLoader.LoadAdditiveAsync("PreparationScene");
        await _sceneLoader.UnloadAdditiveAsync("MainMenuScene");
    }
}
