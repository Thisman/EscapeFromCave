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
        if (_stateMachine == null)
        {
            Debug.LogError("[BattleSceneManager] State machine dependency is missing. Battle flow cannot start.");
            return;
        }

        if (_panelController == null)
        {
            Debug.LogWarning("[BattleSceneManager] PanelController was not injected. UI layers will not be registered.");
        }

        if (_battleGridContainer == null)
        {
            Debug.LogWarning("[BattleSceneManager] BattleGridController reference is missing.");
        }

        InitializeScenePayload();
        RegisterLayers();
        InitializeStateMachine();
    }

    private void OnEnable()
    {
        if (_stateMachine == null)
        {
            Debug.LogError("[BattleSceneManager] Cannot wire button callbacks because state machine is null.");
            return;
        }

        if (_startBattleButton == null)
        {
            Debug.LogError("[BattleSceneManager] Start battle button is not assigned.");
        }
        else
        {
            _startBattleButton.onClick.AddListener(() => _stateMachine.SetState<RoundState>());
        }

        if (_leaveBattleButton == null)
        {
            Debug.LogError("[BattleSceneManager] Leave battle button is not assigned.");
        }
        else
        {
            _leaveBattleButton.onClick.AddListener(() => _stateMachine.SetState<FinishState>());
        }

        if (_finishBattleButton == null)
        {
            Debug.LogError("[BattleSceneManager] Finish battle button is not assigned.");
        }
        else if (_sceneLoader == null)
        {
            Debug.LogError("[BattleSceneManager] SceneLoader is null. Cannot register finish battle handler.");
        }
        else
        {
            _finishBattleButton.onClick.AddListener(async () => await _sceneLoader.UnloadAdditiveWithDataAsync("Battle", null, "Cave_Level_1"));
        }
    }

    private void OnDisable()
    {
        _leaveBattleButton?.onClick.RemoveAllListeners();
        _startBattleButton?.onClick.RemoveAllListeners();
        _finishBattleButton?.onClick.RemoveAllListeners();
    }

    private void InitializeScenePayload()
    {
        string sceneName = gameObject.scene.name;
        _stateMachine.Context.PanelController = _panelController;
        _stateMachine.Context.BattleGridController = _battleGridContainer;
        if (_sceneLoader == null)
        {
            Debug.LogError("[BattleSceneManager] SceneLoader was not injected. Unable to acquire payload.");
            _stateMachine.Context.Payload = null;
            return;
        }

        if (_sceneLoader.TryGetScenePayload<BattleSceneLoadingPayload>(sceneName, out var data))
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
        if (_panelController == null)
        {
            Debug.LogWarning("[BattleSceneManager] PanelController is null. Skipping UI layer registration.");
            return;
        }

        if (_layers == null || _layers.Length == 0)
        {
            Debug.LogWarning("[BattleSceneManager] No UI layers configured for registration.");
            return;
        }

        foreach (var layer in _layers)
        {
            if (layer == null)
            {
                Debug.LogWarning("[BattleSceneManager] Encountered null layer registration entry.");
                continue;
            }

            var layerName = layer.LayerName;
            if (string.IsNullOrEmpty(layerName))
            {
                Debug.LogWarning("[BattleSceneManager] Layer registration is missing a layer name.");
                continue;
            }

            if (layer.Elements == null || layer.Elements.Length == 0)
            {
                Debug.LogWarning($"[BattleSceneManager] Layer '{layerName}' has no elements to register.");
                continue;
            }

            _panelController.Register(layerName, layer.Elements);
            Debug.Log($"[BattleSceneManager] Registered UI layer '{layerName}' with {layer.Elements.Length} elements.");
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
