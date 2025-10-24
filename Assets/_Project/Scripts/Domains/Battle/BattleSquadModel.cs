using System;

public sealed class BattleSquadModel : IReadOnlySquadModel, IDisposable
{
    private readonly SquadModel _sourceModel;
    private int _count;

    public BattleSquadModel(SquadModel sourceModel)
    {
        _sourceModel = sourceModel ?? throw new ArgumentNullException(nameof(sourceModel));
        _count = _sourceModel.Count;
        _sourceModel.Changed += HandleSourceChanged;
    }

    public UnitDefinitionSO UnitDefinition => _sourceModel.UnitDefinition;

    public int Count => _count;

    public bool IsEmpty => _count <= 0;

    public SquadModel SourceModel => _sourceModel;

    public event Action<IReadOnlySquadModel> Changed;

    public void ApplyCasualties(int casualties)
    {
        if (casualties < 0)
            throw new ArgumentOutOfRangeException(nameof(casualties));

        if (casualties == 0)
            return;

        SetCount(Math.Max(0, _count - casualties));
    }

    public void SetCount(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        if (_count == count)
            return;

        _count = count;
        NotifyChanged();
    }

    public void Dispose()
    {
        _sourceModel.Changed -= HandleSourceChanged;
    }

    private void HandleSourceChanged(IReadOnlySquadModel model)
    {
        int newCount = model?.Count ?? 0;
        if (_count == newCount)
            return;

        _count = newCount;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        Changed?.Invoke(this);
    }
}
