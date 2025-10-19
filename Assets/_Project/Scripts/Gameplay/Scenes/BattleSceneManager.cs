using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class BattleSceneManager : MonoBehaviour
{
    [SerializeField] private Button _leaveBattleButton;
    [SerializeField] private Button _startBattleButton;
    [SerializeField] private Button _finishBattleButton;
    [SerializeField] private BattleGridController _battleGridContainer;

    [SerializeField] private LayerRegistration[] _layers;

    [Inject] private SceneLoader _sceneLoader;
    [Inject] private PanelController _panelController;

    [Inject] private StateMachine<BattleContext> _stateMachine;

    private void Start()
    {
        InitializeScenePayload();
        RegisterLayers();
        InitializeStateMachine();
    }

    private void OnEnable()
    {
        _startBattleButton.onClick.AddListener(() => _stateMachine.SetState<RoundState>());
        _leaveBattleButton.onClick.AddListener(() => _stateMachine.SetState<FinishState>());
        _finishBattleButton.onClick.AddListener(async () => await _sceneLoader.UnloadAdditiveWithDataAsync("Battle", null, "Cave_Level_1"));
    }

    private void OnDisable()
    {
        _leaveBattleButton.onClick.RemoveAllListeners();
        _startBattleButton.onClick.RemoveAllListeners();
        _finishBattleButton.onClick.RemoveAllListeners();
    }

    private void InitializeScenePayload()
    {
        string sceneName = gameObject.scene.name;
        _stateMachine.Context.PanelController = _panelController;
        _stateMachine.Context.BattleGridController = _battleGridContainer;
        if (_sceneLoader != null && _sceneLoader.TryGetScenePayload<BattleSceneLoadingPayload>(sceneName, out var data))
        {
            _stateMachine.Context.Payload = data;
            string heroName = data.Hero?.Definition ? data.Hero.Definition.name : "<null>";
            string enemyName = data.Enemy?.Definition ? data.Enemy.Definition.name : "<null>";
            Debug.Log($"[BattleSceneManager] Hero: {heroName}, Army slots: {data.Army?.MaxSlots ?? 0}, Enemy: {enemyName}");
        }
        else
        {
            _stateMachine.Context.Payload = null;
            Debug.LogWarning("[BattleSceneManager] Unable to retrieve battle scene data payload");
        }
    }

    private void InitializeStateMachine()
    {
        if (!_stateMachine.IsStateRegistered<TacticState>())
        {
            _stateMachine.RegisterState(new TacticState());
        }

        if (!_stateMachine.IsStateRegistered<RoundState>())
        {
            _stateMachine.RegisterState(new RoundState());
        }

        if (!_stateMachine.IsStateRegistered<FinishState>())
        {
            _stateMachine.RegisterState(new FinishState());
        }

        _stateMachine.SetState<TacticState>();
    }

    private void RegisterLayers()
    {
        if (_panelController == null || _layers == null)
        {
            return;
        }

        foreach (var layer in _layers)
        {
            if (layer == null)
            {
                continue;
            }

            var layerName = layer.LayerName;
            if (string.IsNullOrEmpty(layerName))
            {
                continue;
            }

            _panelController.Register(layerName, layer.Elements);
        }
    }

    [Serializable]
    private class LayerRegistration
    {
        [SerializeField] private string _layerName;
        [SerializeField] private GameObject[] _elements;

        public string LayerName => _layerName;
        public GameObject[] Elements => _elements;
    }
}
