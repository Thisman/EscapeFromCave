using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class BattleLifetimeScope : LifetimeScope
{
    [SerializeField] BattleGridController _battleGridController;
    [SerializeField] BattleGridDragAndDropController _battleGridDragAndDropController;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<BattleQueueController>(Lifetime.Scoped);

        // TODO: register didn't work
        if (_battleGridController != null)
        {
            builder.RegisterInstance(_battleGridController).AsSelf();
        }
        else
        {
            Debug.LogWarning("[GameLifetimeScope] BattleGridController reference is missing. Dialog interactions will be unavailable.");
        }

        if (_battleGridDragAndDropController != null)
        {
            builder.RegisterInstance(_battleGridDragAndDropController).AsSelf();
        }
        else
        {
            Debug.LogWarning("[GameLifetimeScope] BattleGridDragAndDropController reference is missing. Dialog interactions will be unavailable.");
        }
    }
}
