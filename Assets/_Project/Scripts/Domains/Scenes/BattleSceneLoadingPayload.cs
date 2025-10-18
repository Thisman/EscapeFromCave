using System;
public sealed class BattleSceneLoadingPayload : ISceneLoadingPayload<BattleSceneLoadingPayload>
{
    public UnitModel Hero { get; }
    public ArmyModel Army { get; }
    public UnitModel Enemy { get; }

    public BattleSceneLoadingPayload(UnitModel hero, ArmyModel army, UnitModel enemy)
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
