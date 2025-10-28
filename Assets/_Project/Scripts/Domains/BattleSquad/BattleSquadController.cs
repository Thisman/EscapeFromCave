using System;
using System.Threading.Tasks;
using UnityEngine;

public class BattleSquadController : MonoBehaviour
{
    private BattleSquadModel _squadModel;

    private void OnDestroy()
    {
        _squadModel = null;
    }

    public void Initialize(BattleSquadModel squadModel)
    {
        if (squadModel is not BattleSquadModel battleSquadModel)
            throw new ArgumentException($"{nameof(BattleSquadController)} requires a {nameof(BattleSquadModel)} instance.", nameof(squadModel));

        _squadModel = battleSquadModel;
    }

    public IReadOnlySquadModel GetSquadModel()
    {
        return _squadModel;
    }

    public int ResolveDamage()
    {
        var (minDamage, maxDamage) = _squadModel.GetBaseDamageRange();
        var unitDamage = UnityEngine.Random.Range(minDamage, maxDamage);
        var unitCount = Math.Max(0, _squadModel.Count);

        return (int)unitDamage * unitCount;
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
        animationController.PlayDamageFlash(damage, () => completionSource.TrySetResult(true));
        await completionSource.Task;
    }
}
