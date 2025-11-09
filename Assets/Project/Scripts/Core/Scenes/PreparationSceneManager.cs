using UnityEngine;
using VContainer;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PreparationSceneManager : MonoBehaviour
{
    [Inject] private readonly GameSession _gameSession;
    [Inject] private readonly SceneLoader _sceneLoader;

    [SerializeField] private PreparationMenuUIController _preparationSceneUIController;
    [SerializeField] private DefaultAsset _unitsFolder;

    private UnitDefinitionSO[] _heroDefinitions = System.Array.Empty<UnitDefinitionSO>();
    private UnitDefinitionSO[] _squadDefinitions = System.Array.Empty<UnitDefinitionSO>();

    private void Awake()
    {
        LoadUnits();
        _preparationSceneUIController.PopulateCarousels(_heroDefinitions, _squadDefinitions);
    }

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
        _gameSession.SaveSelectedHeroSquads(selectedHero, selectedSquads);
        await _sceneLoader.LoadAdditiveAsync("Dangeon_Level_1");
        await _sceneLoader.UnloadAdditiveAsync("PreparationScene");
    }

    private void LoadUnits()
    {
        string folderPath = AssetDatabase.GetAssetPath(_unitsFolder);
        string[] guids = AssetDatabase.FindAssets("t:UnitDefinitionSO", new[] { folderPath });
        List<UnitDefinitionSO> heroes = new();
        List<UnitDefinitionSO> allies = new();

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            UnitDefinitionSO definition = AssetDatabase.LoadAssetAtPath<UnitDefinitionSO>(assetPath);

            if (definition.Kind == UnitKind.Hero)
                heroes.Add(definition);
            else if (definition.Kind == UnitKind.Ally)
                allies.Add(definition);
        }

        _heroDefinitions = heroes.ToArray();
        _squadDefinitions = allies.ToArray();
    }
}
