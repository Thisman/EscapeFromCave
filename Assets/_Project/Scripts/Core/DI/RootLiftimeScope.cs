using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

[DefaultExecutionOrder(-1000)]
public sealed class RootLifetimeScope : LifetimeScope
{
    [Header("References")]
    [SerializeField] private InputActionAsset inputActions;

    protected override void Configure(IContainerBuilder b)
    {
        GameServicesModule.Register(b);
        InteractionModule.Register(b);
        InputModule.Register(b, inputActions);
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
