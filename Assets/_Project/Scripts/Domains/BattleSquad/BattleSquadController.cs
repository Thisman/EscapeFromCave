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
