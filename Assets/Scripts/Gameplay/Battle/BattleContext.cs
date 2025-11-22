using System;
using System.Collections.Generic;

public sealed class BattleContext : IDisposable
{
    private bool _disposed;
    private readonly List<BattleSquadController> _battleUnits = new();
    private readonly Dictionary<IReadOnlySquadModel, BattleSquadController> _controllersByModel = new();

    public BattleContext(
        InputService inputService,
        BattleSceneUIController battleSceneUIController,
        BattleGridController battleGridController,
        BattleQueueController battleQueueController,
        BattleGridDragAndDropController battleGridDragAndDropController,

        BattleAbilitiesManager battleAbilityManager,
        BattleEffectsManager battleEffectsManager,
        GameEventBusService sceneEventBusService
    )
    {
        DefendedUnitsThisRound = new HashSet<IReadOnlySquadModel>();

        InputService = inputService;
        BattleEffectsManager = battleEffectsManager;
        SceneEventBusService = sceneEventBusService;
        BattleAbilitiesManager = battleAbilityManager;
        BattleQueueController = battleQueueController;

        BattleGridController = battleGridController;
        BattleSceneUIController = battleSceneUIController;
        BattleGridDragAndDropController = battleGridDragAndDropController;
    }

    public InputService InputService { get; }

    public GameEventBusService SceneEventBusService { get; }

    public BattleEffectsManager BattleEffectsManager { get; }

    public BattleGridController BattleGridController { get; }

    public BattleQueueController BattleQueueController { get; }

    public BattleAbilitiesManager BattleAbilitiesManager { get; }

    public BattleSceneUIController BattleSceneUIController { get; }
    
    public BattleGridDragAndDropController BattleGridDragAndDropController { get; }

    public bool IsFinished { get; set; }
    
    public IBattleAction CurrentAction { get; set; }

    public IReadOnlySquadModel ActiveUnit { get; set; }
    
    public ISet<IReadOnlySquadModel> DefendedUnitsThisRound { get; }

    public IReadOnlyList<BattleSquadController> BattleUnits => _battleUnits;

    public void RegisterSquads(IEnumerable<BattleSquadController> squads)
    {
        _battleUnits.Clear();
        _controllersByModel.Clear();

        if (squads == null)
        {
            return;
        }

        foreach (var squad in squads)
        {
            if (squad == null)
                continue;

            var model = squad.GetSquadModel();
            if (model == null)
                continue;

            _battleUnits.Add(squad);
            _controllersByModel[model] = squad;
        }
    }

    public bool TryGetSquadController(IReadOnlySquadModel model, out BattleSquadController controller)
    {
        if (model == null)
        {
            controller = null;
            return false;
        }

        if (_controllersByModel.TryGetValue(model, out controller) && controller != null)
            return true;

        controller = null;
        return false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (CurrentAction is IDisposable disposableAction)
        {
            disposableAction.Dispose();
        }

        ActiveUnit = null;
        CurrentAction = null;
        DefendedUnitsThisRound.Clear();

        _battleUnits.Clear();
        _controllersByModel.Clear();
    }
}
