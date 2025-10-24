public interface IBattleEntityModel
{
    UnitDefinitionSO Definition { get; }

    int GetInitiative();
}
