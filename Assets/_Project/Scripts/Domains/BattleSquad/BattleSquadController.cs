using System;
using System.Threading.Tasks;
using UnityEngine;

public class BattleSquadController : MonoBehaviour
{
    private BattleSquadModel _squadModel;
    private IReadOnlySquadModel _readOnlyModel;

    public BattleSquadModel Model => _squadModel;

    private void OnDestroy()
    {
        DisposeModel();
    }

    public void Initialize(IReadOnlySquadModel squadModel)
    {
        if (squadModel == null)
            throw new ArgumentNullException(nameof(squadModel));

        if (_readOnlyModel == squadModel)
            return;

        if (squadModel is not BattleSquadModel battleSquadModel)
            throw new ArgumentException($"{nameof(BattleSquadController)} requires a {nameof(BattleSquadModel)} instance.", nameof(squadModel));

        DisposeModel();
        _squadModel = battleSquadModel;
        _readOnlyModel = battleSquadModel;
    }

    public IReadOnlySquadModel GetSquadModel()
    {
        return _readOnlyModel;
    }

    public int ResolveDamage()
    {
        var model = _squadModel;
        if (model == null)
            return 0;

        var definition = model.Definition;
        if (definition == null)
            return 0;

        var stats = definition.GetStatsForLevel(1);
        var unitDamage = Math.Max(0, stats.Damage);
        var unitCount = Math.Max(0, model.Count);

        if (unitDamage == 0 || unitCount == 0)
            return 0;

        return unitDamage * unitCount;
    }

    public async Task ApplyDamage(int damage)
    {
        if (damage <= 0)
            return;

        _squadModel?.ApplyDamage(damage);

        var animationController = GetComponentInChildren<BattleSquadAnimationController>();
        if (animationController == null)
            return;

        var completionSource = new TaskCompletionSource<bool>();
        animationController.PlayDamageFlash(() => completionSource.TrySetResult(true));
        await completionSource.Task;
    }

    private void DisposeModel()
    {
        _squadModel?.Dispose();
        _squadModel = null;
        _readOnlyModel = null;
    }
}
