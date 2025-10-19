using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class BattleSceneLifetimeScope : LifetimeScope
{
    [SerializeField] private GameObject[] _friendlySlots;
    [SerializeField] private GameObject[] _enemySlots;

    protected override void Configure(IContainerBuilder builder)
    {
        ValidateSlots(_friendlySlots, nameof(_friendlySlots));
        ValidateSlots(_enemySlots, nameof(_enemySlots));

        builder.Register<BattleStateContext>(Lifetime.Singleton);
        builder.Register<StateMachine<BattleStateContext>>(Lifetime.Singleton);

        builder.Register<TacticState>(Lifetime.Singleton);
        builder.Register<BattleRoundState>(Lifetime.Singleton);
        builder.Register<FinishState>(Lifetime.Singleton);

        builder.RegisterInstance(new BattleGridModel(_friendlySlots, _enemySlots));
    }

    private static void ValidateSlots(GameObject[] slots, string fieldName)
    {
        if (slots == null)
            throw new InvalidOperationException($"{fieldName} must be assigned with exactly {BattleGridModel.SlotsPerSide} slots.");

        if (slots.Length != BattleGridModel.SlotsPerSide)
            throw new InvalidOperationException($"{fieldName} must contain exactly {BattleGridModel.SlotsPerSide} elements.");

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                throw new InvalidOperationException($"{fieldName}[{i}] is not assigned.");
        }
    }
}
