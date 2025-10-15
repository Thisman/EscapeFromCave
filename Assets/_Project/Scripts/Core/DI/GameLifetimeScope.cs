using UnityEngine;
using VContainer;
using VContainer.Unity;
using UnityEngine.InputSystem;

public class GameLifetimeScope : LifetimeScope
{
    [Header("References")]
    [SerializeField] private InputActionAsset inputActions;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(inputActions).As<InputActionAsset>();

        builder.Register<InputService>(Lifetime.Singleton).As<IInputService>();
        builder.Register<InputRouter>(Lifetime.Singleton);
    }
}
