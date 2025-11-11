using UnityEngine;
using UnityEngine.Rendering.Universal;
using VContainer;
using VContainer.Unity;

public class DangeonSceneManager : MonoBehaviour
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private Transform _playerSpawnPoint;
    [SerializeField] private ArmyRoasterView _armyRoasterView;

    [Inject] private readonly GameSession _gameSession;
    [Inject] private readonly AudioManager _audioManager;
    [Inject] private readonly InputService _inputService;
    [Inject] private readonly IObjectResolver _objectResolver;
    [Inject] private readonly DangeonLevelSceneUIController _uiController;
    
    private void Start()
    {
        _ = _audioManager.PlayClipAsync("BackgroundEffect", "DropsInTheCave");
        _inputService.EnterGameplay();

        PlayerController playerController = InitializePlayer();
        PlayerArmyController playerArmyController = InitializeArmy(playerController);

        _armyRoasterView.Render(playerArmyController);

        if (_uiController != null)
        {
            _uiController.BindArmyController(playerArmyController);
        }
    }

    private PlayerController InitializePlayer()
    {
        Vector3 spawnPosition = _playerSpawnPoint.position;
        Quaternion spawnRotation = _playerSpawnPoint.rotation;

        GameObject playerInstance = _objectResolver.Instantiate(_playerPrefab, spawnPosition, spawnRotation);
        PlayerController playerController = playerInstance.GetComponent<PlayerController>();
        SquadModel squadModel = new(_gameSession.SelectedHero, 1);

        playerController.Initialize(squadModel);

        return playerController;
    }

    private PlayerArmyController InitializeArmy(PlayerController playerController)
    {
        PlayerArmyController armyController = playerController.GetComponent<PlayerArmyController>();
        ArmyModel armyModel = new(armyController.MaxSlots);

        armyController.Initialize(armyModel);

        foreach (var definition in _gameSession.SelectedAllySquads)
        {
            const int defaultAmount = 10;
            armyController.Army.TryAddSquad(definition, defaultAmount);
        }

        return armyController;
    }

}
