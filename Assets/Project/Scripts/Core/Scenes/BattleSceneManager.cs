using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

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

    [Inject] readonly private BattleGridController _battleGridController;
    [Inject] readonly private BattleGridDragAndDropController _battleGridDragAndDropController;

    [Inject] readonly private InputService _inputService;
    [Inject] readonly private AudioManager _audioManager;

    private PanelManager _panelManager;
    private BattleSceneData _battleData;
    private BattleContext _battleContext;
    private BattleRoundsMachine _battleRoundMachine;
    private BattlePhaseMachine _battlePhaseMachine;

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
    }

    private void InitializeBattleData()
    {
        if (!_sceneLoader.TryGetScenePayload(BattleSceneName, out BattleSceneData payload))
        {
            Debug.LogWarning("[BattleSceneManager] Battle scene payload was not found. Using empty battle setup.");
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

            TryAddUnit(collectedUnits, _battleData.Enemy);
        }
        else
        {
            Debug.LogWarning("[BattleSceneManager] Battle data was not resolved. No units will be spawned.");
        }

        _battleContext.BattleUnits = collectedUnits;
    }

    private void InitializeBattleContext()
    {
        AIBattleActionController enemyTurnController = new();
        PlayerBattleActionController playerTurnController = new();
        BattleActionControllerResolver actionControllerResolver = new(playerTurnController, enemyTurnController);
        BattleEffectsManager battleEffectsManager = new();
        BattleAbilityManager battleAbilityManager = new();

        _battleContext = new BattleContext
        {
            PanelManager = _panelManager,
            BattleQueueUIController = _queueUIController,
            BattleGridDragAndDropController = _battleGridDragAndDropController,

            BattleGridController = _battleGridController,
            BattleQueueController = _battleQueueController,

            BattleActionControllerResolver = actionControllerResolver,
            BattleAbilityManager = battleAbilityManager,
            BattleEffectsManager = battleEffectsManager,

            BattleTacticUIController = _tacticUIController,
            BattleCombatUIController = _combatUIController,
            BattleResultsUIController = _resultsUIController,
            InputService = _inputService,
        };
    }

    private void InitializeStateMachines()
    {
        _battleRoundMachine = new BattleRoundsMachine(_battleContext);
        _battlePhaseMachine = new BattlePhaseMachine(_battleContext, _battleRoundMachine);
    }

    private void InitializePanelController()
    {
        if (_tacticUIController == null && _combatUIController == null && _resultsUIController == null)
        {
            return;
        }

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

    private void TryAddUnit(List<BattleSquadController> buffer, BattleSquadSetup setup)
    {
        if (!setup.IsValid)
            return;

        GameObject instance = _objectResolver.Instantiate(_battleSquadPrefab);
        BattleSquadController controller = instance.GetComponent<BattleSquadController>();
        SquadModel squadModel = new(setup.Definition, setup.Count);
        BattleSquadModel battleModel = new(squadModel);
        controller.Initialize(battleModel);

        buffer.Add(controller);
    }

    private static string ResolveOriginSceneName(BattleSceneData data)
    {
        if (data == null)
            return null;

        var heroScene = TryGetSourceSceneName(data.HeroSource);
        if (!string.IsNullOrEmpty(heroScene))
            return heroScene;

        var enemyScene = TryGetSourceSceneName(data.EnemySource);
        if (!string.IsNullOrEmpty(enemyScene))
            return enemyScene;

        return null;
    }

    private static string TryGetSourceSceneName(GameObject source)
    {
        if (source == null)
            return null;

        var scene = source.scene;
        return scene.IsValid() ? scene.name : null;
    }
}
