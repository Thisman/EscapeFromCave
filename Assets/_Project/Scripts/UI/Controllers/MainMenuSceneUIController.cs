using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuSceneUIController : MonoBehaviour
{
    [SerializeField] private Button _startButton;

    public Action OnStartGame;

    private void OnEnable()
    {
        _startButton.onClick.AddListener(() => OnStartGame?.Invoke());
    }

    private void OnDisable()
    {
        _startButton.onClick.RemoveAllListeners();
    }
}
