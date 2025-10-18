using System;
using UnityEngine;

public sealed class InteractionContext
{
    public GameObject Actor { get; }
    public GameObject Target { get; }
    public Vector3 Point { get; }
    public object Payload { get; }
    public float Time { get; }
    public SceneLoader SceneLoader { get; }

    public InteractionContext(
        GameObject actor,
        GameObject target,
        Vector3 point,
        float time,
        SceneLoader sceneLoader,
        object payload = null)
    {
        Actor = actor;
        Target = target;
        Point = point;
        Time = time;
        SceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
        Payload = payload;
    }

    public InteractionContext WithPayload(object payload)
    {
        return new InteractionContext(Actor, Target, Point, Time, SceneLoader, payload);
    }
}
