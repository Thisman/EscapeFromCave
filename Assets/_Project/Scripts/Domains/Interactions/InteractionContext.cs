using UnityEngine;

public sealed class InteractionContext
{
    public GameObject Actor;

    public GameObject Target;

    public Vector3 Point;

    public object Payload;

    public float Time;

    public SceneLoader SceneLoader;

    public DialogController DialogController;

    public InputRouter InputRouter;
}
