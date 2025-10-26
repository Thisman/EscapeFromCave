using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class BattleSceneManager : MonoBehaviour
{
    [SerializeField] private GameObject _battleSquadPrefab;

    [Inject] private SceneLoader _sceneLoader;
    [Inject] private IObjectResolver _objectResolver;

    [Inject] private BattleQueueUIController _queueUIController;
    [Inject] private BattleQueueController _battleQueueController;

    [Inject] private BattleTacticUIController _tacticUIController;
    [Inject] private BattleCombatUIController _combatUIController;
    [Inject] private BattleResultsUIController _resultsUIController;

    [Inject] private BattleGridController _battleGridController;
    [Inject] private BattleGridDragAndDropController _battleGridDragAndDropController;

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

        _battlePhaseMachine.Fire(BattleTrigger.StartBattle);
    }

    private void OnDestroy()
    {
        UnsubscribeFromUiEvents();
    }

    private void InitializeBattleData()
    {
        if (_sceneLoader == null)
        {
            Debug.LogWarning("[BattleSceneManager] SceneLoader was not injected. Unable to resolve battle payload.");
            return;
        }

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
        if (_battleContext == null)
        {
            return;
        }

        var collectedUnits = new List<BattleSquadController>();

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
        var enemyTurnController = new AIBattleActionController();
        var playerTurnController = new PlayerBattleActionController();
        var actionControllerResolver = new BattleActionControllerResolver(playerTurnController, enemyTurnController);

        _battleContext = new BattleContext
        {
            PanelManager = _panelManager,
            BattleQueueUIController = _queueUIController,
            BattleGridDragAndDropController = _battleGridDragAndDropController,

            BattleGridController = _battleGridController,
            BattleQueueController = _battleQueueController,

            BattleActionControllerResolver = actionControllerResolver,

            BattleTacticUIController = _tacticUIController,
            BattleCombatUIController = _combatUIController,
            BattleResultsUIController = _resultsUIController,
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
            ("tactic", new[] { _tacticUIController?.gameObject }),
            ("rounds", new[] {
                _combatUIController?.gameObject,
                _queueUIController?.gameObject
            }),
            ("results", new[] { _resultsUIController?.gameObject })
        );
    }

    private void SubscribeToUiEvents()
    {
        if (_resultsUIController != null)
        {
            _resultsUIController.OnExitBattle += HandleExitBattle;
        }
    }

    private void UnsubscribeFromUiEvents()
    {
        if (_resultsUIController != null)
        {
            _resultsUIController.OnExitBattle -= HandleExitBattle;
        }
    }

    private void HandleExitBattle()
    {
        if (_sceneLoader == null)
        {
            Debug.LogWarning("[BattleSceneManager] SceneLoader is not available. Unable to exit battle scene.");
            return;
        }

        _ = ExitBattleAsync();
    }

    private async Task ExitBattleAsync()
    {
        var returnScene = _originSceneName;

        try
        {
            await _sceneLoader.UnloadAdditiveWithDataAsync(BattleSceneName, null, returnScene);
        }
        catch (InvalidOperationException ex)
        {
            Debug.LogWarning($"[BattleSceneManager] Failed to unload battle scene with session data: {ex.Message}. Falling back to direct unload.");

            try
            {
                await _sceneLoader.UnloadAdditiveAsync(BattleSceneName, returnScene);
            }
            catch (Exception fallbackEx)
            {
                Debug.LogError($"[BattleSceneManager] Failed to unload battle scene via fallback: {fallbackEx}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleSceneManager] Failed to unload battle scene: {ex}");
        }
    }

    private void TryAddUnit(List<BattleSquadController> buffer, BattleSquadSetup setup)
    {
        if (buffer == null)
            return;

        if (!setup.IsValid)
            return;

        if (_battleSquadPrefab == null)
        {
            Debug.LogWarning("[BattleSceneManager] Battle squad prefab is not assigned. Cannot spawn units.");
            return;
        }

        GameObject instance = null;

        try
        {
            instance = _objectResolver != null
                ? _objectResolver.Instantiate(_battleSquadPrefab)
                : Instantiate(_battleSquadPrefab);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleSceneManager] Failed to instantiate battle squad prefab: {ex}");
            return;
        }

        if (instance == null)
            return;

        var controller = instance.GetComponent<BattleSquadController>();
        if (controller == null)
        {
            Debug.LogError("[BattleSceneManager] Spawned battle squad prefab does not contain a BattleSquadController component.");
            Destroy(instance);
            return;
        }

        try
        {
            var squadModel = new SquadModel(setup.Definition, setup.Count);
            var battleModel = new BattleSquadModel(squadModel);
            controller.Initialize(battleModel);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleSceneManager] Failed to initialize battle squad model: {ex}");
            Destroy(instance);
            return;
        }

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
