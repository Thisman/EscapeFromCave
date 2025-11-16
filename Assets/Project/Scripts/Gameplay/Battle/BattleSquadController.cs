using System;
using System.Threading.Tasks;
using UnityEngine;

public class BattleSquadController : MonoBehaviour, IBattleDamageSource, IBattleDamageReceiver, ISquadModelProvider
{
    private BattleSquadModel _squadModel;
    private bool _isValidTarget;

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

    public bool IsValidTarget()
    {
        return _isValidTarget;
    }

    public void SetTargetValidity(bool isValid)
    {
        _isValidTarget = isValid;
    }

    public BattleDamageData ResolveDamage()
    {
        return _squadModel.ResolveDamage();
    }

    public async Task ApplyDamage(BattleDamageData damageData)
    {
        if (damageData == null)
            return;

        int damage = damageData.Value;

        bool damageApplied = damage > 0;
        if (_squadModel != null)
            damageApplied = _squadModel.ApplyDamage(damageData);

        var animationController = GetComponentInChildren<BattleSquadAnimationController>();
        if (animationController == null)
            return;

        var completionSource = new TaskCompletionSource<bool>();
        if (damageApplied)
            animationController.PlayDamageFlash(damage, () => completionSource.TrySetResult(true));
        else
            animationController.PlayDodgeFlash(() => completionSource.TrySetResult(true));

        await completionSource.Task;
    }

    public async Task ApplyStatModifiers(BattleEffectSO source, BattleStatModifier[] modifiers)
    {
        if (_squadModel == null || source == null)
            return;

        _squadModel.SetStatModifiers(source, modifiers ?? Array.Empty<BattleStatModifier>());

        var animationController = GetComponentInChildren<BattleSquadAnimationController>();
        if (animationController == null)
            return;

        var completionSource = new TaskCompletionSource<bool>();
        animationController.PlayStatModifierFlash(() => completionSource.TrySetResult(true));

        await completionSource.Task;
    }

    public void RemoveStatModifiers(BattleEffectSO source)
    {
        if (_squadModel == null || source == null)
            return;

        _squadModel.RemoveStatModifiers(source);
    }
}
