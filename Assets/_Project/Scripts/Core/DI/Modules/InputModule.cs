using UnityEngine.InputSystem;
using VContainer;

public static class InputModule
{
    public static void Register(IContainerBuilder builder, InputActionAsset inputActions)
    {
        builder.RegisterInstance(inputActions).As<InputActionAsset>();
        builder.Register<InputService>(Lifetime.Singleton).As<IInputService>();
        builder.Register<InputRouter>(Lifetime.Singleton);
    }
}
