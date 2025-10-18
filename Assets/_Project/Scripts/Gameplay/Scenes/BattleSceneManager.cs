using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class BattleSceneManager : MonoBehaviour
{
    [SerializeField] private Button _leaveBattleButton;

    [Inject] private SceneLoader _sceneLoader;
    [Inject] private StateMachine<BattleStateContext> _stateMachine;

    private TacticState _tacticState;
    private FightState _fightState;
    private FinishState _finishState;

    private void Start()
    {
        string sceneName = gameObject.scene.name;
        if (_sceneLoader != null && _sceneLoader.TryGetScenePayload<BattleSceneLoadingPayload>(sceneName, out var data))
        {
            _stateMachine.Context.SetPayload(data);
            string heroName = data.Hero?.Definition ? data.Hero.Definition.name : "<null>";
            string enemyName = data.Enemy?.Definition ? data.Enemy.Definition.name : "<null>";
            Debug.Log($"[BattleSceneManager] Hero: {heroName}, Army slots: {data.Army?.MaxSlots ?? 0}, Enemy: {enemyName}");
        }
        else
        {
            _stateMachine.Context.SetPayload(null);
            Debug.LogWarning("[BattleSceneManager] Unable to retrieve battle scene data payload");
        }

        InitializeStateMachine();
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

    private void InitializeStateMachine()
    {
        _tacticState ??= new TacticState();
        _fightState ??= new FightState();
        _finishState ??= new FinishState();

        if (!_stateMachine.IsStateRegistered<TacticState>())
        {
            _stateMachine.RegisterState(_tacticState);
        }

        if (!_stateMachine.IsStateRegistered<FightState>())
        {
            _stateMachine.RegisterState(_fightState);
        }

        if (!_stateMachine.IsStateRegistered<FinishState>())
        {
            _stateMachine.RegisterState(_finishState);
        }

        _stateMachine.SetState<TacticState>();
    }
}
