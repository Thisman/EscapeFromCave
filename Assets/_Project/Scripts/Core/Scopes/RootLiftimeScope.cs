using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

[DefaultExecutionOrder(-1000)]
public sealed class RootLifetimeScope : LifetimeScope
{
    [SerializeField] private InputActionAsset inputActions;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<SceneLoader>(Lifetime.Singleton);
        builder.Register<GameSession>(Lifetime.Singleton);

        builder.RegisterInstance(inputActions).As<InputActionAsset>();
        builder.Register<InputService>(Lifetime.Singleton);
        builder.Register<InputRouter>(Lifetime.Singleton);
    }

    protected override void Awake()
    {
        var roots = FindObjectsByType<RootLifetimeScope>(FindObjectsSortMode.None);
        if (roots.Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        base.Awake();
        DontDestroyOnLoad(gameObject);
    }
}
