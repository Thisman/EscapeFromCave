using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using VContainer;

public class BattleSceneManager : MonoBehaviour
{
    [SerializeField] private BattleCombatUIController _combatUIController;
    [SerializeField] private BattleResultsUIController _resultsUIController;
    [SerializeField] private BattleTacticUIController _tacticUIController;

    private BattleContext _ctx;
    private ActionPipelineMachine _actionPipeline;
    private CombatLoopMachine _combatLoop;
    private BattlePhaseMachine _phaseMachine;
    private PanelController _panelController;

    private readonly List<BattleSquadModel> _debugBattleSquads = new();
    private readonly List<GameObject> _debugBattleUnits = new();
    private Sprite[] _cachedDebugRogueSprites;

    [SerializeField] private BattleGridController _battleGridController;
    [SerializeField] private BattleGridDragAndDropController _battleGridDragAndDropController;
    [SerializeField] private GameObject _battleUnitPrefab;

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
            BattleGridController = _battleGridController,
            BattleGridDragAndDropController = _battleGridDragAndDropController
        };
        _actionPipeline = new ActionPipelineMachine(_ctx);
        _combatLoop = new CombatLoopMachine(_ctx, _actionPipeline);
        _phaseMachine = new BattlePhaseMachine(_ctx, _combatLoop);

        _phaseMachine.Fire(BattleTrigger.Start);
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Create Test Battle Squads")]
#endif
    private void DebugCreateTestBattleSquads()
    {
        ClearDebugBattleUnits();
        _debugBattleSquads.Clear();

        var playerUnit = new UnitModel(CreateDebugDefinition("Debug Hero", UnitType.Hero, 1, 120, 20, 10, 12, 3.5f, GetDebugRogueSprite(0)), 1);
        var allySquadOne = new SquadModel(CreateDebugDefinition("Debug Ally 1", UnitType.Ally, 1, 80, 12, 6, 10, 3f, GetDebugRogueSprite(1)), 5);
        var allySquadTwo = new SquadModel(CreateDebugDefinition("Debug Ally 2", UnitType.Ally, 1, 70, 14, 4, 11, 3.2f, GetDebugRogueSprite(2)), 4);
        var enemyUnit = new UnitModel(CreateDebugDefinition("Debug Enemy", UnitType.Enemy, 1, 150, 25, 12, 9, 2.8f, GetDebugRogueSprite(10)), 1);

        _debugBattleSquads.Add(new BattleSquadModel(playerUnit));
        _debugBattleSquads.Add(new BattleSquadModel(allySquadOne));
        _debugBattleSquads.Add(new BattleSquadModel(allySquadTwo));
        _debugBattleSquads.Add(new BattleSquadModel(enemyUnit));

        var placements = CreateDebugUnitPlacements();

        Debug.Log("Debug battle squads created.");

        if (_battleGridController != null)
        {
            _battleGridController.PopulateWithSquads(placements);
        }
        else
        {
            Debug.LogWarning("BattleSceneManager: Cannot populate battle grid for debug squads. Ensure BattleGridController is assigned.");
        }
    }

    private List<(IReadOnlyBattleSquadModel Squad, Transform Instance)> CreateDebugUnitPlacements()
    {
        var placements = new List<(IReadOnlyBattleSquadModel, Transform)>();

        foreach (var battleSquad in _debugBattleSquads)
        {
            if (battleSquad?.Squad == null)
                continue;

            var instance = CreateDebugUnitInstance(battleSquad);
            if (instance == null)
                continue;

            placements.Add((battleSquad, instance.transform));
        }

        return placements;
    }

    private GameObject CreateDebugUnitInstance(BattleSquadModel battleSquad)
    {
        GameObject instance;
        if (_battleUnitPrefab != null)
        {
            instance = Instantiate(_battleUnitPrefab);
        }
        else
        {
            instance = new GameObject("DebugBattleUnit");
        }

        if (instance == null)
            return null;

        var unitName = battleSquad.Squad?.UnitDefinition != null ? battleSquad.Squad.UnitDefinition.UnitName : "Unknown";
        instance.name = $"Debug_{unitName}";

        TryApplyDraggableTag(instance);
        InitializeBattleSquadController(instance, battleSquad);

        _debugBattleUnits.Add(instance);

        return instance;
    }

    private void InitializeBattleSquadController(GameObject instance, BattleSquadModel battleSquad)
    {
        if (instance == null)
            return;

        var squadController = instance.GetComponentInChildren<BattleSquadController>(true);
        if (squadController == null)
        {
            Debug.LogWarning($"BattleSceneManager: BattleSquadController is missing on debug unit '{instance.name}'.");
            return;
        }

        squadController.Initialize(battleSquad);
    }

    private void TryApplyDraggableTag(GameObject instance)
    {
        if (instance == null)
            return;

        try
        {
            instance.tag = "Draggable";
        }
        catch (UnityException exception)
        {
            Debug.LogWarning($"BattleSceneManager: Failed to set Draggable tag on debug unit '{instance.name}'. {exception.Message}");
        }
    }

    private void ClearDebugBattleUnits()
    {
        for (int i = _debugBattleUnits.Count - 1; i >= 0; i--)
        {
            var unit = _debugBattleUnits[i];
            if (unit == null)
            {
                _debugBattleUnits.RemoveAt(i);
                continue;
            }

            var unitTransform = unit.transform;
            if (_battleGridController != null)
            {
                _battleGridController.TryRemoveOccupant(unitTransform, out _);
            }

            if (Application.isPlaying)
                Destroy(unit);
            else
                DestroyImmediate(unit);

            _debugBattleUnits.RemoveAt(i);
        }
    }

    private static UnitDefinitionSO CreateDebugDefinition(
        string unitName,
        UnitType type,
        int levelIndex,
        int health,
        int damage,
        int defense,
        int initiative,
        float speed,
        Sprite icon)
    {
        var definition = ScriptableObject.CreateInstance<UnitDefinitionSO>();
        definition.UnitName = unitName;
        definition.Type = type;
        definition.Icon = icon;
        definition.Levels = new List<UnitLevelDefintion>
        {
            new()
            {
                LevelIndex = levelIndex,
                XPToNext = 100,
                Health = health,
                Damage = damage,
                Defense = defense,
                Initiative = initiative,
                Speed = speed
            }
        };

        return definition;
    }

    private Sprite GetDebugRogueSprite(int requestedIndex)
    {
        var sprites = LoadDebugRogueSprites();
        if (sprites == null || sprites.Length == 0)
        {
            return null;
        }

        if (requestedIndex < 0)
        {
            requestedIndex = 0;
        }

        if (requestedIndex >= sprites.Length)
        {
            Debug.LogWarning($"BattleSceneManager: Requested debug sprite index {requestedIndex} but only {sprites.Length} sprites are available.");
            requestedIndex = sprites.Length - 1;
        }

        return sprites[requestedIndex];
    }

    private Sprite[] LoadDebugRogueSprites()
    {
        if (_cachedDebugRogueSprites != null)
        {
            return _cachedDebugRogueSprites;
        }

#if UNITY_EDITOR
        const string rogueSpritePath = "Assets/_Project/Art/Packs/32rogues/rogues.png";
        var loadedAssets = AssetDatabase.LoadAllAssetsAtPath(rogueSpritePath);
        var sprites = new List<Sprite>(loadedAssets.Length);
        foreach (var asset in loadedAssets)
        {
            if (asset is Sprite sprite)
            {
                sprites.Add(sprite);
            }
        }

        if (sprites.Count == 0)
        {
            Debug.LogWarning($"BattleSceneManager: Unable to find any sprites in '{rogueSpritePath}'.");
        }

        sprites.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
        _cachedDebugRogueSprites = sprites.ToArray();
#else
        _cachedDebugRogueSprites = Array.Empty<Sprite>();
#endif

        return _cachedDebugRogueSprites;
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
            ("combat", new[] { _combatUIController?.gameObject }),
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

    private void HandleExitBattle()
    {
    }
}
