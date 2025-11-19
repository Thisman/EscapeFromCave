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

    [SerializeField] private UnitSO[] _availableHeroDefinitions;
    [SerializeField] private UnitSO[] _availableSquadDefinitions;
    [SerializeField] private PreparationSceneUIController _preparationSceneUIController;

    private UnitSO[] _heroDefinitions = Array.Empty<UnitSO>();
    private UnitSO[] _squadDefinitions = Array.Empty<UnitSO>();

    private void Awake()
    {
        _heroDefinitions = NormalizeDefinitions(_availableHeroDefinitions);
        _squadDefinitions = NormalizeDefinitions(_availableSquadDefinitions);
        _preparationSceneUIController.Render(_heroDefinitions, _squadDefinitions);
    }

    private void OnEnable()
    {
        _preparationSceneUIController.OnDiveIntoCave += HandleDiveIntoCaveAsync;
    }

    private void OnDisable()
    {
        _preparationSceneUIController.OnDiveIntoCave -= HandleDiveIntoCaveAsync;
    }

    private async Task HandleDiveIntoCaveAsync(UnitSO selectedHero, List<SquadSelection> selectedSquads)
    {
        _gameSession.SaveSelectedHeroSquads(selectedHero, selectedSquads);
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
