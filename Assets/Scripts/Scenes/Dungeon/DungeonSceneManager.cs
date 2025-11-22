using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class DungeonSceneManager : MonoBehaviour
{
    [SerializeField] private Camera _debugCamera;
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private Transform _playerSpawnPoint;

    [SerializeField] private UpgradesUIController _upgradesUIController;
    [SerializeField] private DungeonSceneUIController _dungeonUIController;

    [Inject] private readonly GameSession _gameSession;
    [Inject] private readonly AudioManager _audioManager;
    [Inject] private readonly InputService _inputService;
    [Inject] private readonly IObjectResolver _objectResolver;
    [Inject] private readonly GameEventBusService _sceneEventBusService;

    private PlayerController _playerController;
    private PlayerArmyController _playerArmyController;
    private UpgradeSystem _upgradeSystem;

    private IReadOnlySquadModel _hero;
    private readonly List<IReadOnlySquadModel> _squadsWithHero = new();

    private void Awake()
    {
        _debugCamera.gameObject.SetActive(false);
        InitializePlayer();
        InitializeArmy();

        _upgradeSystem = new UpgradeSystem(_playerController, _playerArmyController);
        _upgradesUIController.Initialize(_sceneEventBusService);
    }

    private void OnEnable()
    {
        _debugCamera.gameObject.SetActive(false);
        SubscribeToGameEvents();
    }

    private void Start()
    {
        _ = _audioManager.PlayClipAsync("BackgroundEffect", "DropsInTheCave");
        _inputService.EnterGameplay();

        UpdateSquadsWithHero(_playerArmyController.Army);
        _dungeonUIController.Initialize(_sceneEventBusService);
        _dungeonUIController.RenderSquads(_squadsWithHero);
    }

    private void OnDisable()
    {
        UnsubscribeFromGameEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromGameEvents();
    }

    private void InitializePlayer()
    {
        _playerSpawnPoint.GetPositionAndRotation(out Vector3 spawnPosition, out Quaternion spawnRotation);
        SquadModel squadModel = new(_gameSession.SelectedHero, 1);
        GameObject playerInstance = _objectResolver.Instantiate(_playerPrefab, spawnPosition, spawnRotation);
        _playerController = playerInstance.GetComponent<PlayerController>();

        _playerController.Initialize(squadModel);
        _hero = _playerController.GetPlayer();
    }

    private void InitializeArmy()
    {
        _playerArmyController = _playerController.GetComponent<PlayerArmyController>();

        ArmyModel armyModel = new(_playerArmyController.MaxSlots);
        _playerArmyController.Initialize(armyModel);

        foreach (var selection in _gameSession.SelectedAllySquads)
        {
            if (selection.Definition == null || selection.Count <= 0)
            {
                continue;
            }

            _playerArmyController.Army.TryAddSquad(selection.Definition, selection.Count);
        }
    }

    private void SubscribeToGameEvents()
    {
        _sceneEventBusService.Subscribe<RequestPlayerUpgrade>(HandleRequestPlayerUpgrade);
        _sceneEventBusService.Subscribe<SelectSquadUpgrade>(HandleSelectUpgrade);
    }

    private void UnsubscribeFromGameEvents()
    {
        _sceneEventBusService.Unsubscribe<RequestPlayerUpgrade>(HandleRequestPlayerUpgrade);
        _sceneEventBusService.Unsubscribe<SelectSquadUpgrade>(HandleSelectUpgrade);
    }

    // TODO: вынести подписку в UI контроллер апгрейдов
    private void HandleRequestPlayerUpgrade(RequestPlayerUpgrade evt) {
        var upgrades = _upgradeSystem.GenerateRandomUpgrades();
        _upgradesUIController.RenderUpgrades(upgrades);
        _upgradesUIController.Show();
    }

    private void HandleSelectUpgrade(SelectSquadUpgrade evt) {
        _upgradesUIController.Hide();
    }

    private void UpdateSquadsWithHero(IReadOnlyArmyModel army)
    {
        _squadsWithHero.Clear();
        _squadsWithHero.Add(_hero);

        IReadOnlyList<IReadOnlySquadModel> squads = army.GetSquads();
        if (squads != null)
        {
            for (int i = 0; i < squads.Count; i++)
            {
                var squad = squads[i];
                if (squad == null || squad.IsEmpty)
                    continue;

                _squadsWithHero.Add(squad);
            }
        }
    }
}
