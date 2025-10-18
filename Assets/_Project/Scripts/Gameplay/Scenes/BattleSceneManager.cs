using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class BattleSceneManager : MonoBehaviour
{
    [SerializeField] private Button _leaveBattleButton;

    [Inject] private SceneLoader _sceneLoader;

    private void Start()
    {
        string sceneName = gameObject.scene.name;
        if (_sceneLoader != null && _sceneLoader.TryGetScenePayload<BattleSceneData>(sceneName, out var data))
        {
            string heroName = data.Hero?.Definition ? data.Hero.Definition.name : "<null>";
            string enemyName = data.Enemy?.Definition ? data.Enemy.Definition.name : "<null>";
            Debug.Log($"[BattleSceneManager] Hero: {heroName}, Army slots: {data.Army?.MaxSlots ?? 0}, Enemy: {enemyName}");
        }
        else
        {
            Debug.LogWarning("[BattleSceneManager] Unable to retrieve battle scene data payload");
        }
    }

    private void OnEnable()
    {
        _leaveBattleButton.onClick.AddListener(OnLeaveBattleButtonClicked);
    }

    private void OnDisable()
    {
        _leaveBattleButton.onClick.RemoveListener(OnLeaveBattleButtonClicked);
    }

    private async void OnLeaveBattleButtonClicked()
    {
        await _sceneLoader.CloseAdditiveWithDataAsync("Battle", null, "Cave_Level_1");
    }
}
