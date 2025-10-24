using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class BattleSceneManager : MonoBehaviour
{
    [SerializeField] private BattleCombatUIController _combatUIController;
    [SerializeField] private BattleResultsUIController _resultsUIController;
    [SerializeField] private BattleTacticUIController _tacticUIController;
    [SerializeField] private BattleQueueUIController _queueUIController;

    [SerializeField] private GameObject[] _battleUnitPrefabs = Array.Empty<GameObject>();

    [Inject] private BattleGridController _battleGridController;
    [Inject] private BattleQueueController _battleQueueController;
    [Inject] BattleGridDragAndDropController _battleGridDragAndDropController;

    private BattleContext _ctx;
    private CombatLoopMachine _combatLoop;
    private BattlePhaseMachine _phaseMachine;
    private PanelController _panelController;

    private BattleUnitController[] _spawnedBattleUnits = Array.Empty<BattleUnitController>();

    private void Awake()
    {
        SubscribeToUiEvents();
        InitializePanelController();
    }

    private void Start()
    {
        _ctx = new BattleContext
        {
            PanelController = _panelController,
            BattleQueueUIController = _queueUIController,
            BattleGridDragAndDropController = _battleGridDragAndDropController,

            BattleGridController = _battleGridController,
            BattleQueueController = _battleQueueController,
        };

        InitializeBattleUnits();

        _combatLoop = new CombatLoopMachine(_ctx);
        _phaseMachine = new BattlePhaseMachine(_ctx, _combatLoop);

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

        _panelController = new PanelController(
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
        if (_combatLoop == null)
            return;

        _combatLoop.DefendActiveUnit();
    }

    private void HandleSkipTurn()
    {
        if (_combatLoop == null)
            return;

        _combatLoop.SkipTurn();
    }

    private void HandleExitBattle()
    {
    }

    private void InitializeBattleUnits()
    {
        if (_ctx == null)
        {
            return;
        }

        var collectedUnits = new List<BattleUnitController>();

        if (_battleUnitPrefabs != null)
        {
            foreach (var prefab in _battleUnitPrefabs)
            {
                if (prefab == null)
                    continue;

                var instance = Instantiate(prefab, transform);
                if (instance == null)
                    continue;

                var battleController = instance.GetComponent<BattleUnitController>();
                if (battleController == null)
                {
                    Debug.LogWarning($"Battle unit prefab '{prefab.name}' is missing a BattleUnitController.");
                    Destroy(instance);
                    continue;
                }

                EnsureBattleModelInitialized(battleController, instance);

                if (battleController.GetSquadModel() == null)
                {
                    Debug.LogWarning($"Battle unit instance '{instance.name}' failed to initialize its battle model.");
                    Destroy(instance);
                    continue;
                }

                collectedUnits.Add(battleController);
            }
        }

        _spawnedBattleUnits = collectedUnits.ToArray();
        _ctx.BattleUnits = _spawnedBattleUnits;
    }

    private static void EnsureBattleModelInitialized(BattleUnitController battleController, GameObject instance)
    {
        if (battleController == null || instance == null)
            return;

        if (battleController.GetSquadModel() != null)
            return;

        if (!instance.TryGetComponent(out SquadController unitController))
            return;

        if (unitController.GetSquadModel() is SquadModel squadModel)
            battleController.Initialize(squadModel);
    }
}
