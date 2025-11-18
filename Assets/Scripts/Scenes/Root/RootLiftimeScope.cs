using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

[DefaultExecutionOrder(-1000)]
public sealed class RootLifetimeScope : LifetimeScope
{
    [SerializeField] private InputActionAsset InputActions;
    [SerializeField] private AudioManager AudioManager;
    [SerializeField] private CursorManager CursorManager;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<SceneLoader>(Lifetime.Singleton);
        builder.Register<GameSession>(Lifetime.Singleton);
        builder.Register<BattleResultHandler>(Lifetime.Singleton);
        builder.Register<BattleSetupHandler>(Lifetime.Singleton);

        builder.RegisterInstance(InputActions).As<InputActionAsset>();
        builder.Register<InputService>(Lifetime.Singleton);

        builder.RegisterInstance(AudioManager).As<AudioManager>();
        builder.RegisterInstance(CursorManager).As<CursorManager>();
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
