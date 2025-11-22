using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using VContainer;

public class PreparationSceneManager : MonoBehaviour
{
    [Inject] private readonly GameSession _gameSession;
    [Inject] private readonly SceneLoader _sceneLoader;
    [Inject] private readonly InputService _inputService;
    [Inject] private readonly GameEventBusService _sceneEventBusService;

    [SerializeField] private UnitSO[] _availableHeroDefinitions;
    [SerializeField] private UnitSO[] _availableSquadDefinitions;
    [SerializeField] private PreparationSceneUIController _preparationSceneUIController;

    private UnitSO[] _heroDefinitions = Array.Empty<UnitSO>();
    private UnitSO[] _squadDefinitions = Array.Empty<UnitSO>();

    private void Awake()
    {
        _heroDefinitions = NormalizeDefinitions(_availableHeroDefinitions);
        _squadDefinitions = NormalizeDefinitions(_availableSquadDefinitions);
    }

    private void Start()
    {
        _inputService.EnterMenu();
        InitalizeUI();
        SubscribeToGameEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromGameEvents();
    }

    private void InitalizeUI()
    {
        _preparationSceneUIController.Initialize(_inputService, _sceneEventBusService);
        _preparationSceneUIController.Render(_heroDefinitions, _squadDefinitions);
    }

    private void SubscribeToGameEvents()
    {
        _sceneEventBusService.SubscribeAsync<RequestDiveIntoCave>(HandleDiveIntoCaveAsync);
    }

    private void UnsubscribeFromGameEvents()
    {
        _sceneEventBusService.UnsubscribeAsync<RequestDiveIntoCave>(HandleDiveIntoCaveAsync);
    }

    private async Task HandleDiveIntoCaveAsync(RequestDiveIntoCave evt)
    {
        _gameSession.SaveSelectedHeroSquads(evt.SelectedHero, evt.SelectedSquads);
        await _sceneLoader.LoadAdditiveAsync("Dangeon_Level_1");
        await _sceneLoader.UnloadAdditiveAsync("PreparationScene");
    }

    private static UnitSO[] NormalizeDefinitions(UnitSO[] definitions)
    {
        return definitions?
            .Where(definition => definition != null)
            .ToArray() ?? Array.Empty<UnitSO>();
    }
}
