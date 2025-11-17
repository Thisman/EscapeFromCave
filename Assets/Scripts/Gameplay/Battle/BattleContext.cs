using System;
using System.Collections.Generic;

public sealed class BattleContext : IDisposable
{
    private readonly List<BattleSquadController> _battleUnits = new();
    private readonly Dictionary<IReadOnlySquadModel, BattleSquadController> _controllersByModel = new();
    private bool _disposed;

    public BattleContext(
        BattleGridController battleGridController,
        BattleQueueController battleQueueController,
        BattleActionControllerResolver battleActionControllerResolver,
        BattleUIController battleUIController,
        BattleAbilityManager battleAbilityManager,
        BattleEffectsManager battleEffectsManager,
        InputService inputService,
        BattleGridDragAndDropController battleGridDragAndDropController = null)
    {
        BattleGridController = battleGridController ?? throw new ArgumentNullException(nameof(battleGridController));
        BattleQueueController = battleQueueController ?? throw new ArgumentNullException(nameof(battleQueueController));
        BattleActionControllerResolver = battleActionControllerResolver ?? throw new ArgumentNullException(nameof(battleActionControllerResolver));
        BattleUIController = battleUIController ?? throw new ArgumentNullException(nameof(battleUIController));
        BattleAbilitiesManager = battleAbilityManager ?? throw new ArgumentNullException(nameof(battleAbilityManager));
        BattleEffectsManager = battleEffectsManager ?? throw new ArgumentNullException(nameof(battleEffectsManager));
        InputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        BattleGridDragAndDropController = battleGridDragAndDropController;
        DefendedUnitsThisRound = new HashSet<IReadOnlySquadModel>();
    }

    public bool IsFinished { get; set; }

    public BattleGridController BattleGridController { get; }

    public BattleGridDragAndDropController BattleGridDragAndDropController { get; }

    public BattleQueueController BattleQueueController { get; }

    public BattleActionControllerResolver BattleActionControllerResolver { get; }

    public BattleUIController BattleUIController { get; }

    public IReadOnlyList<BattleSquadController> BattleUnits => _battleUnits;

    public IReadOnlySquadModel ActiveUnit { get; set; }

    public BattleAbilityManager BattleAbilitiesManager { get; }

    public BattleEffectsManager BattleEffectsManager { get; }

    public IBattleAction CurrentAction { get; set; }

    public BattleResult BattleResult { get; set; }

    public InputService InputService { get; }

    public ISet<IReadOnlySquadModel> DefendedUnitsThisRound { get; }

    public void RegisterSquads(IEnumerable<BattleSquadController> squads)
    {
        _battleUnits.Clear();
        _controllersByModel.Clear();

        if (squads == null)
            return;

        foreach (var squad in squads)
        {
            if (squad == null)
                continue;

            _battleUnits.Add(squad);

            var model = squad.GetSquadModel();
            if (model == null)
                continue;

            _controllersByModel[model] = squad;
        }
    }

    public bool TryGetController(IReadOnlySquadModel model, out BattleSquadController controller)
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

        CurrentAction = null;
        ActiveUnit = null;
        DefendedUnitsThisRound.Clear();
        _battleUnits.Clear();
        _controllersByModel.Clear();
    }
}
