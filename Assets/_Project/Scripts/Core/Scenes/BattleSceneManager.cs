using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class BattleSceneManager : MonoBehaviour
{
    [SerializeField] private BattleCombatUIController _combatUIController;
    [SerializeField] private BattleResultsUIController _resultsUIController;
    [SerializeField] private BattleTacticUIController _tacticUIController;
    [SerializeField] private BattleQueueUIController _queueUIController;

    [SerializeField] private GameObject _battleSquadPrefab;

    private const string BattleSceneName = "BattleScene";

    [Inject] private BattleGridController _battleGridController;
    [Inject] private BattleQueueController _battleQueueController;
    [Inject] BattleGridDragAndDropController _battleGridDragAndDropController;
    [Inject] private SceneLoader _sceneLoader;
    [Inject] private IObjectResolver _objectResolver;

    private BattleContext _battleContext;
    private BattleRound _combatLoopMachine;
    private BattlePhaseMachine _phaseMachine;
    private PanelManager _panelManager;
    private BattleSceneData _battleData;
    private string _originSceneName;

    private void Awake()
    {
        SubscribeToUiEvents();
        InitializePanelController();
    }

    private void Start()
    {
        ResolveBattleData();

        _battleContext = new BattleContext
        {
            PanelManager = _panelManager,
            BattleQueueUIController = _queueUIController,
            BattleGridDragAndDropController = _battleGridDragAndDropController,

            BattleGridController = _battleGridController,
            BattleQueueController = _battleQueueController,
        };

        InitializeBattleUnits();

        _combatLoopMachine = new BattleRound(_battleContext);
        _phaseMachine = new BattlePhaseMachine(_battleContext, _combatLoopMachine);

        _phaseMachine.Fire(BattleTrigger.Start);
    }

    private void OnDestroy()
    {
        UnsubscribeFromUiEvents();
    }

    private void InitializePanelController()
    {
        if (_tacticUIController == null && _combatUIController == null && _resultsUIController == null)
        {
            return;
        }

        _panelManager = new PanelManager(
            ("tactic", new[] { _tacticUIController?.gameObject }),
            ("combat", new[] {
                _combatUIController?.gameObject,
                _queueUIController?.gameObject
            }),
            ("results", new[] { _resultsUIController?.gameObject })
        );
    }

    private void SubscribeToUiEvents()
    {
        if (_tacticUIController != null)
        {
            _tacticUIController.OnStartCombat += HandleStartCombat;
        }

        if (_combatUIController != null)
        {
            _combatUIController.OnLeaveCombat += HandleLeaveCombat;
            _combatUIController.OnDefend += HandleDefend;
            _combatUIController.OnSkipTurn += HandleSkipTurn;
        }

        if (_resultsUIController != null)
        {
            _resultsUIController.OnExitBattle += HandleExitBattle;
        }
    }

    private void UnsubscribeFromUiEvents()
    {
        if (_tacticUIController != null)
        {
            _tacticUIController.OnStartCombat -= HandleStartCombat;
        }

        if (_combatUIController != null)
        {
            _combatUIController.OnLeaveCombat -= HandleLeaveCombat;
            _combatUIController.OnDefend -= HandleDefend;
            _combatUIController.OnSkipTurn -= HandleSkipTurn;
        }

        if (_resultsUIController != null)
        {
            _resultsUIController.OnExitBattle -= HandleExitBattle;
        }
    }

    private void HandleStartCombat()
    {
        _phaseMachine?.Fire(BattleTrigger.EndTactics);
    }

    private void HandleLeaveCombat()
    {
        _phaseMachine?.Fire(BattleTrigger.EndCombat);
    }

    private void HandleDefend()
    {
        if (_combatLoopMachine == null)
            return;

        _combatLoopMachine.DefendActiveUnit();
    }

    private void HandleSkipTurn()
    {
        if (_combatLoopMachine == null)
            return;

        _combatLoopMachine.SkipTurn();
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

        if (_battleGridController != null && collectedUnits.Count > 0)
        {
            if (!_battleGridController.TryPlaceUnits(collectedUnits))
            {
                Debug.LogWarning("[BattleSceneManager] Failed to place battle units on the grid.");
            }
        }
    }

    private void ResolveBattleData()
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
