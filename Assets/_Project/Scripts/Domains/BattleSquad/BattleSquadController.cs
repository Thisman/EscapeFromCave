using System;
using UnityEngine;

public class BattleSquadController : MonoBehaviour
{
    private BattleSquadModel _squadModel;

    public BattleSquadModel Model => _squadModel;

    public void Initialize(BattleSquadModel squadModel)
    {
        if (squadModel == null)
            throw new ArgumentNullException(nameof(squadModel));

        if (_squadModel == squadModel)
            return;

        DisposeModel();
        _squadModel = squadModel;
    }

    public IReadOnlySquadModel GetSquadModel()
    {
        return _squadModel;
    }

    public int ResolveDamage()
    {
        var model = _squadModel;
        if (model == null)
            return 0;

        var definition = model.UnitDefinition;
        if (definition == null)
            return 0;

        var stats = definition.GetStatsForLevel(1);
        return stats.Damage;
    }

    public void ApplyDamage(int damage)
    {
        _ = damage;
    }

    private void OnDestroy()
    {
        DisposeModel();
    }

    private void DisposeModel()
    {
        _squadModel?.Dispose();
        _squadModel = null;
    }
}
