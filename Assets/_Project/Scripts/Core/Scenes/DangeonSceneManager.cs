using UnityEngine;
using UnityEngine.Rendering.Universal;
using VContainer;
using VContainer.Unity;

public class DangeonSceneManager : MonoBehaviour
{
    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private Transform _playerSpawnPoint;
    [SerializeField] private ArmyRoasterView _armyRoasterView;

    [SerializeField] private Light2D _globalLight2D;

    [Inject] private readonly InputRouter _inputRouter;
    [Inject] private readonly GameSession _gameSession;
    [Inject] private readonly IObjectResolver _objectResolver;

    private void Start()
    {
        _globalLight2D.intensity = 0f;
        _inputRouter.EnterGameplay();
        PlayerController playerController = InitializePlayer();
        PlayerArmyController playerArmyController = InitializeArmy(playerController);

        _armyRoasterView.Render(playerArmyController);
    }

    private PlayerController InitializePlayer()
    {
        Vector3 spawnPosition = _playerSpawnPoint != null ? _playerSpawnPoint.position : transform.position;
        Quaternion spawnRotation = _playerSpawnPoint != null ? _playerSpawnPoint.rotation : transform.rotation;

        GameObject playerInstance = _objectResolver.Instantiate(_playerPrefab, spawnPosition, spawnRotation);
        PlayerController playerController = playerInstance.GetComponent<PlayerController>();
        SquadModel squadModel = new(_gameSession.HeroDefinition, 1);

        playerController.Initialize(squadModel);

        return playerController;
    }

    private PlayerArmyController InitializeArmy(PlayerController playerController)
    {
        PlayerArmyController armyController = playerController.GetComponent<PlayerArmyController>();
        ArmyModel armyModel = new(armyController.MaxSlots);

        armyController.Initialize(armyModel);

        foreach (var definition in _gameSession.ArmyDefinition)
        {
            if (definition != null)
            {
                const int defaultAmount = 10;
                armyController.Army.TryAddSquad(definition, defaultAmount);
            }
        }

        return armyController;
    }
}
