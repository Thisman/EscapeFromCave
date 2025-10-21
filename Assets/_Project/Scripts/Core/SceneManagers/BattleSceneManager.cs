using UnityEngine;
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

    private void Awake()
    {
        SubscribeToUiEvents();
        InitializePanelController();
    }

    private async void Start()
    {
        _ctx = new BattleContext();
        _actionPipeline = new ActionPipelineMachine(_ctx);
        _combatLoop = new CombatLoopMachine(_ctx, _actionPipeline);
        _phaseMachine = new BattlePhaseMachine(_ctx, _combatLoop);

        _panelController?.Show("tactic");
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
        _panelController?.Show("combat");
        _phaseMachine?.Fire(BattleTrigger.EndTactics);
    }

    private void HandleLeaveCombat()
    {
        _panelController?.Show("results");
        _phaseMachine?.Fire(BattleTrigger.EndCombat);
    }

    private void HandleExitBattle()
    {
        _panelController?.Show("results");
        _phaseMachine?.Fire(BattleTrigger.ForceResults);
    }
}
