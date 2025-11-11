using System;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuUIController : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;

    private Button _startButton;

    public Action OnStartGame;

    private void OnEnable()
    {
        if (_uiDocument == null)
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        var root = _uiDocument?.rootVisualElement;
        if (root == null)
        {
            return;
        }

        _startButton = root.Q<Button>("StartGameButton");
        if (_startButton != null)
        {
            _startButton.clicked += HandleStartButtonClicked;
        }
    }

    private void OnDisable()
    {
        if (_startButton != null)
        {
            _startButton.clicked -= HandleStartButtonClicked;
            _startButton = null;
        }
    }

    private void HandleStartButtonClicked()
    {
        OnStartGame?.Invoke();
    }
}
