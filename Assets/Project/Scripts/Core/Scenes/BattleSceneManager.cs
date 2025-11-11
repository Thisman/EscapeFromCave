using UnityEngine;
using VContainer;
using VContainer.Unity;
using System.Collections.Generic;

public class BattleSceneManager : MonoBehaviour
{
    [SerializeField] private GameObject _battleSquadPrefab;

    [Inject] readonly private SceneLoader _sceneLoader;
    [Inject] readonly private IObjectResolver _objectResolver;

    [Inject] readonly private BattleQueueUIController _queueUIController;
    [Inject] readonly private BattleQueueController _battleQueueController;

    [Inject] readonly private BattleTacticUIController _tacticUIController;
    [Inject] readonly private BattleCombatUIController _combatUIController;
    [Inject] readonly private BattleResultsUIController _resultsUIController;
    [Inject] readonly private SquadInfoUIController _squadInfoUIController;

    [Inject] readonly private BattleGridController _battleGridController;
    [Inject] readonly private BattleGridDragAndDropController _battleGridDragAndDropController;

    [Inject] readonly private InputService _inputService;
    [Inject] readonly private AudioManager _audioManager;

    private PanelManager _panelManager;
    private BattleSceneData _battleData;
    private BattleContext _battleContext;
    private BattleRoundsMachine _battleRoundMachine;
    private BattlePhaseMachine _battlePhaseMachine;
    private SquadInfoUIManager _squadInfoUIManager;

    private string _originSceneName;
    private const string BattleSceneName = "BattleScene";

    private void Start()
    {
        InitializePanelController();
        SubscribeToUiEvents();

        InitializeBattleData();
        InitializeBattleContext();
        InitializeBattleUnits();
        InitializeStateMachines();

        _ = _audioManager.PlayClipAsync("BackgroundMusic", "JumanjiDrums");
        _battlePhaseMachine.Fire(BattleTrigger.StartBattle);
    }

    private void OnDestroy()
    {
        _ = _audioManager.PlayClipAsync("BackgroundMusic", "TheHumOfCave");
        UnsubscribeFromUiEvents();
        _squadInfoUIManager?.Dispose();
    }

    private void InitializeBattleData()
    {
        if (!_sceneLoader.TryGetScenePayload(BattleSceneName, out BattleSceneData payload))
        {
            Debug.LogWarning($"[{nameof(BattleSceneManager)}.{nameof(InitializeBattleData)}] Battle scene payload was not found. Using empty battle setup.");
            return;
        }

        _battleData = payload;
        _originSceneName = ResolveOriginSceneName(payload);
    }

    private void InitializeBattleUnits()
    {
        List<BattleSquadController> collectedUnits = new();

        if (_battleData != null)
        {
            TryAddUnit(collectedUnits, _battleData.Hero);

            if (_battleData.Army != null)
            {
                foreach (var squad in _battleData.Army)
                {
                    TryAddUnit(collectedUnits, squad);
                }
            }

            if (_battleData.Enemies != null)
            {
                foreach (var squad in _battleData.Enemies)
                {
                    TryAddUnit(collectedUnits, squad);
                }
            }
        }
        else
        {
            Debug.LogWarning($"[{nameof(BattleSceneManager)}.{nameof(InitializeBattleUnits)}] Battle data was not resolved. No units will be spawned.");
        }

        _battleContext.BattleUnits = collectedUnits;
    }

    private void InitializeBattleContext()
    {
        BattleEffectsManager battleEffectsManager = new();
        BattleAbilityManager battleAbilitiesManager = new();
        AIBattleActionController enemyTurnController = new();
        PlayerBattleActionController playerTurnController = new();
        BattleActionControllerResolver actionControllerResolver = new(playerTurnController, enemyTurnController);

        _battleContext = new BattleContext
        {
            InputService = _inputService,
            PanelManager = _panelManager,
            BattleQueueUIController = _queueUIController,
            BattleGridDragAndDropController = _battleGridDragAndDropController,

            BattleGridController = _battleGridController,
            BattleQueueController = _battleQueueController,

            BattleEffectsManager = battleEffectsManager,
            BattleAbilitiesManager = battleAbilitiesManager,
            BattleActionControllerResolver = actionControllerResolver,

            BattleTacticUIController = _tacticUIController,
            BattleCombatUIController = _combatUIController,
            BattleResultsUIController = _resultsUIController,
        };

        _squadInfoUIManager = new SquadInfoUIManager(_squadInfoUIController);
        _battleContext.SquadInfoUIManager = _squadInfoUIManager;
    }

    private void InitializeStateMachines()
    {
        _battleRoundMachine = new BattleRoundsMachine(_battleContext);
        _battlePhaseMachine = new BattlePhaseMachine(_battleContext, _battleRoundMachine);
    }

    private void InitializePanelController()
    {
        _panelManager = new PanelManager(
            ("tactic", new[] { _tacticUIController.gameObject }),
            ("rounds", new[] {
                _combatUIController.gameObject,
                _queueUIController.gameObject
            }),
            ("results", new[] { _resultsUIController.gameObject })
        );
    }

    private void SubscribeToUiEvents()
    {
        if (_resultsUIController != null)
        {
            _resultsUIController.OnExitBattle += ExitBattle;
        }
    }

    private void UnsubscribeFromUiEvents()
    {
        if (_resultsUIController != null)
        {
            _resultsUIController.OnExitBattle -= ExitBattle;
        }
    }

    private void ExitBattle()
    {
        string returnScene = _originSceneName;
        object closeData = null;

        if (_battleContext.IsFinished)
        {
            closeData = _battleContext.BattleResult;
        }

        _ = _sceneLoader.UnloadAdditiveWithDataAsync(BattleSceneName, closeData, returnScene);
    }

    private void TryAddUnit(List<BattleSquadController> squads, BattleSquadSetup setup)
    {
        if (!setup.IsValid)
            return;

        GameObject instance = _objectResolver.Instantiate(_battleSquadPrefab);
        BattleSquadController controller = instance.GetComponent<BattleSquadController>();
        SquadModel squadModel = new(setup.Definition, setup.Count);
        BattleSquadModel battleModel = new(squadModel);
        controller.Initialize(battleModel);

        squads.Add(controller);
    }

    private static string ResolveOriginSceneName(BattleSceneData data)
    {
        string heroScene = SceneUtils.TryGetSourceSceneName(data.HeroSource);
        if (!string.IsNullOrEmpty(heroScene))
            return heroScene;

        return null;
    }
}
