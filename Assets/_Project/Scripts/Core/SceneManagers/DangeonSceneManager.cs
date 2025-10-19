using UnityEngine;
using VContainer;
using VContainer.Unity;

public class DangeonSceneManager : MonoBehaviour
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private Transform _playerSpawnPoint;

    [Inject] private readonly InputRouter _inputRouter;
    [Inject] private readonly GameSession _gameSession;
    [Inject] private readonly IObjectResolver _objectResolver;

    public void Start()
    {
        _inputRouter.EnterGameplay();
        InitializePlayer();
    }

    private void InitializePlayer()
    {
        if (_playerPrefab == null)
        {
            Debug.LogError("[DangeonSceneManager] Player prefab is not assigned. Unable to spawn player.");
            return;
        }

        if (_gameSession.HeroDefinition == null)
        {
            Debug.LogWarning("[DangeonSceneManager] GameSession does not contain a hero definition. Player will not be spawned.");
            return;
        }

        if (_objectResolver == null)
        {
            Debug.LogError("[DangeonSceneManager] Object resolver is not available. Unable to instantiate player with dependencies.");
            return;
        }

        Vector3 spawnPosition = _playerSpawnPoint != null ? _playerSpawnPoint.position : transform.position;
        Quaternion spawnRotation = _playerSpawnPoint != null ? _playerSpawnPoint.rotation : transform.rotation;

        var playerObject = _objectResolver.Instantiate(_playerPrefab, spawnPosition, spawnRotation);
        var playerController = playerObject.GetComponent<PlayerController>();

        if (playerController == null)
        {
            Debug.LogError("[DangeonSceneManager] Instantiated player prefab does not contain a PlayerController component.");
            return;
        }

        var unitModel = new UnitModel(_gameSession.HeroDefinition);
        playerController.Initialize(unitModel);

        InitializeArmy(playerController);
    }

    private void InitializeArmy(PlayerController playerController)
    {
        var armyController = playerController.GetComponent<PlayerArmyController>();
        if (armyController == null)
        {
            Debug.LogWarning("[DangeonSceneManager] Player prefab is missing PlayerArmyController. Army initialization skipped.");
            return;
        }

        var armyModel = new ArmyModel(armyController.MaxSlots);
        armyController.Initialize(armyModel);

        foreach (var definition in _gameSession.ArmyDefinition)
        {
            if (definition != null)
            {
                const int defaultAmount = 10;
                armyController.TryAddUnits(definition, defaultAmount);
            }
        }
    }
}
