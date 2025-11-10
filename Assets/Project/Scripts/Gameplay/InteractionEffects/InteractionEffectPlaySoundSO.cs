using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "PlaySoundEffect", menuName = "Gameplay/Effects/Play Sound")]
public sealed class InteractionEffectPlaySoundSO : InteractionEffectSO
{
    [SerializeField] private AudioClip _clip;

    [SerializeField, Range(0f, 1f)] private float _volume = 1f;

    [SerializeField] private bool _waitForCompletion = false;

    public override async Task<InteractionEffectResult> Apply(InteractionContext ctx, IReadOnlyList<GameObject> targets)
    {
        if (_clip == null)
        {
            Debug.LogWarning($"[{nameof(InteractionEffectPlaySoundSO)}.{nameof(Apply)}] Audio clip is not assigned. Unable to play sound.");
            return InteractionEffectResult.Continue;
        }

        var position = ctx.Actor.transform.position;
        AudioSource.PlayClipAtPoint(_clip, position, Mathf.Clamp01(_volume));

        if (_waitForCompletion)
        {
            float duration = Mathf.Max(0f, _clip.length);
            if (duration > 0f)
            {
                await Task.Delay(TimeSpan.FromSeconds(duration));
            }
        }

        return InteractionEffectResult.Continue;
    }
}
