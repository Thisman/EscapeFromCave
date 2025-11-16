using System.Threading.Tasks;
using UnityEngine;
using VContainer;

public class MainMenuSceneManager : MonoBehaviour
{
    [SerializeField] private MainMenuUIController _mainMenuSceneUIController;

    [Inject] private readonly SceneLoader _sceneLoader;
    [Inject] private readonly InputService _inputService;
    [Inject] private readonly AudioManager _audioManager;

    public void Start()
    {
        _ = _audioManager.PlayClipAsync("BackgroundMusic", "TheHumOfCave");
    }

    private void OnEnable()
    {
        _inputService.EnterMenu();
        _mainMenuSceneUIController.OnStartGame += HandleStartGameAsync;
    }

    private void OnDisable()
    {
        if (_mainMenuSceneUIController != null)
        {
            _mainMenuSceneUIController.OnStartGame -= HandleStartGameAsync;
        }
    }

    private async Task HandleStartGameAsync()
    {
        await _sceneLoader.LoadAdditiveAsync("PreparationScene");
        await _sceneLoader.UnloadAdditiveAsync("MainMenuScene");
    }
}
