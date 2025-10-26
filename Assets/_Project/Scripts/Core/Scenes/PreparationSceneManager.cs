using UnityEngine;
using UnityEngine.UI;
using VContainer;
using System.Collections.Generic;

public class PreparationSceneManager : MonoBehaviour
{
    [Inject] private readonly GameSession _gameSession;
    [Inject] private readonly SceneLoader _sceneLoader;

    [SerializeField] private PreparationMenuUIController _preparationSceneUIController;

    private void OnEnable()
    {
        _preparationSceneUIController.OnStartGame += HandleStartGame;
    }

    private void OnDisable()
    {
        _preparationSceneUIController.OnStartGame -= HandleStartGame;
    }

    private async void HandleStartGame(UnitDefinitionSO selectedHero, List<UnitDefinitionSO> selectedSquads)
    {
        _gameSession.SelectHeroAndArmy(selectedHero, selectedSquads);
        await _sceneLoader.UnloadAdditiveAsync("PreparationScene");
        await _sceneLoader.LoadAdditiveAsync("Dangeon_Level_1");
    }
}
