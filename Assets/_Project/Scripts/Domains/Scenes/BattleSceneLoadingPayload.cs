using System;
public sealed class BattleSceneLoadingPayload : ISceneLoadingPayload<BattleSceneLoadingPayload>
{
    public IReadOnlyUnitModel Hero { get; }
    public IReadOnlyArmyModel Army { get; }
    public IReadOnlyUnitModel Enemy { get; }

    public BattleSceneLoadingPayload(IReadOnlyUnitModel hero, IReadOnlyArmyModel army, IReadOnlyUnitModel enemy)
    {
        Hero = hero ?? throw new ArgumentNullException(nameof(hero));
        Army = army ?? throw new ArgumentNullException(nameof(army));
        Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
    }

    public BattleSceneLoadingPayload GetData()
    {
        return this;
    }
}
