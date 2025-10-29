using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

[DefaultExecutionOrder(-1000)]
public sealed class RootLifetimeScope : LifetimeScope
{
    [SerializeField] private InputActionAsset InputActions;
    [SerializeField] private AudioManager AudioManager;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<SceneLoader>(Lifetime.Singleton);
        builder.Register<GameSession>(Lifetime.Singleton);

        builder.RegisterInstance(InputActions).As<InputActionAsset>();
        builder.Register<InputService>(Lifetime.Singleton);
        builder.Register<InputRouter>(Lifetime.Singleton);

        builder.RegisterInstance(AudioManager).As<AudioManager>();
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
