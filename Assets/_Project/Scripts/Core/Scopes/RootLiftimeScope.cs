using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

[DefaultExecutionOrder(-1000)]
public sealed class RootLifetimeScope : LifetimeScope
{
    [SerializeField] private InputActionAsset inputActions;

    protected override void Configure(IContainerBuilder b)
    {
        b.Register<SceneLoader>(Lifetime.Singleton);
        b.Register<GameSession>(Lifetime.Singleton);

        b.RegisterInstance(inputActions).As<InputActionAsset>();
        b.Register<InputService>(Lifetime.Singleton);
        b.Register<InputRouter>(Lifetime.Singleton);
    }

    protected override void Awake()
    {
        var roots = FindObjectsOfType<RootLifetimeScope>();
        if (roots.Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        base.Awake();
        DontDestroyOnLoad(gameObject);
    }
}
