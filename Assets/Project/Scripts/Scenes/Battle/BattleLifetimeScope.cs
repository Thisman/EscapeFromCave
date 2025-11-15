using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class BattleLifetimeScope : LifetimeScope
{
    [SerializeField] private BattleGridController _battleGridController;
    [SerializeField] private BattleGridDragAndDropController _battleGridDragAndDropController;

    [SerializeField] private BattleUIController _battleUIController;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<BattleQueueController>(Lifetime.Singleton);

        builder.RegisterInstance(_battleGridController).AsSelf();
        builder.RegisterInstance(_battleGridDragAndDropController).AsSelf();

        builder.RegisterInstance(_battleUIController).AsSelf();
    }
}
