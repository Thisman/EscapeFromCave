using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class BattleLifetimeScope : LifetimeScope
{
    [SerializeField] BattleGridController _battleGridController;
    [SerializeField] BattleGridDragAndDropController _battleGridDragAndDropController;

    [SerializeField] private BattleQueueUIController _queueUIController;
    [SerializeField] private BattleTacticUIController _tacticUIController;
    [SerializeField] private BattleCombatUIController _combatUIController;
    [SerializeField] private BattleResultsUIController _resultsUIController;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<BattleQueueController>(Lifetime.Singleton);

        if (_battleGridController != null)
        {
            builder.RegisterInstance(_battleGridController).AsSelf();
        }
        else
        {
            Debug.LogWarning("[GameLifetimeScope] BattleGridController reference is missing.");
        }

        if (_battleGridDragAndDropController != null)
        {
            builder.RegisterInstance(_battleGridDragAndDropController).AsSelf();
        }
        else
        {
            Debug.LogWarning("[GameLifetimeScope] BattleGridDragAndDropController reference is missing.");
        }

        if (_queueUIController != null)
        {
            builder.RegisterInstance(_queueUIController).AsSelf();
        }
        else
        {
            Debug.LogWarning("[GameLifetimeScope] BattleQueueUIController reference is missing.");
        }

        if (_combatUIController != null)
        {
            builder.RegisterInstance(_combatUIController).AsSelf();
        }
        else
        {
            Debug.LogWarning("[GameLifetimeScope] BattleCombatUIController reference is missing.");
        }

        if (_tacticUIController != null)
        {
            builder.RegisterInstance(_tacticUIController).AsSelf();
        }
        else
        {
            Debug.LogWarning("[GameLifetimeScope] BattleTacticUIController reference is missing.");
        }

        if (_resultsUIController != null)
        {
            builder.RegisterInstance(_resultsUIController).AsSelf();
        }
        else
        {
            Debug.LogWarning("[GameLifetimeScope] BattleResultsUIController reference is missing.");
        }
    }
}
