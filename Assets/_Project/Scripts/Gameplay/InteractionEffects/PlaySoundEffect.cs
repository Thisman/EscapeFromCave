using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Effects/Play Sound")]
public sealed class PlaySoundEffect : EffectSO
{
    [SerializeField]
    private AudioClip _clip;

    [SerializeField, Range(0f, 1f)]
    private float _volume = 1f;

    [SerializeField]
    private bool _waitForCompletion = false;

    public override Task Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        if (_clip == null)
        {
            Debug.LogWarning("[PlaySoundEffect] Audio clip is not assigned. Unable to play sound.");
            return Task.CompletedTask;
        }

        var position = ctx?.Actor != null ? ctx.Actor.transform.position : Vector3.zero;
        AudioSource.PlayClipAtPoint(_clip, position, Mathf.Clamp01(_volume));

        if (!_waitForCompletion)
        {
            return Task.CompletedTask;
        }

        var duration = Mathf.Max(0f, _clip.length);
        if (duration <= 0f)
        {
            return Task.CompletedTask;
        }

        return Task.Delay(TimeSpan.FromSeconds(duration));
    }
}
