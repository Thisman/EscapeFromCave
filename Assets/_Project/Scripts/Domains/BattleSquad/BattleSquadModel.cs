using System;

public sealed class BattleSquadModel : IReadOnlySquadModel
{
    private readonly IReadOnlySquadModel _sourceModel;

    private int _squadHealth;

    public BattleSquadModel(IReadOnlySquadModel sourceModel)
    {
        _sourceModel = sourceModel ?? throw new ArgumentNullException(nameof(sourceModel));
        _squadHealth = CalculateInitialTotalHealth();
    }

    public UnitDefinitionSO Definition => _sourceModel.Definition;

    public int Count => CalculateCount();

    public bool IsEmpty => Count <= 0;

    public event Action<IReadOnlySquadModel> Changed;

    public void ApplyDamage(int damage)
    {
        if (damage <= 0)
            return;

        if (_squadHealth <= 0)
            return;

        SetSquadHealth(Math.Max(0, _squadHealth - damage));
    }

    private int CalculateInitialTotalHealth()
    {
        return _sourceModel.Count * (int)_sourceModel.Definition.BaseHealth;
    }

    private int CalculateCount()
    {
        int unitBaseHealth = (int)_sourceModel.Definition.BaseHealth;
        return (_squadHealth + unitBaseHealth - 1) / unitBaseHealth;
    }

    private void SetSquadHealth(int newSquadHealth)
    {
        newSquadHealth = Math.Max(0, newSquadHealth);
        if (_squadHealth == newSquadHealth)
            return;

        _squadHealth = newSquadHealth;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        Changed?.Invoke(this);
    }
}
