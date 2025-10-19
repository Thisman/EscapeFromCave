using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class BattleUnitsPlacementController : MonoBehaviour
{
    [SerializeField] private GameObject _unitPrefab;

    [Inject] private IObjectResolver _resolver;
    [Inject] private BattleGridModel _battleGridModel;

    private readonly List<GameObject> _spawnedUnits = new();

    public void Arrange(IReadOnlyUnitModel hero, IReadOnlyArmyModel army, IReadOnlyList<IReadOnlyUnitModel> enemies)
    {
        if (_battleGridModel == null)
            throw new InvalidOperationException("BattleGridModel is not available for placement.");

        if (_unitPrefab == null)
        {
            Debug.LogError("[BattleUnitsPlacementController] Unit prefab is not assigned.");
            return;
        }

        ResetGrid();

        PlaceEnemies(enemies);
        PlaceHero(hero);
        PlaceArmy(army);
    }

    private void ResetGrid()
    {
        for (int i = 0; i < BattleGridModel.SlotsPerSide; i++)
        {
            _battleGridModel.TryClearFriendly(i);
            _battleGridModel.TryClearEnemy(i);
        }

        foreach (var unit in _spawnedUnits)
        {
            if (unit != null)
                Destroy(unit);
        }
        _spawnedUnits.Clear();
    }

    private void PlaceEnemies(IReadOnlyList<IReadOnlyUnitModel> enemies)
    {
        if (enemies == null || enemies.Count == 0)
            return;

        var shuffled = new List<IReadOnlyUnitModel>(enemies.Count);
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null)
                shuffled.Add(enemies[i]);
        }

        Shuffle(shuffled);

        foreach (var enemy in shuffled)
        {
            var unitObject = CreateUnitInstance(enemy?.Definition?.UnitName ?? "Enemy");
            InitializePresenter(unitObject, enemy);
            if (!_battleGridModel.TryPlaceEnemyRandom(unitObject))
            {
                Debug.LogWarning("[BattleUnitsPlacementController] Failed to place enemy unit due to lack of free enemy slots.");
                DestroyUnitInstance(unitObject);
            }
        }
    }

    private void PlaceHero(IReadOnlyUnitModel hero)
    {
        if (hero == null)
            return;

        var heroObject = CreateUnitInstance(hero.Definition != null ? hero.Definition.UnitName : "Hero");
        InitializePresenter(heroObject, hero);
        if (!_battleGridModel.TryPlaceFriendlyRandomBack(heroObject))
        {
            Debug.LogWarning("[BattleUnitsPlacementController] Failed to place hero on the back row.");
            DestroyUnitInstance(heroObject);
        }
    }

    private void PlaceArmy(IReadOnlyArmyModel army)
    {
        if (army == null)
            return;

        var squads = army.GetAllSlots();
        if (squads == null || squads.Count == 0)
            return;

        var shuffled = new List<IReadOnlySquadModel>(squads.Count);
        for (int i = 0; i < squads.Count; i++)
        {
            var squad = squads[i];
            if (squad != null && !squad.IsEmpty)
                shuffled.Add(squad);
        }

        Shuffle(shuffled);

        foreach (var squad in shuffled)
        {
            var unitObject = CreateUnitInstance(squad.UnitDefinition != null ? squad.UnitDefinition.UnitName : "Squad");
            InitializePresenter(unitObject, squad);
            if (!_battleGridModel.TryPlaceFriendlyRandomFront(unitObject))
            {
                Debug.LogWarning("[BattleUnitsPlacementController] Failed to place squad on the front row.");
                DestroyUnitInstance(unitObject);
            }
        }
    }

    private GameObject CreateUnitInstance(string baseName)
    {
        if (_resolver == null)
            throw new InvalidOperationException("ObjectResolver is not available for unit instantiation.");

        var instance = _resolver.Instantiate(_unitPrefab);
        instance.name = string.IsNullOrEmpty(baseName) ? instance.name : $"{baseName}_Instance";
        _spawnedUnits.Add(instance);
        return instance;
    }

    private static void InitializePresenter(GameObject instance, IReadOnlyUnitModel unit)
    {
        if (instance == null)
            return;

        var presenter = instance.GetComponent<BattleUnitPresenter>();
        presenter?.Initialize(unit);
    }

    private static void InitializePresenter(GameObject instance, IReadOnlySquadModel squad)
    {
        if (instance == null)
            return;

        var presenter = instance.GetComponent<BattleUnitPresenter>();
        presenter?.Initialize(squad);
    }

    private void DestroyUnitInstance(GameObject instance)
    {
        if (instance == null)
            return;

        _spawnedUnits.Remove(instance);
        Destroy(instance);
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
