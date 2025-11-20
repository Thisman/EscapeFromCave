using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class DungeonSceneManager : MonoBehaviour
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private Transform _playerSpawnPoint;

    [SerializeField] private UpgradesUIController _upgradesUIController;
    [SerializeField] private DungeonSceneUIController _dungeonUIController;

    [Inject] private readonly GameSession _gameSession;
    [Inject] private readonly AudioManager _audioManager;
    [Inject] private readonly InputService _inputService;
    [Inject] private readonly IObjectResolver _objectResolver;

    private PlayerController _playerController;
    private PlayerArmyController _playerArmyController;

    private IReadOnlySquadModel _heroSquad;
    private readonly List<IReadOnlySquadModel> _squadsWithHero = new();

    private void Awake()
    {
        InitializePlayer();
        InitializeArmy();
    }

    private void OnEnable()
    {
        SubscribeToGameEvents();
    }

    private void Start()
    {
        _ = _audioManager.PlayClipAsync("BackgroundEffect", "DropsInTheCave");
        _inputService.EnterGameplay();

        UpdateSquadsWithHero(_playerArmyController.Army);
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
        _heroSquad = _playerController.GetPlayer();
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
        _playerArmyController.ArmyChanged += HandleArmyChanged;
        _upgradesUIController.OnSelectUpgrade += HandleSelectUpgrade;
    }

    private void UnsubscribeFromGameEvents()
    {
        _playerArmyController.ArmyChanged -= HandleArmyChanged;
        _upgradesUIController.OnSelectUpgrade -= HandleSelectUpgrade;
    }

    private void HandleArmyChanged(IReadOnlyArmyModel army)
    {
        UpdateSquadsWithHero(army);

        _dungeonUIController.RenderSquads(_squadsWithHero);
    }

    private void HandleSelectUpgrade() {
        _upgradesUIController.Hide();
    }

    private void UpdateSquadsWithHero(IReadOnlyArmyModel army)
    {
        _squadsWithHero.Clear();
        _squadsWithHero.Add(_heroSquad);

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
