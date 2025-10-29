using UnityEngine;
using VContainer;
using VContainer.Unity;

public class RootSceneManager : MonoBehaviour
{
    [SerializeField] private AudioManager _audioManager;

    [Inject] private SceneLoader _sceneLoader;

    private static bool _audioManagerRegistered;

    private void Awake()
    {
        if (_audioManager == null)
        {
            Debug.LogWarning("[RootSceneManager] AudioManager reference is not assigned. Audio services will not be registered.");
            return;
        }

        if (_audioManagerRegistered)
        {
            return;
        }

        DontDestroyOnLoad(_audioManager.gameObject);

        LifetimeScope.Enqueue(builder =>
        {
            builder.RegisterInstance(_audioManager).AsSelf();
        });

        _audioManagerRegistered = true;
    }

    private async void Start()
    {
        await _sceneLoader.LoadAdditiveAsync("MainMenuScene");
    }
}
