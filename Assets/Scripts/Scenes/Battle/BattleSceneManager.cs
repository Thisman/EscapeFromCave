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
    private BattleQueueSystem _queueSystem;
    private BattleTargetHighlightSystem _targetHighlightSystem;
    private BattleEffectTriggerSystem _effectTriggerSystem;
    private BattleAbilityCooldownSystem _abilityCooldownSystem;

    private string _originSceneName;
    private const string BattleSceneName = "BattleScene";

    private async void Start()
    {
        SubscribeToGameEvents();
        InitializeBattleData();
        InitializeBattleContext();
        InitializeBattleUnits();
        InitializeBattleSystems();
        InitializeStateMachines();
        InitializeUI();

        await ApplyPassiveAbilitiesAsync();

        _battlePhaseMachine.Fire(BattlePhasesTrigger.StartBattle);
        await _audioManager.PlayClipAsync("BackgroundMusic", "JumanjiDrums");
    }

    private async void OnDestroy()
    {
        UnsubscribeFromGameEvents();
        _abilityCooldownSystem?.Dispose();
        _effectTriggerSystem?.Dispose();
        _targetHighlightSystem?.Dispose();
        _queueSystem?.Dispose();
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

    private void InitializeBattleSystems()
    {
        _queueSystem = new BattleQueueSystem(_battleContext);
        _targetHighlightSystem = new BattleTargetHighlightSystem(_battleContext);
        _effectTriggerSystem = new BattleEffectTriggerSystem(_battleContext);
        _abilityCooldownSystem = new BattleAbilityCooldownSystem(_battleContext);
    }

    private void InitializeStateMachines()
    {
        _battleRoundMachine = new BattleRoundsMachine(_battleContext);
        _battlePhaseMachine = new BattlePhasesMachine(_battleContext, _battleRoundMachine);
    }

    private async System.Threading.Tasks.Task ApplyPassiveAbilitiesAsync()
    {
        if (_battleContext == null)
            return;

        foreach (var squadController in _battleContext.BattleUnits)
        {
            if (squadController == null)
                continue;

            var squadModel = squadController.GetSquadModel();
            var abilities = squadModel?.Abilities;

            if (abilities == null || abilities.Length == 0)
                continue;

            foreach (var ability in abilities)
            {
                if (ability == null || ability.AbilityType != BattleAbilityType.Passive)
                    continue;

                foreach (var target in ResolvePassiveTargets(ability, squadController))
                {
                    if (target == null)
                        continue;

                    await ability.Apply(_battleContext, target);
                }
            }
        }
    }

    private IEnumerable<BattleSquadController> ResolvePassiveTargets(BattleAbilitySO ability, BattleSquadController owner)
    {
        var ownerModel = owner?.GetSquadModel();

        if (ownerModel == null)
            yield break;

        switch (ability.AbilityTargetType)
        {
            case BattleAbilityTargetType.Self:
            case BattleAbilityTargetType.SingleAlly:
                yield return owner;
                break;

            case BattleAbilityTargetType.AllAllies:
                foreach (var target in FilterUnitsBySide(ownerModel, true))
                    yield return target;
                break;

            case BattleAbilityTargetType.SingleEnemy:
            case BattleAbilityTargetType.AllEnemies:
                foreach (var target in FilterUnitsBySide(ownerModel, false))
                    yield return target;
                break;

            default:
                yield return owner;
                break;
        }
    }

    private IEnumerable<BattleSquadController> FilterUnitsBySide(IReadOnlySquadModel owner, bool sameSide)
    {
        var units = _battleContext?.BattleUnits;
        if (units == null)
            yield break;

        foreach (var unit in units)
        {
            var unitModel = unit?.GetSquadModel();
            if (unitModel == null)
                continue;

            bool isSameSide = IsSameSide(owner, unitModel);

            if (sameSide && isSameSide)
            {
                yield return unit;
            }
            else if (!sameSide && IsEnemies(owner, unitModel))
            {
                yield return unit;
            }
        }
    }

    private static bool IsSameSide(IReadOnlySquadModel actor, IReadOnlySquadModel target)
    {
        if (actor == null || target == null)
            return false;

        if (actor.IsFriendly() || actor.IsAlly() || actor.IsHero())
            return target.IsFriendly() || target.IsAlly() || target.IsHero();

        if (actor.IsEnemy())
            return target.IsEnemy();

        return actor.IsNeutral() && target.IsNeutral();
    }

    private static bool IsEnemies(IReadOnlySquadModel actor, IReadOnlySquadModel target)
    {
        if (actor == null || target == null)
            return false;

        if (actor.IsFriendly() || actor.IsAlly() || actor.IsHero())
            return target.IsEnemy();

        if (actor.IsEnemy())
            return target.IsFriendly() || target.IsAlly() || target.IsHero();

        return false;
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
