using UnityEngine;

public sealed class BattleSquadController : MonoBehaviour
{
    private BattleSquadModel _battleSquadModel;

    public void Initialize(BattleSquadModel battleSquadModel)
    {
        _battleSquadModel = battleSquadModel;
    }

    public IReadOnlyBattleSquadModel GetBattleSquadModel()
    {
        return _battleSquadModel;
    }
}
