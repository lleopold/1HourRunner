using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    const string KEY_ENABLED = "music_enabled";
    const string KEY_VOLUME = "music_volume";

    [Header("UI Music")]
    public AudioClip uiMusic;
    public List<string> uiScenes = new() { "Preload", "ChoosePlayer", "ChooseWeapon", "ChooseLevel" };

    [Header("Gameplay Music")]
    public bool autoloadGameplayFromResources = true;
    public string gameplayFolder = "Gameplay"; // Resources/Audio/Gameplay/
    public List<AudioClip> gameplayClips = new();
    public bool shuffle = true;
    public bool loopPlaylist = true;

    [Header("Playback")]
    [Range(0f, 1f)] public float defaultVolume = 0.4f;
    [Range(0.05f, 5f)] public float fadeSeconds = 1.25f;

    static MusicManager _inst;
    AudioSource _a, _b, _active;
    Coroutine _playlistCo;
    bool _enabled;
    float _volume;
    readonly List<AudioClip> _playQueue = new();

    void Awake()
    {
        if (_inst != null) { Destroy(gameObject); return; }
        _inst = this;
        DontDestroyOnLoad(gameObject);

        _a = gameObject.AddComponent<AudioSource>();
        _b = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { _a, _b }) { s.playOnAwake = false; s.loop = false; s.volume = 0f; }
        _active = _a;

        LoadSettings();
        SceneManager.sceneLoaded += (_, __) => HandleScene(SceneManager.GetActiveScene().name);
        HandleScene(SceneManager.GetActiveScene().name);
    }

    void OnDestroy() { if (_inst == this) SceneManager.sceneLoaded -= (_, __) => { }; }

    // ---------- Settings ----------
    void LoadSettings()
    {
        _enabled = PlayerPrefs.GetInt(KEY_ENABLED, 1) == 1;
        _volume = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_VOLUME, defaultVolume));
    }

    public static void SetMusicEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(KEY_ENABLED, enabled ? 1 : 0); PlayerPrefs.Save();
        if (_inst == null) return;
        _inst._enabled = enabled;
        if (!enabled) _inst.StopAllMusic(); else _inst.HandleScene(SceneManager.GetActiveScene().name);
    }

    public static void SetMusicVolume(float v)
    {
        v = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KEY_VOLUME, v); PlayerPrefs.Save();
        if (_inst == null) return;
        _inst._volume = v;
        if (_inst._active != null && _inst._active.isPlaying) _inst._active.volume = v;
    }

    // ---------- Scene routing ----------
    void HandleScene(string sceneName)
    {
        if (!_enabled) { StopAllMusic(); return; }

        if (uiScenes.Contains(sceneName))
        {
            StopPlaylist();
            // Do not restart if already playing same clip
            if (IsCurrentlyPlaying(uiMusic)) { _active.loop = true; _active.volume = _volume; return; }
            if (uiMusic != null) { StartCoroutine(FadeTo(uiMusic, loop: true)); }
            else { StopAllMusic(); }
            return;
        }

        BuildGameplayQueue();
        if (_playQueue.Count == 0) { StopAllMusic(); return; }
        StartPlaylist();
    }

    bool IsCurrentlyPlaying(AudioClip c) =>
        _active != null && _active.isPlaying && _active.clip == c;

    // ---------- Playlist ----------
    void BuildGameplayQueue()
    {
        _playQueue.Clear();
        if (autoloadGameplayFromResources)
        {
            var loaded = Resources.LoadAll<AudioClip>("Audio/" + gameplayFolder);
            if (loaded != null && loaded.Length > 0) _playQueue.AddRange(loaded);
        }
        if (gameplayClips != null && gameplayClips.Count > 0) _playQueue.AddRange(gameplayClips);

        // de-dup + remove nulls
        var seen = new HashSet<AudioClip>();
        for (int i = _playQueue.Count - 1; i >= 0; i--)
        {
            var c = _playQueue[i];
            if (c == null || seen.Contains(c)) _playQueue.RemoveAt(i);
            else seen.Add(c);
        }
        if (shuffle) Shuffle(_playQueue);
    }

    void StartPlaylist() { StopPlaylist(); _playlistCo = StartCoroutine(Co_Playlist()); }
    void StopPlaylist() { if (_playlistCo != null) { StopCoroutine(_playlistCo); _playlistCo = null; } }

    IEnumerator Co_Playlist()
    {
        int idx = 0;
        do
        {
            var clip = _playQueue[idx];
            yield return FadeTo(clip, loop: false);
            while (_active.isPlaying) yield return null;

            idx++;
            if (idx >= _playQueue.Count)
            {
                if (!loopPlaylist) yield break;
                idx = 0; if (shuffle) Shuffle(_playQueue);
            }
        } while (true);
    }

    // ---------- Crossfade (no restart if same clip) ----------
    IEnumerator FadeTo(AudioClip next, bool loop)
    {
        if (!_enabled) { StopAllMusic(); yield break; }

        AudioSource from = _active;
        AudioSource to = (_active == _a) ? _b : _a;

        // If same clip is already playing, do NOT restart.
        if (from.isPlaying && from.clip == next)
        {
            from.loop = loop;
            from.volume = _volume;
            yield break;
        }

        if (next == null)
        {
            float t = 0f, va = _a.volume, vb = _b.volume;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Clamp01(t / fadeSeconds);
                _a.volume = va * k; _b.volume = vb * k;
                yield return null;
            }
            _a.Stop(); _b.Stop(); _a.volume = 0f; _b.volume = 0f; yield break;
        }

        to.clip = next; to.loop = loop; to.volume = 0f;
        if (!to.isPlaying) to.Play();

        float t2 = 0f, fromStart = from.isPlaying ? from.volume : 0f;
        while (t2 < fadeSeconds)
        {
            t2 += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t2 / fadeSeconds);
            to.volume = _volume * k;
            if (from.isPlaying) from.volume = fromStart * (1f - k);
            yield return null;
        }

        if (from.isPlaying) { from.Stop(); from.volume = 0f; }
        to.volume = _volume;
        _active = to;
    }

    void StopAllMusic() { StopPlaylist(); StartCoroutine(FadeTo(null, loop: false)); }

    static void Shuffle(List<AudioClip> list)
    {
        for (int i = list.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (list[i], list[j]) = (list[j], list[i]); }
    }
}
