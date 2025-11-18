using UnityEngine;

public sealed class InteractionContext
{
    public GameObject Actor;

    public GameObject Target;

    public Vector3 Point;

    public float Time;

    public SceneLoader SceneLoader;

    public DialogManager DialogManager;

    public InputService InputService;

    public IBattleResultHandler BattleResultHandler;
}
