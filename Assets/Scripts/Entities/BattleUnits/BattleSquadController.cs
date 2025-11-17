using System;
using System.Threading.Tasks;
using UnityEngine;

public class BattleSquadController : MonoBehaviour, IBattleDamageSource, IBattleDamageReceiver, ISquadModelProvider
{
    [SerializeField] private Collider2D _collider2D;

    private BattleSquadModel _squadModel;
    private bool _isValidTarget;
    private bool _isInteractionEnabled = true;

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

        UpdateColliderState();
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

    public void SetInteractionEnabled(bool enabled)
    {
        _isInteractionEnabled = enabled;
        UpdateColliderState();
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

    public async Task ApplyStatModifiers(BattleEffectSO source, BattleSquadStatModifier[] modifiers)
    {
        if (_squadModel == null || source == null)
            return;

        _squadModel.SetStatModifiers(source, modifiers ?? Array.Empty<BattleSquadStatModifier>());

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
        if (model is BattleSquadModel)
            UpdateColliderState();
    }

    private void UpdateColliderState()
    {
        if (_collider2D == null)
            return;

        bool squadAlive = _squadModel == null || _squadModel.Status != BattleSquadStatus.Dead;
        _collider2D.enabled = _isInteractionEnabled && squadAlive;
    }
}
