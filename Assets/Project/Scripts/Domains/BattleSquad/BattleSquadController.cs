using System;
using System.Threading.Tasks;
using UnityEngine;

public class BattleSquadController : MonoBehaviour, IBattleDamageSource, IBattleDamageReceiver
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

    public BattleDamageData ResolveDamage()
    {
        return _squadModel?.ResolveDamage() ?? new BattleDamageData(DamageType.Physical, 0);
    }

    public async Task ApplyDamage(BattleDamageData damageData)
    {
        if (damageData == null)
            return;

        int damage = damageData.Value;
        if (damage <= 0)
            return;

        _squadModel?.ApplyDamage(damageData);

        var animationController = GetComponentInChildren<BattleSquadAnimationController>();
        if (animationController == null)
            return;

        var completionSource = new TaskCompletionSource<bool>();
        animationController.PlayDamageFlash(damage, () => completionSource.TrySetResult(true));
        await completionSource.Task;
    }
}
