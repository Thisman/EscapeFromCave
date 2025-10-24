public interface IReadOnlyBattleSquadModel : IReadOnlySquadModel, IBattleEntityModel
{
    SquadModel GetBaseModel();
}
