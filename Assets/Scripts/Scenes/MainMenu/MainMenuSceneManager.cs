using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using VContainer;

public class MainMenuSceneManager : MonoBehaviour
{
    [SerializeField] private MainMenuSceneUIController _mainMenuSceneUIController;

    [Inject] private readonly SceneLoader _sceneLoader;
    [Inject] private readonly InputService _inputService;
    [Inject] private readonly AudioManager _audioManager;
    [Inject] private readonly GameEventBusService _sceneEventBusService;

    public void Start()
    {
        InitializeBackgroundMusic();
    }

    private void OnEnable()
    {
        _inputService.EnterMenu();
        InitializeUI();
        SubscribeToGameEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromGameEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromGameEvents();
    }

    private Task InitializeBackgroundMusic()
    {
        return _audioManager.PlayClipAsync("BackgroundMusic", "TheHumOfCave");
    }

    private void InitializeUI()
    {
        _mainMenuSceneUIController.Initialize(_sceneEventBusService);
    }

    private void SubscribeToGameEvents()
    {
        _sceneEventBusService.SubscribeAsync<RequestGameStart>(HandleStartGameAsync);
    }

    private void UnsubscribeFromGameEvents()
    {
        _sceneEventBusService.UnsubscribeAsync<RequestGameStart>(HandleStartGameAsync);
    }

    private async Task HandleStartGameAsync(RequestGameStart _)
    {
        await _sceneLoader.LoadAdditiveAsync("PreparationScene");
        await _sceneLoader.UnloadAdditiveAsync("MainMenuScene");
    }
}
