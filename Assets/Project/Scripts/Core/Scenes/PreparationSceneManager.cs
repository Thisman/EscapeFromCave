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

    private UnitSO[] _heroDefinitions = System.Array.Empty<UnitSO>();
    private UnitSO[] _squadDefinitions = System.Array.Empty<UnitSO>();

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

    private async void HandleStartGame(UnitSO selectedHero, List<UnitSO> selectedSquads)
    {
        _gameSession.SaveSelectedHeroSquads(selectedHero, selectedSquads);
        await _sceneLoader.LoadAdditiveAsync("Dangeon_Level_1");
        await _sceneLoader.UnloadAdditiveAsync("PreparationScene");
    }

    private void LoadUnits()
    {
        string folderPath = AssetDatabase.GetAssetPath(_unitsFolder);
        string[] guids = AssetDatabase.FindAssets("t:UnitSO", new[] { folderPath });
        List<UnitSO> heroes = new();
        List<UnitSO> allies = new();

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            UnitSO definition = AssetDatabase.LoadAssetAtPath<UnitSO>(assetPath);

            if (definition.Kind == UnitKind.Hero)
                heroes.Add(definition);
            else if (definition.Kind == UnitKind.Ally)
                allies.Add(definition);
        }

        _heroDefinitions = heroes.ToArray();
        _squadDefinitions = allies.ToArray();
    }
}
