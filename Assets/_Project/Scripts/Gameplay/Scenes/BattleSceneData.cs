using System;
public sealed class BattleSceneData : ISceneLoadingPayload<BattleSceneData>
{
    public UnitModel Hero { get; }
    public ArmyModel Army { get; }
    public UnitModel Enemy { get; }

    public BattleSceneData(UnitModel hero, ArmyModel army, UnitModel enemy)
    {
        Hero = hero ?? throw new ArgumentNullException(nameof(hero));
        Army = army ?? throw new ArgumentNullException(nameof(army));
        Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
    }

    public BattleSceneData GetData()
    {
        return this;
    }
}
