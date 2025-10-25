using System;

public sealed class BattleSquadModel : IReadOnlySquadModel, IDisposable
{
    private readonly SquadModel _sourceModel;
    private readonly int _unitHealth;
    private int _totalHealth;

    public BattleSquadModel(SquadModel sourceModel)
    {
        _sourceModel = sourceModel ?? throw new ArgumentNullException(nameof(sourceModel));
        _unitHealth = CalculateUnitHealth(_sourceModel.UnitDefinition);
        _totalHealth = CalculateInitialTotalHealth();
        _sourceModel.Changed += HandleSourceChanged;
    }

    public UnitDefinitionSO UnitDefinition => _sourceModel.UnitDefinition;

    public int Count => CalculateCount();

    public bool IsEmpty => _totalHealth <= 0;

    public SquadModel SourceModel => _sourceModel;

    public int UnitHealth => _unitHealth;

    public int TotalHealth => _totalHealth;

    public event Action<IReadOnlySquadModel> Changed;

    public void ApplyCasualties(int casualties)
    {
        if (casualties < 0)
            throw new ArgumentOutOfRangeException(nameof(casualties));

        if (casualties == 0 || _unitHealth <= 0)
            return;

        ApplyDamage(casualties * _unitHealth);
    }

    public void ApplyDamage(int damage)
    {
        if (damage <= 0)
            return;

        if (_totalHealth <= 0)
            return;

        SetTotalHealth(Math.Max(0, _totalHealth - damage));
    }

    public void SetCount(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        if (_unitHealth <= 0)
        {
            if (_totalHealth != 0)
                SetTotalHealth(0);
            return;
        }

        int newTotal = count * _unitHealth;
        SetTotalHealth(newTotal);
    }

    public void Dispose()
    {
        _sourceModel.Changed -= HandleSourceChanged;
    }

    private int CalculateInitialTotalHealth()
    {
        if (_unitHealth <= 0)
            return 0;

        int sourceCount = Math.Max(0, _sourceModel.Count);
        return sourceCount * _unitHealth;
    }

    private static int CalculateUnitHealth(UnitDefinitionSO definition)
    {
        if (definition == null)
            return 0;

        var stats = definition.GetStatsForLevel(1);
        return Math.Max(0, stats.Health);
    }

    private int CalculateCount()
    {
        if (_unitHealth <= 0)
            return 0;

        if (_totalHealth <= 0)
            return 0;

        return (_totalHealth + _unitHealth - 1) / _unitHealth;
    }

    private void HandleSourceChanged(IReadOnlySquadModel model)
    {
        int newCount = model?.Count ?? 0;
        int currentCount = Count;
        if (newCount == currentCount)
            return;

        SetCount(newCount);
    }

    private void SetTotalHealth(int newTotalHealth)
    {
        newTotalHealth = Math.Max(0, newTotalHealth);
        if (_totalHealth == newTotalHealth)
            return;

        _totalHealth = newTotalHealth;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        Changed?.Invoke(this);
    }
}
