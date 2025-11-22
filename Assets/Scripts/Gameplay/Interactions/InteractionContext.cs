using UnityEngine;

public sealed class InteractionContext
{
    public float Time;
    
    public GameObject Actor;

    public GameObject Target;

    public SceneLoader SceneLoader;

    public InputService InputService;

    public DialogManager DialogManager;

    public BattleSetupHandler BattleSetupHandler;
    
    public BattleResultHandler BattleResultHandler;

    public GameEventBusService SceneEventBusService;
}
