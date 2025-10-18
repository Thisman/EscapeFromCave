using VContainer;
using VContainer.Unity;

public class BattleSceneLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<BattleStateContext>(Lifetime.Singleton);
        builder.Register<StateMachine<BattleStateContext>>(Lifetime.Singleton);

        builder.Register<TacticState>(Lifetime.Singleton);
        builder.Register<FightState>(Lifetime.Singleton);
        builder.Register<FinishState>(Lifetime.Singleton);
    }
}
