using UnityEngine;
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
    [Inject] private readonly DungeonUIController _dungeonUIController;

    private PlayerArmyController _playerArmyController;

    private void Start()
    {
        _ = _audioManager.PlayClipAsync("BackgroundEffect", "DropsInTheCave");
        _inputService.EnterGameplay();

        PlayerController playerController = InitializePlayer();
        _playerArmyController = InitializeArmy(playerController);

        _armyRoasterView.Render(_playerArmyController);
        SubscribeToArmyChanges(_playerArmyController);
    }

    private void OnDestroy()
    {
        if (_playerArmyController != null)
        {
            _playerArmyController.ArmyChanged -= HandleArmyChanged;
        }

        _dungeonUIController?.RenderSquads(null);
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

    private void SubscribeToArmyChanges(PlayerArmyController armyController)
    {
        if (armyController == null)
        {
            return;
        }

        armyController.ArmyChanged += HandleArmyChanged;
        HandleArmyChanged(armyController.Army);
    }

    private void HandleArmyChanged(IReadOnlyArmyModel army)
    {
        if (_dungeonUIController == null)
        {
            return;
        }

        if (army == null)
        {
            _dungeonUIController.RenderSquads(null);
            return;
        }

        _dungeonUIController.RenderSquads(army.GetSquads());
    }
}
