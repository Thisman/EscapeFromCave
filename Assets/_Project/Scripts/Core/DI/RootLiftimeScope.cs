using UnityEngine;
using VContainer;
using VContainer.Unity;

[DefaultExecutionOrder(-1000)]
public sealed class RootLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder b)
    {
        b.Register<GameSession>(Lifetime.Singleton).As<IGameSession>();
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
