using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class BattleLifetimeScope : LifetimeScope
{
    [SerializeField] private BattleGridController _battleGridController;
    [SerializeField] private BattleGridDragAndDropController _battleGridDragAndDropController;

    [SerializeField] private BattleSceneUIController _battleSceneUIController;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<BattleQueueController>(Lifetime.Singleton);

        builder.RegisterInstance(_battleGridController).AsSelf();
        builder.RegisterInstance(_battleGridDragAndDropController).AsSelf();

        builder.RegisterInstance(_battleSceneUIController).AsSelf();
        builder.Register<GameEventBusService>(Lifetime.Singleton);
    }
}
