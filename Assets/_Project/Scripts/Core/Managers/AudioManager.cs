using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public sealed class AudioManager : MonoBehaviour
{
    public static class TrackNames
    {
        public const string BackgroundMusic = "BackgroundMusic";
        public const string BackgroundEffect = "BackgroundEffect";
        public const string Effects = "Effects";
    }

    private const string AudioRootFolder = "_Project/Audio";

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3",
        ".ogg",
        ".wav",
        ".aiff",
        ".aif"
    };

    [SerializeField] private AudioTrackDefinition[] _trackDefinitions =
    {
        new AudioTrackDefinition { Name = TrackNames.BackgroundMusic, Loop = true, DefaultVolume = 1f },
        new AudioTrackDefinition { Name = TrackNames.BackgroundEffect, Loop = true, DefaultVolume = 0.8f },
        new AudioTrackDefinition { Name = TrackNames.Effects, Loop = false, DefaultVolume = 1f }
    };

    [SerializeField] private AudioListener _listener;

    private readonly Dictionary<string, AudioTrackState> _tracks = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ClipLoadHandle> _clipHandles = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _clipPaths = new(StringComparer.OrdinalIgnoreCase);

    public AudioListener Listener => _listener;

    public IReadOnlyDictionary<string, AudioSource> Tracks
    {
        get
        {
            var result = new Dictionary<string, AudioSource>(_tracks.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var (name, state) in _tracks)
            {
                if (state?.ActiveSource != null)
                {
                    result[name] = state.ActiveSource;
                }
            }

            return result;
        }
    }

    private void Awake()
    {
        if (_listener == null)
        {
            _listener = FindFirstObjectByType<AudioListener>();
        }

        InitialiseTracks();
    }

    public bool TryGetTrack(string trackName, out AudioSource source)
    {
        source = null;

        if (string.IsNullOrWhiteSpace(trackName))
        {
            return false;
        }

        if (!_tracks.TryGetValue(trackName, out var trackState) || trackState == null)
        {
            Debug.LogWarning($"[AudioManager] Track '{trackName}' is not registered.");
            return false;
        }

        source = trackState.ActiveSource;
        return source != null;
    }

    public void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] Attempted to play an empty clip at point.");
            return;
        }

        AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(volume));
    }

    public async Task<bool> SetTrackClipAsync(
        string trackName,
        string clipName,
        bool loop = true,
        float transitionDuration = 1f,
        CancellationToken cancellationToken = default)
    {
        if (!_tracks.TryGetValue(trackName, out var track) || track == null)
        {
            Debug.LogWarning($"[AudioManager] Track '{trackName}' is not registered.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(clipName))
        {
            Debug.LogWarning("[AudioManager] Clip name is empty. Unable to play audio.");
            return false;
        }

        var clip = await GetClipAsync(clipName, cancellationToken).ConfigureAwait(false);

        if (clip == null)
        {
            Debug.LogWarning($"[AudioManager] Clip '{clipName}' is not loaded.");
            return false;
        }

        await track.PlayAsync(this, clip, loop, transitionDuration, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<IReadOnlyList<string>> LoadFolderAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveFullAudioPath(relativePath);

        if (!Directory.Exists(fullPath))
        {
            Debug.LogWarning($"[AudioManager] Unable to find audio folder '{fullPath}'.");
            return Array.Empty<string>();
        }

        var audioFiles = Directory.GetFiles(fullPath)
            .Where(file => SupportedExtensions.Contains(Path.GetExtension(file) ?? string.Empty))
            .ToArray();

        if (audioFiles.Length == 0)
        {
            return Array.Empty<string>();
        }

        var loadTasks = new List<Task<AudioClip>>(audioFiles.Length);
        var loadedNames = new List<string>(audioFiles.Length);

        foreach (var file in audioFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var clipName = Path.GetFileNameWithoutExtension(file);

            if (string.IsNullOrEmpty(clipName))
            {
                continue;
            }

            _clipPaths[clipName] = file;

            if (_clipHandles.TryGetValue(clipName, out var existingHandle))
            {
                if (existingHandle.Clip != null)
                {
                    loadedNames.Add(clipName);
                    continue;
                }

                if (existingHandle.LoadTask != null)
                {
                    loadTasks.Add(existingHandle.LoadTask);
                    continue;
                }
            }

            var loadTask = LoadClipInternalAsync(file, clipName, cancellationToken);
            _clipHandles[clipName] = new ClipLoadHandle { LoadTask = loadTask };
            loadTasks.Add(loadTask);
        }

        if (loadTasks.Count == 0)
        {
            return loadedNames;
        }

        var clips = await Task.WhenAll(loadTasks).ConfigureAwait(false);

        foreach (var clip in clips)
        {
            if (clip == null)
            {
                continue;
            }

            loadedNames.Add(clip.name);
        }

        return loadedNames;
    }

    public async Task<bool> PlayClipAsync(
        string trackName,
        string clipName,
        bool loop = true,
        float transitionDuration = 1f,
        CancellationToken cancellationToken = default)
    {
        return await SetTrackClipAsync(trackName, clipName, loop, transitionDuration, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<AudioClip> GetClipAsync(string clipName, CancellationToken cancellationToken)
    {
        if (_clipHandles.TryGetValue(clipName, out var handle))
        {
            if (handle.Clip != null)
            {
                return handle.Clip;
            }

            if (handle.LoadTask != null)
            {
                return await handle.LoadTask.ConfigureAwait(false);
            }
        }

        if (!_clipPaths.TryGetValue(clipName, out var filePath))
        {
            Debug.LogWarning($"[AudioManager] Clip '{clipName}' is not registered. Load a folder containing the clip first.");
            return null;
        }

        var loadTask = LoadClipInternalAsync(filePath, clipName, cancellationToken);
        _clipHandles[clipName] = new ClipLoadHandle { LoadTask = loadTask };
        return await loadTask.ConfigureAwait(false);
    }

    private async Task<AudioClip> LoadClipInternalAsync(string filePath, string clipName, CancellationToken cancellationToken)
    {
        try
        {
            var uri = new Uri(filePath);
            var audioType = ResolveAudioType(Path.GetExtension(filePath));

            using var request = UnityWebRequestMultimedia.GetAudioClip(uri.AbsoluteUri, audioType);
            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogError($"[AudioManager] Failed to load clip '{clipName}' from '{filePath}': {request.error}");
                _clipHandles.Remove(clipName);
                return null;
            }

            var clip = DownloadHandlerAudioClip.GetContent(request);
            clip.name = clipName;

            _clipHandles[clipName] = new ClipLoadHandle { Clip = clip };
            return clip;
        }
        catch (OperationCanceledException)
        {
            _clipHandles.Remove(clipName);
            throw;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[AudioManager] Unexpected error while loading clip '{clipName}': {exception}");
            _clipHandles.Remove(clipName);
            return null;
        }
    }

    private void InitialiseTracks()
    {
        _tracks.Clear();

        if (_trackDefinitions == null || _trackDefinitions.Length == 0)
        {
            _trackDefinitions = Array.Empty<AudioTrackDefinition>();
        }

        foreach (var definition in _trackDefinitions)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.Name))
            {
                continue;
            }

            if (_tracks.ContainsKey(definition.Name))
            {
                Debug.LogWarning($"[AudioManager] Duplicate track name '{definition.Name}' detected. Skipping.");
                continue;
            }

            var state = new AudioTrackState(definition, transform);
            _tracks[definition.Name] = state;
        }
    }

    private static string ResolveFullAudioPath(string relativePath)
    {
        var projectPath = Application.dataPath;
        var combined = string.IsNullOrWhiteSpace(relativePath)
            ? Path.Combine(projectPath, AudioRootFolder)
            : Path.Combine(projectPath, AudioRootFolder, relativePath);

        return Path.GetFullPath(combined);
    }

    private static AudioType ResolveAudioType(string extension)
    {
        if (string.IsNullOrEmpty(extension))
        {
            return AudioType.UNKNOWN;
        }

        switch (extension.ToLowerInvariant())
        {
            case ".mp3":
                return AudioType.MPEG;
            case ".ogg":
                return AudioType.OGGVORBIS;
            case ".wav":
                return AudioType.WAV;
            case ".aif":
            case ".aiff":
                return AudioType.AIFF;
            default:
                return AudioType.UNKNOWN;
        }
    }

    [Serializable]
    private sealed class AudioTrackDefinition
    {
        public string Name;
        public bool Loop = true;
        [Range(0f, 1f)] public float DefaultVolume = 1f;
    }

    private sealed class ClipLoadHandle
    {
        public Task<AudioClip> LoadTask;
        public AudioClip Clip;
    }

    private sealed class AudioTrackState
    {
        private readonly AudioTrackDefinition _definition;
        private readonly AudioSource _firstSource;
        private readonly AudioSource _secondSource;

        private AudioSource _activeSource;
        private AudioSource _inactiveSource;
        private Coroutine _transitionRoutine;

        public AudioTrackState(AudioTrackDefinition definition, Transform parent)
        {
            _definition = definition;
            _firstSource = CreateSource(definition, parent, definition.Name + "_A");
            _secondSource = CreateSource(definition, parent, definition.Name + "_B");

            _activeSource = _firstSource;
            _inactiveSource = _secondSource;
        }

        public AudioSource ActiveSource => _activeSource;

        public async Task PlayAsync(
            MonoBehaviour owner,
            AudioClip clip,
            bool loop,
            float transitionDuration,
            CancellationToken cancellationToken)
        {
            if (owner == null)
            {
                Debug.LogWarning("[AudioManager] Owner is missing. Unable to play audio.");
                return;
            }

            if (clip == null)
            {
                return;
            }

            loop = loop || _definition.Loop;

            if (_transitionRoutine != null)
            {
                owner.StopCoroutine(_transitionRoutine);
            }

            _transitionRoutine = owner.StartCoroutine(TransitionRoutine(clip, loop, transitionDuration, cancellationToken));

            while (_transitionRoutine != null)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    owner.StopCoroutine(_transitionRoutine);
                    _transitionRoutine = null;
                    cancellationToken.ThrowIfCancellationRequested();
                }

                await Task.Yield();
            }
        }

        private IEnumerator TransitionRoutine(AudioClip clip, bool loop, float duration, CancellationToken cancellationToken)
        {
            var active = _activeSource;
            var inactive = _inactiveSource;

            PrepareSource(inactive, clip, loop, 0f);

            if (duration <= 0f)
            {
                if (active != null)
                {
                    active.Stop();
                    active.volume = _definition.DefaultVolume;
                }

                if (inactive != null)
                {
                    inactive.volume = _definition.DefaultVolume;
                    inactive.Play();
                }

                SwapSources();
                _transitionRoutine = null;
                yield break;
            }

            inactive?.Play();

            var elapsed = 0f;
            var initialActiveVolume = active != null ? active.volume : 0f;
            var targetVolume = Mathf.Clamp01(_definition.DefaultVolume);

            while (elapsed < duration)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    inactive?.Stop();
                    _transitionRoutine = null;
                    yield break;
                }

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);

                if (active != null)
                {
                    active.volume = Mathf.Lerp(initialActiveVolume, 0f, t);
                }

                if (inactive != null)
                {
                    inactive.volume = Mathf.Lerp(0f, targetVolume, t);
                }

                yield return null;
            }

            if (active != null)
            {
                active.Stop();
                active.volume = targetVolume;
            }

            if (inactive != null)
            {
                inactive.volume = targetVolume;
            }

            SwapSources();
            _transitionRoutine = null;
        }

        private void SwapSources()
        {
            (_activeSource, _inactiveSource) = (_inactiveSource, _activeSource);
        }

        private void PrepareSource(AudioSource source, AudioClip clip, bool loop, float volume)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
            source.clip = clip;
            source.loop = loop;
            source.volume = volume;
            source.playOnAwake = false;
        }

        private static AudioSource CreateSource(AudioTrackDefinition definition, Transform parent, string objectName)
        {
            var trackObject = new GameObject(objectName)
            {
                hideFlags = HideFlags.DontSave
            };

            trackObject.transform.SetParent(parent, false);

            var source = trackObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = definition.Loop;
            source.volume = Mathf.Clamp01(definition.DefaultVolume);

            return source;
        }
    }
}
