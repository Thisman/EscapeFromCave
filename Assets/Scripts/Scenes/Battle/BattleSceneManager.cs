using UnityEngine;
using VContainer;
using VContainer.Unity;
using System.Collections.Generic;

public class BattleSceneManager : MonoBehaviour
{
    [SerializeField] private GameObject _battleSquadPrefab;
    [SerializeField] private BattleGridController _battleGridController;
    [SerializeField] private BattleGridDragAndDropController _battleGridDragAndDropController;
    [SerializeField] private BattleSceneUIController _battleSceneUIController;

    [Inject] readonly private SceneLoader _sceneLoader;
    [Inject] readonly private InputService _inputService;
    [Inject] readonly private AudioManager _audioManager;
    [Inject] readonly private IObjectResolver _objectResolver;
    [Inject] readonly private GameEventBusService _sceneEventBusService;

    private BattleSceneData _battleData;
    private BattleContext _battleContext;
    private BattlePhasesMachine _battlePhaseMachine;
    private BattleRoundsMachine _battleRoundMachine;

    private string _originSceneName;
    private const string BattleSceneName = "BattleScene";

    private async void Start()
    {
        SubscribeToGameEvents();
        InitializeBattleData();
        InitializeBattleContext();
        InitializeBattleUnits();
        InitializeStateMachines();
        InitializeUI();

        _battlePhaseMachine.Fire(BattlePhasesTrigger.StartBattle);
        await _audioManager.PlayClipAsync("BackgroundMusic", "JumanjiDrums");
    }

    private async void OnDestroy()
    {
        UnsubscribeFromGameEvents();
        _battleContext.Dispose();
        await _audioManager.PlayClipAsync("BackgroundMusic", "TheHumOfCave");
    }

    private void InitializeUI()
    {
        _battleSceneUIController.Initialize(_sceneEventBusService);
    }

    private void InitializeBattleData()
    {
        _sceneLoader.TryGetScenePayload(BattleSceneName, out BattleSceneData payload);

        Debug.Assert(payload != null, "Battle payload is null!");
        _battleData = payload;
        _originSceneName = ResolveOriginSceneName(payload);
    }

    private void InitializeBattleUnits()
    {
        List<BattleSquadController> collectedUnits = new();

        TryAddUnit(collectedUnits, _battleData.Hero);

        foreach (var squad in _battleData.Army)
        {
            TryAddUnit(collectedUnits, squad);
        }

        foreach (var squad in _battleData.Enemies)
        {
            TryAddUnit(collectedUnits, squad);
        }

        _battleContext.RegisterSquads(collectedUnits);
    }

    private void InitializeBattleContext()
    {
        BattleEffectsManager battleEffectsManager = new();
        BattleQueueController battleQueueController = new();
        BattleAbilitiesManager battleAbilitiesManager = new();

        _battleContext = new BattleContext(
            _inputService,
            _battleSceneUIController,
            _battleGridController,
            battleQueueController,
            _battleGridDragAndDropController,

            battleAbilitiesManager,
            battleEffectsManager,
            _sceneEventBusService
        );
    }

    private void InitializeStateMachines()
    {
        _battleRoundMachine = new BattleRoundsMachine(_battleContext);
        _battlePhaseMachine = new BattlePhasesMachine(_battleContext, _battleRoundMachine);
    }

    private void SubscribeToGameEvents()
    {
        _sceneEventBusService.Subscribe<RequestReturnToDungeon>(HandleReturnToDungeon);
    }

    private void UnsubscribeFromGameEvents()
    {
        _sceneEventBusService.Unsubscribe<RequestReturnToDungeon>(HandleReturnToDungeon);
    }

    private async void HandleReturnToDungeon(RequestReturnToDungeon evt)
    {
        string returnScene = _originSceneName;
        object closeData = null;

        if (_battleContext.IsFinished)
        {
            closeData = _battlePhaseMachine?.BattleResult;
        }

        await _sceneLoader.UnloadAdditiveWithDataAsync(BattleSceneName, closeData, returnScene);
    }

    private void TryAddUnit(List<BattleSquadController> squads, BattleSquadSetup setup)
    {
        if (!setup.IsValid)
            return;

        GameObject instance = _objectResolver.Instantiate(_battleSquadPrefab);
        BattleSquadController controller = instance.GetComponent<BattleSquadController>();
        SquadModel squadModel = new(setup.Definition, setup.Count, setup.Experience);
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
