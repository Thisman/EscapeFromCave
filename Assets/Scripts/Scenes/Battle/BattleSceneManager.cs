using UnityEngine;
using VContainer;
using VContainer.Unity;
using System.Collections.Generic;

public class BattleSceneManager : MonoBehaviour
{
    [SerializeField] private GameObject _battleSquadPrefab;

    [Inject] readonly private SceneLoader _sceneLoader;
    [Inject] readonly private IObjectResolver _objectResolver;

    [Inject] readonly private BattleGridController _battleGridController;
    [Inject] readonly private BattleQueueController _battleQueueController;
    [Inject] readonly private BattleSceneUIController _battleSceneUIController;
    [Inject] readonly private BattleGridDragAndDropController _battleGridDragAndDropController;

    [Inject] readonly private InputService _inputService;
    [Inject] readonly private AudioManager _audioManager;

    private BattleSceneData _battleData;
    private BattleContext _battleContext;
    private BattlePhasesMachine _battlePhaseMachine;
    private BattleRoundsMachine _battleRoundMachine;

    private string _originSceneName;
    private const string BattleSceneName = "BattleScene";

    private async void Start()
    {
        SubscribeToUiEvents();
        InitializeBattleData();
        InitializeBattleContext();
        InitializeBattleUnits();
        InitializeStateMachines();

        _battlePhaseMachine.Fire(BattlePhasesTrigger.StartBattle);
        await _audioManager.PlayClipAsync("BackgroundMusic", "JumanjiDrums");
    }

    private async void OnDestroy()
    {
        UnsubscribeFromUiEvents();
        _battleContext.Dispose();
        await _audioManager.PlayClipAsync("BackgroundMusic", "TheHumOfCave");
    }

    private void InitializeBattleData()
    {
        if (!_sceneLoader.TryGetScenePayload(BattleSceneName, out BattleSceneData payload))
        {
            Debug.LogWarning($"[{nameof(BattleSceneManager)}.{nameof(InitializeBattleData)}] Battle scene payload was not found. Using empty battle setup.");
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

            if (_battleData.Enemies != null)
            {
                foreach (var squad in _battleData.Enemies)
                {
                    TryAddUnit(collectedUnits, squad);
                }
            }
        }
        else
        {
            Debug.LogWarning($"[{nameof(BattleSceneManager)}.{nameof(InitializeBattleUnits)}] Battle data was not resolved. No units will be spawned.");
        }

        _battleContext.RegisterSquads(collectedUnits);
    }

    private void InitializeBattleContext()
    {
        BattleEffectsManager battleEffectsManager = new();
        BattleAbilitiesManager battleAbilitiesManager = new();

        _battleContext = new BattleContext(
            _inputService,
            _battleSceneUIController,
            _battleGridController,
            _battleQueueController,
            _battleGridDragAndDropController,

            battleAbilitiesManager,
            battleEffectsManager
        );

    }

    private void InitializeStateMachines()
    {
        _battleRoundMachine = new BattleRoundsMachine(_battleContext);
        _battlePhaseMachine = new BattlePhasesMachine(_battleContext, _battleRoundMachine);
    }

    private void SubscribeToUiEvents()
    {
        if (_battleSceneUIController != null)
        {
            _battleSceneUIController.OnFinishBattle += ExitBattle;
        }
    }

    private void UnsubscribeFromUiEvents()
    {
        if (_battleSceneUIController != null)
        {
            _battleSceneUIController.OnFinishBattle -= ExitBattle;
        }
    }

    private async void ExitBattle()
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
