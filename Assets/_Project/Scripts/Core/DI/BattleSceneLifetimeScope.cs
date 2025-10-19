using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class BattleSceneLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<BattleContext>(Lifetime.Singleton);
        builder.Register<StateMachine<BattleContext>>(Lifetime.Singleton);
    }
}
