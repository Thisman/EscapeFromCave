using System;
using System.Threading.Tasks;
using UnityEngine;

public class BattleSquadController : MonoBehaviour, IBattleDamageSource, IBattleDamageReceiver, ISquadModelProvider
{
    [SerializeField] private Collider2D _collider2D;

    private BattleSquadModel _squadModel;
    private bool _isValidTarget;

    private void Awake()
    {
        _collider2D ??= GetComponent<Collider2D>();
    }

    private void OnDestroy()
    {
        if (_squadModel != null)
            _squadModel.Changed -= HandleSquadModelChanged;

        _squadModel = null;
    }

    public void Initialize(BattleSquadModel squadModel)
    {
        if (squadModel is not BattleSquadModel battleSquadModel)
            throw new ArgumentException($"{nameof(BattleSquadController)} requires a {nameof(BattleSquadModel)} instance.", nameof(squadModel));

        if (_squadModel != null)
            _squadModel.Changed -= HandleSquadModelChanged;

        _squadModel = battleSquadModel;
        _squadModel.Changed += HandleSquadModelChanged;

        UpdateColliderState(_squadModel);
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

    private void HandleSquadModelChanged(IReadOnlySquadModel model)
    {
        if (model is BattleSquadModel battleModel)
            UpdateColliderState(battleModel);
    }

    private void UpdateColliderState(BattleSquadModel model)
    {
        if (_collider2D == null || model == null)
            return;

        _collider2D.enabled = model.Status != BattleSquadStatus.Dead;
    }
}
