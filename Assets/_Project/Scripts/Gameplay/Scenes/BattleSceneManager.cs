using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class BattleSceneManager : MonoBehaviour
{
    [SerializeField] private Button _leaveBattleButton;

    [Inject] private SceneLoader _sceneLoader;

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
        await _sceneLoader.UnloadAdditiveAsync("Battle", "Cave_1");
    }
}
