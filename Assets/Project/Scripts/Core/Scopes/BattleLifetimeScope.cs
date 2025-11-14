using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class BattleLifetimeScope : LifetimeScope
{
    [SerializeField] private BattleGridController _battleGridController;
    [SerializeField] private BattleGridDragAndDropController _battleGridDragAndDropController;

    [SerializeField] private BattleQueueUIController _queueUIController;
    [SerializeField] private BattleTacticUIController _tacticUIController;
    [SerializeField] private BattleCombatUIController _combatUIController;
    [SerializeField] private BattleResultsUIController _resultsUIController;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<BattleQueueController>(Lifetime.Singleton);

        builder.RegisterInstance(_battleGridController).AsSelf();
        builder.RegisterInstance(_battleGridDragAndDropController).AsSelf();

        builder.RegisterInstance(_queueUIController).AsSelf();
        builder.RegisterInstance(_combatUIController).AsSelf();
        builder.RegisterInstance(_tacticUIController).AsSelf();
        builder.RegisterInstance(_resultsUIController).AsSelf();
    }
}
