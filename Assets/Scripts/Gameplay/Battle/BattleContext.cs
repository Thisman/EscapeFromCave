using System;
using System.Collections.Generic;

public sealed class BattleContext : IDisposable
{
    private bool _disposed;
    private readonly List<BattleSquadController> _battleUnits = new();
    private readonly Dictionary<IReadOnlySquadModel, BattleSquadController> _controllersByModel = new();

    public BattleContext(
        InputService inputService,
        BattleUIController battleUIController,
        BattleGridController battleGridController,
        BattleQueueController battleQueueController,
        BattleGridDragAndDropController battleGridDragAndDropController,

        BattleAbilityManager battleAbilityManager,
        BattleEffectsManager battleEffectsManager
    )
    {
        DefendedUnitsThisRound = new HashSet<IReadOnlySquadModel>();

        InputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        BattleEffectsManager = battleEffectsManager ?? throw new ArgumentNullException(nameof(battleEffectsManager));
        BattleAbilitiesManager = battleAbilityManager ?? throw new ArgumentNullException(nameof(battleAbilityManager));
        BattleQueueController = battleQueueController ?? throw new ArgumentNullException(nameof(battleQueueController));

        BattleUIController = battleUIController != null ? battleUIController : throw new ArgumentNullException(nameof(battleUIController));
        BattleGridController = battleGridController != null ? battleGridController : throw new ArgumentNullException(nameof(battleGridController));
        BattleGridDragAndDropController = battleGridDragAndDropController != null ? battleGridDragAndDropController : throw new ArgumentNullException(nameof(battleGridDragAndDropController));
    }

    public InputService InputService { get; }

    public BattleUIController BattleUIController { get; }

    public BattleEffectsManager BattleEffectsManager { get; }

    public BattleGridController BattleGridController { get; }

    public BattleQueueController BattleQueueController { get; }

    public BattleAbilityManager BattleAbilitiesManager { get; }

    public BattleGridDragAndDropController BattleGridDragAndDropController { get; }

    public bool IsFinished { get; set; }

    public BattleResult BattleResult { get; set; }
    
    public IBattleAction CurrentAction { get; set; }

    public IReadOnlySquadModel ActiveUnit { get; set; }
    
    public ISet<IReadOnlySquadModel> DefendedUnitsThisRound { get; }

    public IReadOnlyList<BattleSquadController> BattleUnits => _battleUnits;


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

        CurrentAction = null;
        ActiveUnit = null;
        DefendedUnitsThisRound.Clear();
        _battleUnits.Clear();
        _controllersByModel.Clear();
    }
}
